# Implementation Blueprint: Role-Based Global Chat with Gemini Moderation & Knowledge-Driven Support

این سند تبدیل تحلیل مفهومی به نقشه‌ی اجرایی برای پیاده‌سازی بک‌اند مبتنی بر **.NET 8 (ASP.NET Core)** و فرانت‌اند **Next.js 14 (App Router)** است. هدف، ایجاد چت گلوبال نقش‌محور با پایش هوشمند Gemini و چت پشتیبانی دانشی است.

---

## 1. Architecture Overview

- **Frontend**: Next.js 14، TypeScript، Server Actions، Zustand/Redux Toolkit برای state، Tailwind CSS برای UI، Socket.IO client برای چت زنده.
- **Backend**: ASP.NET Core 8 Web API، SignalR برای چت real-time، EF Core 8 با PostgreSQL، Redis برای کش و rate limiting، Azure Blob/S3 برای فایل، Background services (Hangfire/Hosted Services) برای پردازش صف.
- **AI Services**: Google Gemini APIs (`gemini-1.5-flash` برای moderation، `gemini-1.5-pro` + `text-embedding-004` برای RAG).
- **Infrastructure**: Docker Compose برای dev، CI/CD (GitHub Actions)، Secrets از طریق Azure Key Vault/AWS Secrets Manager.

---

## 2. Domain & Data Design (Backend)

1. **Entities**
   - `Message`، `Attachment`, `ModerationLog`, `UserDiscipline`, `SupportKnowledgeItem`, `SupportQA`, `Appeal`, `RateLimitEntry`.
2. **DB Schema Tasks**
   - ایجاد migration اولیه با جداول و ایندکس مناسب (role_channel, event_id).
   - افزودن ستون‌های JSON برای `moderation_reasons`, `retrieved_sources`.
3. **Repositories/Services**
   - MessageService (CRUD + paging + pinning).
   - ModerationService (calls Gemini + rule engine).
   - DisciplineService (score ledger + thresholds).
   - SupportKnowledgeService (CRUD + search metadata).
   - SupportChatService (retrieval + logging).
4. **Vector Store**
   - استفاده از pgvector یا Redis Stack. جدول `knowledge_vectors` با ستون برداری 1536.

---

## 3. Backend Feature Breakdown (.NET)

### 3.1 Real-time Role Chats
- SignalR Hub با گروه‌بندی بر اساس `role_channel` و `event_id`.
- Middleware احراز هویت JWT + claim نقش.
- API ها:
  - `POST /api/chats/{role}/messages` (ارسال → صف moderation).
  - `GET /api/chats/{role}/messages` (pagination، فیلتر event).
  - `POST /api/chats/{role}/pins` (ادمین/مدیر نقش).
- Queue (Azure Service Bus/RabbitMQ) برای پردازش async پیام و فراخوانی Gemini در BackgroundService.

### 3.2 Moderation Pipeline
- Rule Engine اولیه (کلمات ممنوع، لینک‌های blacklist).
- سرویس Gemini Moderation:
  - مدل Flash برای پیش‌فرض؛ اگر خروجی «Ambiguous»، دوباره با Pro.
  - Mapping به سطوح اقدام (Publish/SoftWarn/Hold/Block).
- ثبت `ModerationLog` + به‌روزرسانی `UserDiscipline`.
- Webhook یا SignalR event برای اطلاع به کاربر (هشدار، بلاک).

### 3.3 Appeals & Admin Controls
- API ها برای لیست اعتراض‌ها، تغییر امتیاز، تنظیم Threshold.
- Dashboard endpoints برای گزارش‌ها (Aggregations با LINQ/SQL).

### 3.4 Support Chat (RAG)
- API `POST /api/support/ask` → جریان Intent Detection + Retrieval + Generation.
- ایندکس‌گذاری دانش با Background job (Parse PDFs → Text → Chunk → Embed → Store).
- ذخیره `SupportQA` با منابع، confidence.
- Endpoint مدیریت دانش: CRUD با role/event scoping.

### 3.5 Rate Limiting & Anti-Abuse
- Redis-based sliding window برای پیام‌ها.
- Flood detector: الگوریتم بررسی تکرار پیام.
- لینک‌چک با سرویس third-party یا لیست داخلی.

### 3.6 Observability & Auditing
- Serilog + OpenTelemetry (Tracing, Metrics).
- Audit Trail middleware برای ثبت تغییرات ادمین و تصمیمات AI.

---

## 4. Frontend Feature Breakdown (Next.js)

### 4.1 Global Chat UI
- صفحات `/dashboard/[role]/chat` با App Router.
- استفاده از Server Components برای داده اولیه و Client Components برای real-time.
- بخش‌های UI:
  - لیست گفتگو (virtualized list، نمایش status پیام).
  - Composer با پشتیبانی Emoji، آپلود فایل (Dropzone + presigned URL).
  - Badge نقش و امتیاز سلامت (discipline score).
  - بنر سیاست محتوا و لینک اعتراض.
- مدیریت state پیام‌های Pending/Held با optimistic updates + SignalR client.

### 4.2 Moderation Feedback UX
- Modal/Toast هشدار با توضیح AI.
- صفحه `Policy & Appeals` برای مشاهده تاریخچه امتیاز منفی.
- فرم اعتراض (calls `/api/appeals`).

### 4.3 Support Chat UI
- صفحه `/support` (دسترسی از سایدبار).
- Chat-like experience با پیام سیستم/کاربر، نمایش منابع (chips لینک‌دار).
- وضعیت اعتماد پاسخ (Confidence meter)، پیشنهاد escalate.

### 4.4 Admin Panel Extensions
- ماژول مدیریت دانش (editor با Markdown، آپلود فایل → indexing job).
- تنظیم Thresholdها، مشاهده گزارش Heatmap و جدول تخلفات.
- استفاده از React Query برای داده‌های مدیریتی.

### 4.5 Localization & Accessibility
- i18n با `next-intl` (فارسی/انگلیسی).
- RTL پشتیبانی (Tailwind + CSS logical properties).
- کیبورد ناوبری، ARIA labels.

---

## 5. DevOps & Environment Setup

1. **Local Dev**
   - Docker Compose: webapi, postgres, redis, vector-db, nextjs, traefik proxy.
   - Seed scripts برای ایجاد نقش‌ها و داده نمونه.
2. **CI/CD**
   - GitHub Actions: build & test .NET، lint & test Next.js، امنیت (Dependabot, Snyk optional).
   - Deployment به Azure Web App + Static Web App یا AWS (ECS + Amplify).
3. **Secrets & Config**
   - استفاده از User Secrets در dev، Key Vault در prod.
   - ذخیره کلیدهای Gemini و Storage.
4. **Monitoring**
   - Application Insights/Datadog برای backend.
   - Frontend logging با Sentry.

---

## 6. Milestones & Sprints (High-Level)

### Sprint 1 (2 هفته)
- Setup پروژه‌ها (.NET + Next.js + shared models).
- طراحی DB و migrations.
- پیاده‌سازی ابتدایی SignalR chat بدون moderation.
- UI چت پایه (ارسال/دریافت).

### Sprint 2
- ادغام Gemini moderation pipeline + امتیاز انضباطی.
- UI هشدارها، Appeals API & UI.
- Rate limiting و audit logging.

### Sprint 3
- RAG pipeline: ingestion، embedding، پرسش و پاسخ.
- UI چت پشتیبانی، مدیریت دانش ادمین.
- گزارش‌ها و پنل مدیریتی.

### Hardening (Post-Sprint)
- تست امنیتی، بار، بهینه‌سازی latency.
- مستندسازی و آموزش اپراتورها.

---

## 7. Acceptance Checklist

- پیام‌ها قبل از انتشار توسط سرویس moderation بررسی و تصمیم ثبت می‌شود.
- امتیاز انضباطی با Thresholdهای قابل تنظیم اعمال و قابل مشاهده است.
- چت پشتیبانی حداقل 80٪ سوالات سناریوی آزمایشی را پاسخ می‌دهد و منابع را نشان می‌دهد.
- ادمین می‌تواند سیاست‌ها، دانش‌پایه و اعتراض‌ها را مدیریت کند.
- نرخ ارسال پیام، ترافیک moderation و گزارش‌ها در داشبورد قابل مشاهده است.

