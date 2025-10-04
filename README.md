# NabTeams Conversational Modules

این مخزن شامل پیاده‌سازی اولیه‌ی بک‌اند ASP.NET Core و فرانت‌اند Next.js برای قابلیت‌های «چت گلوبال نقش‌محور با پایش Gemini» و «چت پشتیبانی دانشی» است.

## ساختار

```
backend/           # وب‌سرویس ASP.NET Core (Minimal API + Controllers)
frontend/          # اپ Next.js با App Router
implementation_plan.md  # سند طراحی و تحلیل قبلی
```

## راه‌اندازی بک‌اند

1. نصب پیش‌نیازها: [.NET 8 SDK](https://dotnet.microsoft.com/download).
2. اجرای دستورات:
   ```bash
   cd backend/NabTeams.Api
   dotnet restore
   dotnet run --urls http://localhost:5000
   ```
3. مستندات Swagger در مسیر `http://localhost:5000/swagger` در دسترس است.

### نقاط کلیدی API

- `POST /api/chat/{role}/messages` — ارسال پیام و دریافت نتیجه‌ی پایش.
- `GET /api/chat/{role}/messages` — دریافت پیام‌های منتشر شده در کانال نقش.
- `GET /api/moderation/{role}/logs` — مشاهده‌ی لاگ‌های پایش اخیر.
- `GET /api/discipline/{role}/{userId}` — وضعیت امتیاز انضباطی کاربر.
- `POST /api/support/query` — پرسش از چت پشتیبانی دانشی.

## راه‌اندازی فرانت‌اند

1. نصب [Node.js 18+](https://nodejs.org/).
2. اجرای دستورات:
   ```bash
   cd frontend
   npm install
   npm run dev
   ```
3. وب‌اپ در `http://localhost:3000` با فرض اجرای بک‌اند روی `http://localhost:5000` در دسترس خواهد بود.

> برای تغییر آدرس سرویس بک‌اند متغیر محیطی `NEXT_PUBLIC_API_URL` را تنظیم کنید.

## قابلیت‌های مهم

- **پایش محتوایی هم‌زمان:** سرویس `GeminiModerationService` پیام را ارزیابی کرده و بر اساس سطح ریسک خروجی Publish/Hold/Block می‌دهد.
- **نرخ‌دهی و امتیاز منفی:** Rate Limiter پیام‌های پشت سر هم را محدود کرده و امتیاز منفی در `InMemoryUserDisciplineStore` ذخیره می‌شود.
- **گزارش‌دهی:** لاگ‌ها و امتیازات از طریق API قابل دسترسی است.
- **پشتیبانی مبتنی بر دانش:** مولفه‌ی `SupportResponder` با روش شباهت واژگانی ساده پاسخ را از منابع آماده می‌سازد و میزان اطمینان را اعلام می‌کند.
- **فرانت‌اند راست‌به‌چپ:** رابط کاربری داشبورد، چت و پشتیبانی با واکنش‌گرایی و نمایش وضعیت پایش طراحی شده است.

## گام‌های بعدی پیشنهادی

- جایگزینی سرویس‌های شبیه‌سازی‌شده با اتصال واقعی به Google Gemini (Moderation و RAG).
- اتصال به پایگاه‌داده پایدار (SQL/NoSQL) به جای ذخیره‌سازی در حافظه.
- افزودن احراز هویت Single Sign-On و مدیریت نشست کاربر.
- توسعه‌ی ماژول اعتراض (Appeal) و مدیریت دانش در پنل ادمین.
