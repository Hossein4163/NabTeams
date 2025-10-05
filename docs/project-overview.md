# NabTeams Project Overview

این سند نمای کاملی از ساختار کد موجود در مخزن NabTeams و نقش هر جزء ارائه می‌دهد تا مسیر توسعه‌های بعدی (از جمله ماژول‌های ثبت‌نام و مدیریت اکوسیستم) با شناخت دقیق وضعیت فعلی آغاز شود.

## لایه‌های اصلی

- **backend/** — سرویس ASP.NET Core شامل لایه دامنه، کاربرد، زیرساخت و وب برای API و SignalR.
- **frontend/** — اپلیکیشن Next.js 14 (App Router) با NextAuth برای احراز هویت و رابط نقش‌محور.
- **docs/** — مستندات عملیاتی و معماری.
- **ops/** — اسکریپت‌های تست بار و اسکن امنیتی.
- **implementation_plan.md** — طرح اولیهٔ معماری و گام‌های توسعه.

## Backend

### ساختار Solution

`backend/NabTeams/NabTeams.sln` شامل پروژه‌های زیر است:

| پروژه | توضیح |
| --- | --- |
| `Domain` | مدل دامنه شامل موجودیت‌ها و مقادیر ثابت. |
| `Application` | قراردادها و DTOها برای تعامل لایه‌های بالاتر. |
| `Infrastructure` | پیاده‌سازی دسترسی داده، صف، سرویس Gemini، محدودکننده نرخ و Health Check. |
| `Web` | میزبان ASP.NET Core، کنترلرهای REST، SignalR و Middleware. |
| `NabTeams.Api.Tests` | تست‌های واحد سرویس‌های کلیدی. |

### Domain (`backend/NabTeams/src/Domain`)

- **Entities/Message.cs** — مدل پیام، لاگ پایش، و امتیاز انضباطی کاربر به همراه وضعیت پیام (منتشر شده، نگه‌داشته شده، مسدود). 
- **Entities/Appeal.cs** — مدل اعتراض و وضعیت رسیدگی. 
- **Entities/SupportModels.cs** — رکوردهای کوئری/پاسخ پشتیبانی دانشی و آیتم‌های پایگاه دانش. 
- **Enums/** — تعریف کانال‌های نقش‌محور و سایر ثابت‌ها.

### Application (`backend/NabTeams/src/Application`)

- **Common/Requests.cs** — DTOهای ورودی/خروجی برای ارسال پیام، مدیریت پایگاه دانش، و اعتراض‌ها؛ شامل محدودیت طول پیام (۲۰۰۰ کاراکتر) و حداکثر طول سوال پشتیبانی (۱۵۰۰ کاراکتر).
- **Common/GeminiOptions.cs** — تنظیمات تزریق‌شده برای سرویس Gemini (کلید، نام مدل‌ها، حالت شبیه‌ساز).
- **Abstractions/** — اینترفیس‌هایی برای سرویس‌های زیرساختی: مخزن چت، صف پردازش، سرویس پایش/Rate Limiter، پاسخ‌گو پشتیبانی، و ثبت متریک‌ها. لایه Web با تکیه بر این قراردادها پیاده‌سازی شده است.

### Infrastructure (`backend/NabTeams/src/Infrastructure`)

- **Persistence/**
  - `ApplicationDbContext.cs` — DbContext مبتنی بر EF Core برای پیام‌ها، اعتراض‌ها، لاگ‌ها، امتیاز انضباطی و آیتم‌های دانش.
  - `EfStores.cs` و `InMemoryStores.cs` — پیاده‌سازی قراردادهای مخزن برای محیط پایگاه‌داده و حافظه‌ای (حالت توسعه/تست).
  - `DatabaseInitializer.cs` — اعمال مهاجرت و Seed اولیه منابع دانش.
  - `EntityMappingExtensions.cs` — تبدیل بین مدل دامنه و مدل دیتابیس.
- **Services/**
  - `GeminiModerationService.cs` — فراخوانی Gemini (Moderation/RAG) با قابلیت Failover به قواعد محلی.
  - `SupportResponder.cs` — پیاده‌سازی RAG برای پاسخ‌گویی دانشی و بازگشت منابع استناد.
  - `SlidingWindowRateLimiter.cs` — محدودسازی نرخ پیام به ازای کاربر/کانال (تست واحد دارد).
- **Queues/ChatModerationQueue.cs** — صف درون‌حافظه‌ای برای پردازش پیام‌های منتظر پایش توسط Worker پس‌زمینه.
- **Monitoring/MetricsRecorder.cs** — تولید متریک‌های Prometheus برای تاخیر چت، تعداد پیام‌های مسدود و خطاهای Gemini.
- **HealthChecks/** — دو Health Check (پایگاه‌داده و دسترسی به Gemini) که در مسیرهای `/health/live` و `/health/ready` استفاده می‌شوند.

### Web (`backend/NabTeams/src/Web`)

- **Program.cs** — راه‌اندازی سرویس، رجیستری DI، ثبت SignalR، Swagger، Health Checks و پیکربندی CORS.
- **Configuration/AuthenticationSettings.cs** — تنظیمات SSO/JWT و حالت توسعه بدون احراز هویت.
- **Middleware/SecurityHeadersMiddleware.cs** — تزریق هدرهای امنیتی (HSTS، CSP، X-Content-Type-Options و ...).
- **Controllers/**
  - `ChatController.cs` — Endpoints ارسال/دریافت پیام نقش‌محور با اعمال Rate Limiter، پایش و بازگردانی وضعیت پیام.
  - `ModerationController.cs` — مشاهده لاگ‌های پایش (صرفاً برای ادمین).
  - `DisciplineController.cs` — دریافت وضعیت امتیاز انضباطی کاربر در هر نقش.
  - `AppealsController.cs` — ثبت اعتراض توسط کاربر و بررسی/تصمیم‌گیری توسط ادمین.
  - `KnowledgeBaseController.cs` — CRUD پایگاه دانش (ادمین).
  - `SupportController.cs` — مسیر پرسش پشتیبانی دانشی (Gemini RAG).
- **Hubs/ChatHub.cs** — کانال SignalR برای توزیع پیام‌های منتشر شده و به‌روزرسانی بلادرنگ.
- **Background/ChatModerationWorker.cs** — Worker پس‌زمینه‌ای که صف پیام‌ها را مصرف و نتایج پایش را ثبت می‌کند.
- **appsettings.json** — تنظیمات پیش‌فرض (اتصال PostgreSQL، Gemini، Logging و تنظیمات توسعه).

### Tests (`backend/NabTeams/test/NabTeams.Api.Tests`)

- `ChatControllerTests.cs` — اعتبارسنجی رفتار ارسال پیام، محدودیت طول و نرخ.
- `SlidingWindowRateLimiterTests.cs` — پوشش منطق Rate Limiter.
- `SupportResponderTests.cs` — تضمین صحت پاسخ RAG در حالت شبیه‌ساز و انتخاب منابع.

## Frontend (`frontend/`)

### پیکربندی

- `package.json` — وابستگی‌ها شامل Next.js، NextAuth، axios، SWR و SignalR.
- `next.config.mjs` — فعال‌سازی Strict Mode و تنظیمات Next.
- `tsconfig.json` — پیکربندی TypeScript با مسیرهای مطلق برای `@/`.
- `next-auth.d.ts` — افزودن فیلد نقش‌ها به Session و JWT.

### App Router (`frontend/app`)

- `layout.tsx` — پوسته اصلی با فونت فارسی، RTL، و Providers عمومی.
- `providers.tsx` — راه‌اندازی `SessionProvider`, `RoleProvider` و تم.
- `page.tsx` — داشبورد نقش‌محور که بر اساس نقش به ماژول‌ها لینک می‌دهد.
- `(dashboard)/global-chat/page.tsx` — صفحه چت عمومی با `ChatPanel`.
- `(dashboard)/support/page.tsx` — دسترسی به پشتیبانی دانشی با `SupportPanel`.
- `(dashboard)/knowledge-base/page.tsx` — مدیریت منابع دانش (فقط ادمین).
- `appeals/page.tsx` — ثبت و مشاهده اعتراض‌ها.
- `auth/signin/page.tsx` — صفحه ورود با گزینه SSO یا ورود آزمایشی.
- `api/auth/[...nextauth]/route.ts` — پیکربندی NextAuth، Provider های OIDC و توسعه.

### Components (`frontend/components`)

- `chat-panel.tsx` — UI و منطق ارسال/نمایش پیام، اتصال SignalR و مدیریت وضعیت «Held/Blocked».
- `support-panel.tsx` — فرم ارسال سوال پشتیبانی و نمایش پاسخ همراه منابع استناد.
- `knowledge-base-manager.tsx` — لیست و فرم CRUD آیتم‌های دانش با محافظت نقش ادمین.
- `appeals-panel.tsx` — ثبت اعتراض برای پیام مسدود و مشاهده وضعیت رسیدگی.
- `role-switcher.tsx` — انتخاب نقش فعال کاربر و بروزرسانی Context.

### کتابخانه‌ها (`frontend/lib`)

- `api.ts` — کلاینت axios با درج توکن NextAuth و هدرهای دیباگ در حالت توسعه.
- `chat-hub.ts` — راه‌اندازی اتصال SignalR به `/hubs/chat` به همراه مدیریت Debug Headers.
- `role-context.tsx` — Context و Provider برای نقش فعال.
- `use-role.ts` — هوک سفارشی برای کار با نقش فعال و سطح دسترسی.

### Public Assets (`frontend/public`)

- شامل لوگوها و دارایی‌های استاتیک مورد استفاده در UI.

## عملیات (`ops/`)

- `load-tests/chat-smoke.js` — اسکریپت k6 برای سنجش تاخیر و Health API چت با امکان تزریق هدرهای دیباگ یا توکن.
- `security/zap-baseline.sh` — اجرای OWASP ZAP Baseline Scan روی محیط استیج/پروداکشن.

## وابستگی‌ها و یکپارچگی‌ها

- **پایگاه‌داده:** PostgreSQL 15 (قابل تعویض) با مهاجرت‌های خودکار.
- **هوش مصنوعی:** Google Gemini برای Moderation و RAG، با حالت شبیه‌سازی Rule-based در صورت عدم وجود API Key.
- **احراز هویت:** JWT Bearer در بک‌اند (قابل غیرفعال‌سازی در توسعه) و NextAuth در فرانت‌اند.
- **Real-time:** SignalR در بک‌اند و @microsoft/signalr در فرانت‌اند برای انتشار پیام‌ها.

## مسیرهای توسعه پیشنهادی

- افزودن ماژول‌های ثبت‌نام چندمرحله‌ای و مدیریت پروفایل بر بستر EF Core.
- گسترش Context نقش‌ها برای پوشش نقش سرمایه‌گذار/داور با دسترسی‌های جدید.
- توسعه تست‌های یکپارچه برای گردش‌کارهای جدید پس از پیاده‌سازی ثبت‌نام و ارزیابی بیزینس‌پلن.

این سند باید در کنار README موجود خوانده شود تا تصویر کامل از نحوه اجرای فعلی و نیازهای توسعه آینده فراهم گردد.
