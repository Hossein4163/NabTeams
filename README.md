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
| `Gemini__BusinessPlanModel`, `Gemini__BusinessPlanTemperature`  | بک‌اند      | مدل و دمای تولید متن برای تحلیل بیزینس‌پلن (پیش‌فرض `gemini-1.5-pro` و `0.2`).                                     |
| `Gemini__BaseUrl`                                               | بک‌اند      | آدرس پایه API Gemini؛ در صورت استفاده از پروکسی می‌توان مقداردهی کرد.                                             |
| `Authentication__Authority`                                     | بک‌اند      | آدرس سرور SSO/OIDC.                                                                                               |
| `Authentication__Audience`                                      | بک‌اند      | Audience توکن JWT.                                                                                                |
| `Authentication__AdminRole`                                     | بک‌اند      | نام نقش ادمین (پیش‌فرض `admin`).                                                                                  |
| `Authentication__Disabled`                                      | بک‌اند      | اگر `true` باشد، احراز هویت غیرفعال می‌شود (برای توسعه محلی).                                                     |
| `Payments__BaseUrl`                                             | بک‌اند      | آدرس پایه ارائه‌دهنده پرداخت (پیش‌فرض `https://api.idpay.ir`).                                                    |
| `Payments__ApiKey`                                              | بک‌اند      | کلید API درگاه پرداخت.                                                                                             |
| `Payments__CallbackBaseUrl`                                     | بک‌اند      | آدرس پابلیک برای بازگشت از درگاه (مثلاً `https://portal.example.com`).                                            |
| `Payments__Sandbox`                                             | بک‌اند      | اگر `true` باشد هدر Sandbox برای IdPay فعال می‌شود.                                                                |
| `Notification__Email__Host`, `Notification__Email__Username`, `Notification__Email__Password` | بک‌اند | تنظیمات SMTP برای ارسال ایمیل (همراه با `Notification__Email__SenderAddress`).                                    |
| `Notification__Sms__ApiKey`, `Notification__Sms__BaseUrl`, `Notification__Sms__SenderNumber` | بک‌اند | تنظیمات Gateway پیامک (پیش‌فرض برای Kavenegar).                                                                    |
| `Registration__StoragePath`                                    | بک‌اند      | مسیر سفارشی ذخیره‌سازی مدارک (نسبی به ریشه اپ یا مطلق).                                                             |
| `Registration__PublicBaseUrl`                                  | بک‌اند      | آدرس پابلیک فایل‌های آپلود شده در صورت استفاده از فضای خارج از `wwwroot`.                                         |
| `Operations__ArtifactsPath`                                    | بک‌اند      | مسیر ذخیره‌سازی مستندات عملیات (پیش‌فرض: `wwwroot/uploads/operations`).                                            |
| `Operations__ArtifactsPublicBaseUrl`                           | بک‌اند      | آدرس پابلیک مستندات عملیات هنگام استفاده از فضای ذخیره‌سازی خارجی/CDN.                                           |
| `INTEGRATIONS__GEMINI`                                          | بک‌اند      | JSON شامل `providerKey`, `displayName`, `configuration` و `isActive` برای ثبت خودکار تنظیمات Gemini در پایگاه‌داده. |
| `INTEGRATIONS__PAYMENT_IDPAY`                                   | بک‌اند      | JSON تنظیمات درگاه پرداخت (IdPay یا سازگار) برای درج و فعال‌سازی خودکار.                                           |
| `INTEGRATIONS__EMAIL_SMTP`                                      | بک‌اند      | JSON حاوی تنظیمات SMTP (هاست، پورت، کاربر، رمز و ...).                                                              |
| `INTEGRATIONS__SMS_KAVENEGAR`                                   | بک‌اند      | JSON تنظیمات Gateway پیامک (مانند Kavenegar) جهت ثبت در جدول یکپارچه‌سازی.                                         |
| `NEXTAUTH_URL`                                                  | فرانت‌اند   | آدرس پابلیک اپ Next.js (مثلاً `http://localhost:3000`).                                                           |
| `NEXTAUTH_SECRET`                                               | فرانت‌اند   | کلید رمزنگاری سشن NextAuth.                                                                                       |
| `SSO_ISSUER`, `SSO_CLIENT_ID`, `SSO_CLIENT_SECRET`, `SSO_SCOPE` | فرانت‌اند   | تنظیمات ارائه‌دهنده OIDC برای NextAuth. اگر مقداردهی نشود و `AUTH_ALLOW_DEV=true` باشد، ورود آزمایشی فعال می‌شود. |
| `AUTH_ALLOW_DEV`                                                | فرانت‌اند   | در صورت `true` (پیش‌فرض)، Provider ورود آزمایشی (Credentials) فعال می‌شود.                                        |
| `NEXT_PUBLIC_API_URL`                                           | فرانت‌اند   | آدرس سرویس بک‌اند (پیش‌فرض `http://localhost:5000`).                                                              |
| `FileStorage__RootPath`                                         | بک‌اند      | مسیر ذخیره فایل‌های بارگذاری‌شده ثبت‌نام. اگر نسبی باشد نسبت به ریشه پروژه وب تفسیر می‌شود (پیش‌فرض: `storage/uploads`). |
| `FileStorage__PublicBaseUrl`                                    | بک‌اند      | آدرس پایه قابل‌دسترسی برای فایل‌های آپلود شده (مثلاً `/uploads` یا URL کامل CDN).                                 |

> **نکته:** پس از ورود به سیستم با نقش ادمین، می‌توانید مقادیر مربوط به Gemini، درگاه پرداخت، SMTP و پنل پیامکی را از مسیر `/dashboard/admin/integrations` وارد یا ویرایش کنید؛ در این صورت مقادیر جدول بالا به‌عنوان مقدار پیش‌فرض عمل کرده و در زمان اجرا با داده‌های پایگاه‌داده جایگزین می‌شوند.

> **نکته:** پس از ورود به سیستم با نقش ادمین، می‌توانید مقادیر مربوط به Gemini، درگاه پرداخت، SMTP و پنل پیامکی را از مسیر `/dashboard/admin/integrations` وارد یا ویرایش کنید؛ در این صورت مقادیر جدول بالا به‌عنوان مقدار پیش‌فرض عمل کرده و در زمان اجرا با داده‌های پایگاه‌داده جایگزین می‌شوند. همچنین می‌توانید وضعیت اجرای چک‌لیست امنیت/عملیات را از مسیر `/dashboard/admin/operations` به‌روزرسانی و مستند کنید تا پیش‌نیازهای انتشار دنبال شود. در محیط‌های تولیدی می‌توان مقدار JSON هر سرویس را در متغیرهای `INTEGRATIONS__*` قرار داد تا هنگام راه‌اندازی به صورت خودکار در پایگاه‌داده ذخیره و فعال شود؛ مثال برای Gemini:
> ```json
> {
>   "providerKey": "gemini",
>   "displayName": "Google Gemini Production",
>   "configuration": {
>     "ApiKey": "prod-key",
>     "BaseUrl": "https://generativelanguage.googleapis.com",
>     "BusinessPlanModel": "gemini-1.5-pro"
>   },
>   "isActive": true
> }
> ```

> **نکته:** پس از ورود به سیستم با نقش ادمین، می‌توانید مقادیر مربوط به Gemini، درگاه پرداخت، SMTP و پنل پیامکی را از مسیر `/dashboard/admin/integrations` وارد یا ویرایش کنید؛ در این صورت مقادیر جدول بالا به‌عنوان مقدار پیش‌فرض عمل کرده و در زمان اجرا با داده‌های پایگاه‌داده جایگزین می‌شوند. همچنین می‌توانید وضعیت اجرای چک‌لیست امنیت/عملیات را از مسیر `/dashboard/admin/operations` به‌روزرسانی و مستند کنید تا پیش‌نیازهای انتشار دنبال شود. در محیط‌های تولیدی می‌توان مقدار JSON هر سرویس را در متغیرهای `INTEGRATIONS__*` قرار داد تا هنگام راه‌اندازی به صورت خودکار در پایگاه‌داده ذخیره و فعال شود؛ مثال برای Gemini:
> ```json
> {
>   "providerKey": "gemini",
>   "displayName": "Google Gemini Production",
>   "configuration": {
>     "ApiKey": "prod-key",
>     "BaseUrl": "https://generativelanguage.googleapis.com",
>     "BusinessPlanModel": "gemini-1.5-pro"
>   },
>   "isActive": true
> }
> ```

> **نکته:** پس از ورود به سیستم با نقش ادمین، می‌توانید مقادیر مربوط به Gemini، درگاه پرداخت، SMTP و پنل پیامکی را از مسیر `/dashboard/admin/integrations` وارد یا ویرایش کنید؛ در این صورت مقادیر جدول بالا به‌عنوان مقدار پیش‌فرض عمل کرده و در زمان اجرا با داده‌های پایگاه‌داده جایگزین می‌شوند. همچنین می‌توانید وضعیت اجرای چک‌لیست امنیت/عملیات را از مسیر `/dashboard/admin/operations` به‌روزرسانی و مستند کنید تا پیش‌نیازهای انتشار دنبال شود. در محیط‌های تولیدی می‌توان مقدار JSON هر سرویس را در متغیرهای `INTEGRATIONS__*` قرار داد تا هنگام راه‌اندازی به صورت خودکار در پایگاه‌داده ذخیره و فعال شود؛ مثال برای Gemini:
> ```json
> {
>   "providerKey": "gemini",
>   "displayName": "Google Gemini Production",
>   "configuration": {
>     "ApiKey": "prod-key",
>     "BaseUrl": "https://generativelanguage.googleapis.com",
>     "BusinessPlanModel": "gemini-1.5-pro"
>   },
>   "isActive": true
> }
> ```

> **نکته:** پس از ورود به سیستم با نقش ادمین، می‌توانید مقادیر مربوط به Gemini، درگاه پرداخت، SMTP و پنل پیامکی را از مسیر `/dashboard/admin/integrations` وارد یا ویرایش کنید؛ در این صورت مقادیر جدول بالا به‌عنوان مقدار پیش‌فرض عمل کرده و در زمان اجرا با داده‌های پایگاه‌داده جایگزین می‌شوند. همچنین می‌توانید وضعیت اجرای چک‌لیست امنیت/عملیات را از مسیر `/dashboard/admin/operations` به‌روزرسانی و مستند کنید تا پیش‌نیازهای انتشار دنبال شود. در محیط‌های تولیدی می‌توان مقدار JSON هر سرویس را در متغیرهای `INTEGRATIONS__*` قرار داد تا هنگام راه‌اندازی به صورت خودکار در پایگاه‌داده ذخیره و فعال شود؛ مثال برای Gemini:
> ```json
> {
>   "providerKey": "gemini",
>   "displayName": "Google Gemini Production",
>   "configuration": {
>     "ApiKey": "prod-key",
>     "BaseUrl": "https://generativelanguage.googleapis.com",
>     "BusinessPlanModel": "gemini-1.5-pro"
>   },
>   "isActive": true
> }
> ```

> **نکته:** پس از ورود به سیستم با نقش ادمین، می‌توانید مقادیر مربوط به Gemini، درگاه پرداخت، SMTP و پنل پیامکی را از مسیر `/dashboard/admin/integrations` وارد یا ویرایش کنید؛ در این صورت مقادیر جدول بالا به‌عنوان مقدار پیش‌فرض عمل کرده و در زمان اجرا با داده‌های پایگاه‌داده جایگزین می‌شوند. همچنین می‌توانید وضعیت اجرای چک‌لیست امنیت/عملیات را از مسیر `/dashboard/admin/operations` به‌روزرسانی و مستند کنید تا پیش‌نیازهای انتشار دنبال شود. در محیط‌های تولیدی می‌توان مقدار JSON هر سرویس را در متغیرهای `INTEGRATIONS__*` قرار داد تا هنگام راه‌اندازی به صورت خودکار در پایگاه‌داده ذخیره و فعال شود؛ مثال برای Gemini:
> ```json
> {
>   "providerKey": "gemini",
>   "displayName": "Google Gemini Production",
>   "configuration": {
>     "ApiKey": "prod-key",
>     "BaseUrl": "https://generativelanguage.googleapis.com",
>     "BusinessPlanModel": "gemini-1.5-pro"
>   },
>   "isActive": true
> }
> ```

> **نکته:** پس از ورود به سیستم با نقش ادمین، می‌توانید مقادیر مربوط به Gemini، درگاه پرداخت، SMTP و پنل پیامکی را از مسیر `/dashboard/admin/integrations` وارد یا ویرایش کنید؛ در این صورت مقادیر جدول بالا به‌عنوان مقدار پیش‌فرض عمل کرده و در زمان اجرا با داده‌های پایگاه‌داده جایگزین می‌شوند. همچنین می‌توانید وضعیت اجرای چک‌لیست امنیت/عملیات را از مسیر `/dashboard/admin/operations` به‌روزرسانی و مستند کنید تا پیش‌نیازهای انتشار دنبال شود. در محیط‌های تولیدی می‌توان مقدار JSON هر سرویس را در متغیرهای `INTEGRATIONS__*` قرار داد تا هنگام راه‌اندازی به صورت خودکار در پایگاه‌داده ذخیره و فعال شود؛ مثال برای Gemini:
> ```json
> {
>   "providerKey": "gemini",
>   "displayName": "Google Gemini Production",
>   "configuration": {
>     "ApiKey": "prod-key",
>     "BaseUrl": "https://generativelanguage.googleapis.com",
>     "BusinessPlanModel": "gemini-1.5-pro"
>   },
>   "isActive": true
> }
> ```

> **نکته:** پس از ورود به سیستم با نقش ادمین، می‌توانید مقادیر مربوط به Gemini، درگاه پرداخت، SMTP و پنل پیامکی را از مسیر `/dashboard/admin/integrations` وارد یا ویرایش کنید؛ در این صورت مقادیر جدول بالا به‌عنوان مقدار پیش‌فرض عمل کرده و در زمان اجرا با داده‌های پایگاه‌داده جایگزین می‌شوند. همچنین می‌توانید وضعیت اجرای چک‌لیست امنیت/عملیات را از مسیر `/dashboard/admin/operations` به‌روزرسانی و مستند کنید تا پیش‌نیازهای انتشار دنبال شود. در محیط‌های تولیدی می‌توان مقدار JSON هر سرویس را در متغیرهای `INTEGRATIONS__*` قرار داد تا هنگام راه‌اندازی به صورت خودکار در پایگاه‌داده ذخیره و فعال شود؛ مثال برای Gemini:
> ```json
> {
>   "providerKey": "gemini",
>   "displayName": "Google Gemini Production",
>   "configuration": {
>     "ApiKey": "prod-key",
>     "BaseUrl": "https://generativelanguage.googleapis.com",
>     "BusinessPlanModel": "gemini-1.5-pro"
>   },
>   "isActive": true
> }
> ```

> **نکته:** پس از ورود به سیستم با نقش ادمین، می‌توانید مقادیر مربوط به Gemini، درگاه پرداخت، SMTP و پنل پیامکی را از مسیر `/dashboard/admin/integrations` وارد یا ویرایش کنید؛ در این صورت مقادیر جدول بالا به‌عنوان مقدار پیش‌فرض عمل کرده و در زمان اجرا با داده‌های پایگاه‌داده جایگزین می‌شوند. همچنین می‌توانید وضعیت اجرای چک‌لیست امنیت/عملیات را از مسیر `/dashboard/admin/operations` به‌روزرسانی و مستند کنید تا پیش‌نیازهای انتشار دنبال شود. در محیط‌های تولیدی می‌توان مقدار JSON هر سرویس را در متغیرهای `INTEGRATIONS__*` قرار داد تا هنگام راه‌اندازی به صورت خودکار در پایگاه‌داده ذخیره و فعال شود؛ مثال برای Gemini:
> ```json
> {
>   "providerKey": "gemini",
>   "displayName": "Google Gemini Production",
>   "configuration": {
>     "ApiKey": "prod-key",
>     "BaseUrl": "https://generativelanguage.googleapis.com",
>     "BusinessPlanModel": "gemini-1.5-pro"
>   },
>   "isActive": true
> }
> ```

## راه‌اندازی بک‌اند

1. پیش‌نیازها: [Docker اختیاری برای PostgreSQL]، [.NET 8 SDK](https://dotnet.microsoft.com/download)، و سرویس PostgreSQL در حال اجرا.
   > **توجه:** فایل `backend/global.json` نسخهٔ SDK را روی شاخهٔ 8.0 قفل می‌کند؛ بنابراین حتماً `dotnet --list-sdks` باید شامل نسخه‌ای از سری 8.0 باشد، در غیر این صورت دستورات `dotnet` اجرا نخواهند شد.
2. ایجاد پایگاه‌داده (نمونه):
   ```bash
   docker run --name nabteams-postgres -e POSTGRES_PASSWORD=nabteams -e POSTGRES_USER=nabteams -e POSTGRES_DB=nabteams -p 5432:5432 -d postgres:15
   ```
3. اعمال مایگریشن‌های EF Core (یک بار برای هر محیط):
   ```bash
   cd backend/NabTeams/src/Web
   dotnet ef database update
   ```
4. اجرای سرویس:
   ```bash
   dotnet restore
   dotnet run --urls http://localhost:5000
   ```
5. اولین اجرا مهاجرت EF Core را اعمال و منابع اولیهٔ دانش و ثبت‌نام را Seed می‌کند. مستندات Swagger در `http://localhost:5000/swagger` در دسترس است.

### مهم‌ترین APIها

- `POST /api/chat/{role}/messages` — ارسال پیام، پایش Gemini و اعمال امتیاز انضباطی.
- `GET /api/chat/{role}/messages` — دریافت پیام‌های منتشرشده کانال (پیام‌های مسدود‌شده نمایش داده نمی‌شوند).
- `GET /api/discipline/{role}/me` — مشاهده وضعیت امتیاز انضباطی کاربر جاری.
- `POST /api/appeals` — ثبت اعتراض نسبت به پیام مسدود شده.
- `GET /api/appeals` — فهرست اعتراض‌های کاربر.
- `GET /api/appeals/admin` و `POST /api/appeals/{id}/decision` — بررسی و تصمیم‌گیری توسط ادمین.
- `POST /api/registrations/participants/{id}/finalize` — تأیید نهایی ثبت‌نام توسط تیم.
- `POST /api/registrations/participants/{id}/approve` — تایید ادمین، ایجاد لینک پرداخت و ارسال اعلان.
- `POST /api/registrations/participants/{id}/payments/complete` — ثبت موفقیت پرداخت و ارسال اعلان تایید.
- `POST /api/registrations/participants/{id}/analysis` — ارسال طرح کسب‌وکار برای تحلیل هوش مصنوعی و دریافت امتیاز و پیشنهادها.
- `GET /api/registrations/participants/{id}/analysis` — فهرست تحلیل‌های انجام‌شده برای تیم.
- `GET /api/events` — مشاهده فهرست رویدادهای فعال و وضعیت تسک‌منیجر (عمومی).
- `POST/PUT/DELETE /api/events` — مدیریت رویداد و فعال/غیرفعال‌سازی تسک‌منیجر (فقط ادمین).
- `GET /api/registrations/participants/{id}/tasks` — فهرست تسک‌های تیم (نیازمند فعال بودن تسک‌منیجر برای رویداد).
- `POST /api/registrations/participants/{id}/tasks` — ایجاد تسک جدید برای تیم شرکت‌کننده.
- `PUT /api/registrations/participants/{id}/tasks/{taskId}` — ویرایش عنوان/توضیح/سررسید.
- `PATCH /api/registrations/participants/{id}/tasks/{taskId}/status` — تغییر وضعیت تسک.
- `DELETE /api/registrations/participants/{id}/tasks/{taskId}` — حذف تسک انتخاب‌شده.
- `POST /api/registrations/participants/{id}/tasks/ai-advice` — دریافت پیشنهادهای هوش مصنوعی برای برنامه‌ریزی و تسک‌های بعدی.
- `POST /api/support/query` — پاسخ دانشی (RAG) با Gemini.
- `POST /api/registrations/participants/uploads` — بارگذاری مدارک ثبت‌نام و دریافت لینک قابل‌دانلود.
- `GET/POST/DELETE /api/knowledge-base` — مدیریت منابع دانش توسط ادمین.
- `GET /api/moderation/{role}/logs` — مشاهده لاگ‌های پایش (ادمین).
- `GET /api/admin/operations-checklist` و `PUT /api/admin/operations-checklist/{id}` — مشاهده و بروزرسانی وضعیت چک‌لیست امنیت و عملیات.
- `POST /api/admin/operations-checklist/{id}/artifact` — بارگذاری فایل مستند (گزارش امنیت، تست بار، سیاست حریم خصوصی و ...) برای هر آیتم چک‌لیست.
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
- **تسک‌منیجر هوشمند برای تیم‌ها:** رویدادها می‌توانند قابلیت مدیریت تسک و پیشنهادهای Gemini را فعال کنند؛ شرکت‌کننده پس از نهایی‌سازی ثبت‌نام می‌تواند از داشبورد «مدیریت تسک تیم» برای ثبت، ویرایش، تغییر وضعیت و دریافت توصیه‌های هوش مصنوعی استفاده کند.
- **احراز هویت و مجوز:** بک‌اند با JWT Bearer از SSO سازمانی پشتیبانی می‌کند. مسیرهای ادمین با Policy `AdminOnly` محافظت شده‌اند. فرانت‌اند از NextAuth (OIDC) با امکان ورود آزمایشی بهره می‌گیرد.
- **چرخهٔ انضباطی کامل:** هر پیام پایش شده، امتیاز منفی/مثبت را به‌روزرسانی می‌کند. وضعیت کاربر و تاریخچه رویدادها قابل استعلام است.
- **ماژول اعتراض:** کاربران می‌توانند برای پیام‌های مسدود‌شده اعتراض ثبت کنند، و ادمین‌ها با فیلتر نقش/وضعیت بررسی و تایید/رد را ثبت می‌کنند.
- **پشتیبانی دانشی RAG:** Gemini پاسخ را بر اساس منابع مدیریت‌شده توسط ادمین (به همراه Confidence و منابع استناد) تولید می‌کند. در نبود API Key الگوریتم رتبه‌بندی داخلی استفاده می‌شود.
- **فرانت‌اند راست‌به‌چپ با نقش‌محوری:** داشبورد Next.js شامل مدیریت نقش، چت، پشتیبانی، مدیریت دانش و اعتراض‌ها است. جلسات NextAuth نقش‌های کاربر را به صورت Context در اختیار تمام اجزا قرار می‌دهد.
- **هاردنینگ امنیتی و عملیاتی:** هدرهای امنیتی پیش‌فرض، فشرده‌سازی پاسخ، متریک‌های Prometheus و Logهای HTTP برای مانیتورینگ و مقابله با تهدیدات فعال هستند. دستورالعمل کامل در `docs/operations-runbook.md` موجود است.
- **درگاه پرداخت واقعی:** سرویس گردش‌کار ثبت‌نام از IdPay (یا هر درگاه RESTful سازگار) لینک پرداخت دریافت می‌کند، وضعیت تراکنش را نگه می‌دارد و اعلان ایمیل/پیامک واقعی ارسال می‌کند.
- **خلاصه نهایی خودکار:** هنگام تأیید نهایی تیم، سرویس `RegistrationSummaryBuilder` با استفاده از مدارک، لینک‌ها، اعضا، اعلان‌ها و نتایج تحلیل هوش مصنوعی فایل متنی تجمیعی تولید می‌کند تا برای داوران و سرمایه‌گذاران قابل دانلود باشد.
- **تحلیل بیزینس‌پلن با Gemini:** تیم‌ها می‌توانند خلاصه طرح خود را ارسال کنند تا هوش مصنوعی خلاصه، نقاط قوت/ریسک و پیشنهادهای بهبود را به همراه امتیاز بازگرداند؛ نتایج در داشبورد ثبت‌نام نمایش داده می‌شود.
- **پنل تنظیمات یکپارچه‌سازی:** ادمین بدون نیاز به ویرایش فایل‌های پیکربندی می‌تواند کلیدهای Gemini، تنظیمات درگاه پرداخت، SMTP و پنل پیامکی را در داشبورد وارد، ویرایش و فعال کند؛ مقادیر ذخیره‌شده بلافاصله توسط سرویس‌ها استفاده می‌شوند.
- **چک‌لیست امنیت و عملیات:** صفحهٔ `/dashboard/admin/operations` وضعیت انجام اسکن امنیتی، مانیتورینگ، آموزش اپراتورها، تست بار و الزامات حریم خصوصی را در قالب آیتم‌های قابل‌ویرایش ثبت می‌کند تا تیم DevOps مسیر انتشار را مستندسازی کند.
- **لاگ ممیزی ادمین:** صفحهٔ `/dashboard/admin/audit` رویدادهای مدیریتی (ویرایش کلیدها، تایید ثبت‌نام، تغییر وضعیت چک‌لیست) را از پایگاه‌داده می‌خواند و امکان فیلتر بر اساس نوع موجودیت یا شناسه فراهم می‌کند تا تیم امنیت بتواند فعالیت‌ها را بررسی کند.

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

## راهنمای استفاده

برای آشنایی با نحوه ثبت‌نام، پیگیری وضعیت، پرداخت و دریافت تحلیل هوش مصنوعی از طرح کسب‌وکار، فایل [`docs/user-guide.md`](docs/user-guide.md) را مطالعه کنید.

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
- **گردش‌کار رویداد و تسک‌منیجر AI**
  1. ادمین از مسیر `/dashboard/admin/events` رویداد جدید می‌سازد یا وضعیت «فعال‌سازی تسک‌منیجر هوش مصنوعی» را برای رویداد موجود تغییر می‌دهد.
  2. شرکت‌کننده در هنگام ثبت‌نام رویداد خود را انتخاب می‌کند؛ پس از تأیید نهایی، لینک «مدیریت تسک‌های تیم» در صفحهٔ موفقیت و داشبورد ثبت‌نام نمایش داده می‌شود.
  3. صفحهٔ `/dashboard/tasks` امکان جست‌وجوی شناسهٔ ثبت‌نام، ثبت تسک‌های جدید، ویرایش/حذف، تغییر وضعیت (Todo, InProgress, Blocked, Completed, Archived) و درخواست پیشنهاد هوش مصنوعی را فراهم می‌کند.
  4. درخواست پیشنهاد هوش مصنوعی (`/tasks/ai-advice`) خلاصه‌ای از وضعیت فعلی تیم، تسک‌های پیشنهادی، ریسک‌ها و گام‌های بعدی را بازمی‌گرداند تا هد تیم بتواند برنامه‌ریزی را بر اساس تحلیل Gemini انجام دهد.
  5. اگر رویدادی تسک‌منیجر فعال نداشته باشد، API پاسخ خطای معنادار برمی‌گرداند و UI پیام راهنما نمایش می‌دهد تا برگزارکننده قابلیت را فعال کند.

