# NabTeams Conversational Platform

این مخزن شامل بک‌اند ASP.NET Core (.NET 8) و فرانت‌اند Next.js 14 برای پیاده‌سازی «چت گلوبال نقش‌محور با پایش محتوایی Gemini»، «چت پشتیبانی دانشی مبتنی بر RAG»، «چرخهٔ انضباطی و امتیاز منفی» و «ماژول اعتراض» است. نسخهٔ حاضر به پایگاه‌داده PostgreSQL متصل می‌شود، احراز هویت SSO/JWT دارد و از Gemini برای Moderation/RAG (با قابلیت Fallback) بهره می‌گیرد.

## ساختار مخزن

```
backend/
  NabTeams/src/Domain/            # لایه دامنه (Entities، Value Objects)
  NabTeams/src/Application/       # لایه کاربرد (Contracts، DTOها، سیاست‌ها)
  NabTeams/src/Infrastructure/    # پیاده‌سازی زیرساخت (EF Core، Gemini، Health Checks)
  NabTeams/src/Web/               # هاست ASP.NET Core (Program، Controllers، SignalR)
  NabTeams/test/NabTeams.Api.Tests/    # تست‌های واحد بک‌اند
frontend/                # اپ Next.js (App Router + NextAuth)
implementation_plan.md   # سند تحلیل و طراحی اولیه
```

## متغیرهای محیطی کلیدی

| نام                                                             | محل استفاده | توضیح                                                                                                             |
| --------------------------------------------------------------- | ----------- | ----------------------------------------------------------------------------------------------------------------- |
| `ConnectionStrings__DefaultConnection`                          | بک‌اند      | رشته اتصال PostgreSQL (پیش‌فرض: `Host=localhost;Port=5432;Database=nabteams;Username=nabteams;Password=nabteams`) |
| `Gemini__ApiKey`                                                | بک‌اند      | کلید دسترسی Google Gemini. در صورت خالی بودن، سرویس‌ها به حالت Rule-based برمی‌گردند.                             |
| `Gemini__ModerationModel`, `Gemini__RagModel`                   | بک‌اند      | نام مدل برای Moderation/RAG. پیش‌فرض: `gemini-1.5-pro`.                                                           |
| `Authentication__Authority`                                     | بک‌اند      | آدرس سرور SSO/OIDC.                                                                                               |
| `Authentication__Audience`                                      | بک‌اند      | Audience توکن JWT.                                                                                                |
| `Authentication__AdminRole`                                     | بک‌اند      | نام نقش ادمین (پیش‌فرض `admin`).                                                                                  |
| `Authentication__Disabled`                                      | بک‌اند      | اگر `true` باشد، احراز هویت غیرفعال می‌شود (برای توسعه محلی).                                                     |
| `NEXTAUTH_URL`                                                  | فرانت‌اند   | آدرس پابلیک اپ Next.js (مثلاً `http://localhost:3000`).                                                           |
| `NEXTAUTH_SECRET`                                               | فرانت‌اند   | کلید رمزنگاری سشن NextAuth.                                                                                       |
| `SSO_ISSUER`, `SSO_CLIENT_ID`, `SSO_CLIENT_SECRET`, `SSO_SCOPE` | فرانت‌اند   | تنظیمات ارائه‌دهنده OIDC برای NextAuth. اگر مقداردهی نشود و `AUTH_ALLOW_DEV=true` باشد، ورود آزمایشی فعال می‌شود. |
| `AUTH_ALLOW_DEV`                                                | فرانت‌اند   | در صورت `true` (پیش‌فرض)، Provider ورود آزمایشی (Credentials) فعال می‌شود.                                        |
| `NEXT_PUBLIC_API_URL`                                           | فرانت‌اند   | آدرس سرویس بک‌اند (پیش‌فرض `http://localhost:5000`).                                                              |

## راه‌اندازی بک‌اند

1. پیش‌نیازها: [Docker اختیاری برای PostgreSQL]، [.NET 8 SDK](https://dotnet.microsoft.com/download)، و سرویس PostgreSQL در حال اجرا.
2. ایجاد پایگاه‌داده (نمونه):
   ```bash
   docker run --name nabteams-postgres -e POSTGRES_PASSWORD=nabteams -e POSTGRES_USER=nabteams -e POSTGRES_DB=nabteams -p 5432:5432 -d postgres:15
   ```
3. اجرای سرویس:
   ```bash
   cd backend/src/Web
   dotnet restore
   dotnet run --urls http://localhost:5000
   ```
4. اولین اجرا مهاجرت EF Core را اعمال و منابع اولیهٔ دانش را Seed می‌کند. مستندات Swagger در `http://localhost:5000/swagger` در دسترس است.

### مهم‌ترین APIها

- `POST /api/chat/{role}/messages` — ارسال پیام، پایش Gemini و اعمال امتیاز انضباطی.
- `GET /api/chat/{role}/messages` — دریافت پیام‌های منتشرشده کانال (پیام‌های مسدود‌شده نمایش داده نمی‌شوند).
- `GET /api/discipline/{role}/me` — مشاهده وضعیت امتیاز انضباطی کاربر جاری.
- `POST /api/appeals` — ثبت اعتراض نسبت به پیام مسدود شده.
- `GET /api/appeals` — فهرست اعتراض‌های کاربر.
- `GET /api/appeals/admin` و `POST /api/appeals/{id}/decision` — بررسی و تصمیم‌گیری توسط ادمین.
- `POST /api/support/query` — پاسخ دانشی (RAG) با Gemini.
- `GET/POST/DELETE /api/knowledge-base` — مدیریت منابع دانش توسط ادمین.
- `GET /api/moderation/{role}/logs` — مشاهده لاگ‌های پایش (ادمین).
- `GET /health/live` — بررسی زنده بودن سرویس (بدون وابستگی به زیرساخت).
- `GET /health/ready` — بررسی آمادگی شامل اتصال پایگاه‌داده و دسترسی به Gemini.

## راه‌اندازی فرانت‌اند

1. پیش‌نیاز: [Node.js 18+](https://nodejs.org/)، متغیرهای محیطی NextAuth (حداقل `NEXTAUTH_SECRET`).
2. نصب و اجرا:
   ```bash
   cd frontend
   npm install
   npm run dev
   ```
3. اپلیکیشن در `http://localhost:3000` در دسترس است. برای استفاده از SSO باید متغیرهای `SSO_*` و `NEXTAUTH_URL` مقداردهی شود. در محیط توسعه می‌توانید از دکمه «ورود آزمایشی» استفاده کنید (`AUTH_ALLOW_DEV=true`).

## قابلیت‌های کلیدی

- **اتصال واقعی به Gemini:** `GeminiModerationService` و `SupportResponder` در صورت وجود `Gemini__ApiKey` درخواست ساختار‌یافته JSON به API رسمی می‌فرستند و در صورت خطا به Rule-based fallback می‌کنند.
- **پایش سلامت و تاب‌آوری:** بک‌اند دارای Health Check های جداگانه برای پایگاه‌داده و Gemini است (`/health/live`, `/health/ready`) و مشتری Gemini با سیاست‌های Retry و Circuit-Breaker محافظت می‌شود.
- **پایگاه‌داده پایدار:** تمام پیام‌ها، لاگ‌ها، امتیازات انضباطی، دانش و اعتراض‌ها در PostgreSQL ذخیره می‌شوند. مهاجرت‌ها به صورت خودکار هنگام اجرا اعمال می‌شوند.
- **احراز هویت و مجوز:** بک‌اند با JWT Bearer از SSO سازمانی پشتیبانی می‌کند. مسیرهای ادمین با Policy `AdminOnly` محافظت شده‌اند. فرانت‌اند از NextAuth (OIDC) با امکان ورود آزمایشی بهره می‌گیرد.
- **چرخهٔ انضباطی کامل:** هر پیام پایش شده، امتیاز منفی/مثبت را به‌روزرسانی می‌کند. وضعیت کاربر و تاریخچه رویدادها قابل استعلام است.
- **ماژول اعتراض:** کاربران می‌توانند برای پیام‌های مسدود‌شده اعتراض ثبت کنند، و ادمین‌ها با فیلتر نقش/وضعیت بررسی و تایید/رد را ثبت می‌کنند.
- **پشتیبانی دانشی RAG:** Gemini پاسخ را بر اساس منابع مدیریت‌شده توسط ادمین (به همراه Confidence و منابع استناد) تولید می‌کند. در نبود API Key الگوریتم رتبه‌بندی داخلی استفاده می‌شود.
- **فرانت‌اند راست‌به‌چپ با نقش‌محوری:** داشبورد Next.js شامل مدیریت نقش، چت، پشتیبانی، مدیریت دانش و اعتراض‌ها است. جلسات NextAuth نقش‌های کاربر را به صورت Context در اختیار تمام اجزا قرار می‌دهد.
- **هاردنینگ امنیتی و عملیاتی:** هدرهای امنیتی پیش‌فرض، فشرده‌سازی پاسخ، متریک‌های Prometheus و Logهای HTTP برای مانیتورینگ و مقابله با تهدیدات فعال هستند. دستورالعمل کامل در `docs/operations-runbook.md` موجود است.

## نکات توسعه

- برای اجرا بدون SSO، مقدار `Authentication__Disabled=true` را در بک‌اند و `AUTH_ALLOW_DEV=true` را در فرانت‌اند قرار دهید تا ورود آزمایشی فعال شود.
- در حالت غیرفعال بودن احراز هویت (`Authentication__Disabled=true`) فرانت‌اند به صورت خودکار شناسه، ایمیل و نقش‌های کاربر را از طریق هدرهای `X-Debug-User`، `X-Debug-Email` و `X-Debug-Roles` ارسال می‌کند تا API بتواند سیاست‌های نقش‌محور را اعمال کند.
- برای اتصال real-time (SignalR) در همین حالت توسعه، همان داده‌ها از طریق Query String (`debug_user`، `debug_email`، `debug_roles`) نیز ارسال می‌شود تا هندشیک وب‌سوکت بدون نیاز به هدر سفارشی کار کند.
- جهت اتصال به سرویس Gemini، کلید سرویس را در `Gemini__ApiKey` قرار دهید. در صورت نیاز می‌توانید مدل‌ها را از طریق `Gemini__ModerationModel` و `Gemini__RagModel` تغییر دهید.
- درخواست‌های API از فرانت‌اند همیشه توکن دسترسی NextAuth را در هدر `Authorization` ارسال می‌کنند؛ در حالت توسعه (بدون احراز هویت) بک‌اند نیز در حالت آزاد اجرا می‌شود.
- نرخ محدودسازی پیام‌ها، نگاشت امتیاز و قوانین پایش در `GeminiModerationService` و `SlidingWindowRateLimiter` قابل تنظیم است.

## تست و استقرار

- برای اطمینان از پایداری پایگاه‌داده، اجرای دوره‌ای `dotnet ef migrations add` و `dotnet ef database update` (در محیط‌های غیرتوسعه) توصیه می‌شود.
- پیشنهاد می‌شود متغیرهای محیطی در فایل `.env` (فرانت‌اند) و Secret Manager یا KeyVault (بک‌اند) نگهداری شوند.
- هنگام استقرار فرانت‌اند، `NEXTAUTH_URL` باید آدرس نهایی (HTTPS) باشد تا تبادل سشن به درستی انجام گیرد.

برای توسعه بیشتر می‌توانید به سند `implementation_plan.md` مراجعه کنید که ریزمعماری و جریان‌های فرایندی را شرح می‌دهد.

## Hardening & QA

- **آزمون‌های واحد:** پروژه `backend/NabTeams.Api.Tests` پوشش‌بخش منطق‌های حساس (کنترل طول پیام، نرخ‌محدودسازی و رتبه‌بندی پاسخ دانشی) است. برای اجرا:
  ```bash
  dotnet test backend/NabTeams.Api.Tests
  ```
  اجرای این تست‌ها پیش از انتشار هر نسخه الزامی است تا از جلوگیری پیام‌های بیش‌ازحد بلند و رفتار صحیح RAG مطمئن شوید.
- **آزمون بار (k6):** اسکریپت `ops/load-tests/chat-smoke.js`، صحت Health Check و تاخیر چت را زیر بار سبک بررسی می‌کند. نمونه اجرا با هدرهای توسعه:
  ```bash
  k6 run ops/load-tests/chat-smoke.js \
    --env BASE_URL=http://localhost:5000 \
    --env DEBUG_MODE=1 \
    --env CHAT_ROLE=participant
  ```
  در صورت داشتن توکن واقعی، `--env BEARER_TOKEN=...` را جایگزین هدرهای دیباگ کنید.
- **محدودیت محتوایی:** حداکثر طول پیام چت ۲۰۰۰ نویسه و حداکثر طول پرسش پشتیبانی ۱۵۰۰ نویسه است. این محدودیت در بک‌اند enforce شده و فرانت‌اند پیش از ارسال هشدار می‌دهد تا از حملات DoS متنی پیشگیری شود.
- **سیاست پاسخ‌گویی:** خطاهای Gemini یا عدم تطابق منابع در لاگ‌ها ثبت می‌شوند. اپراتورها باید گزارش‌های حاصل از Health Check را در مانیتورینگ بررسی کنند و در صورت عبور متریک `chat_latency` از 800ms اقدامات scaling انجام دهند.
- **اسکن امنیتی:** اسکریپت `ops/security/zap-baseline.sh`، اسکن پایه OWASP ZAP را اجرا و گزارش HTML ایجاد می‌کند. برای جزئیات بیشتر به `docs/operations-runbook.md` مراجعه کنید.
