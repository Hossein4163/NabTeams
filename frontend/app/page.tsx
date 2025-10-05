'use client';

import Link from 'next/link';
import { signIn, signOut, useSession } from 'next-auth/react';
import { RoleSwitcher } from '../components/role-switcher';

export default function HomePage() {
  const { data: session, status } = useSession();
  const isAuthenticated = status === 'authenticated';
  const roles = (session?.user?.roles ?? []) as string[];
  const isAdmin = roles.includes('admin');

  return (
    <div className="space-y-8">
      <header className="space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <h1 className="text-3xl font-semibold">داشبورد تعاملی نب‌تیمز</h1>
            <p className="text-slate-300 max-w-3xl">
              این نسخه شامل چت گلوبال نقش‌محور با پایش محتوایی Gemini، چت پشتیبانی مبتنی بر دانش ادمین و مدیریت اعتراض‌ها است. برای استفاده، ابتدا از طریق SSO وارد شوید.
            </p>
          </div>
          <div className="flex flex-col items-end gap-2 text-sm text-slate-300">
            {isAuthenticated ? (
              <>
                <span>{session?.user?.email ?? session?.user?.name}</span>
                <button
                  onClick={() => signOut()}
                  className="rounded-lg border border-slate-600 px-3 py-1 text-xs text-slate-200"
                >
                  خروج از حساب
                </button>
              </>
            ) : (
              <button
                onClick={() => signIn()}
                className="rounded-lg bg-emerald-500 px-4 py-2 text-sm font-medium text-emerald-950"
              >
                ورود با SSO یا ورود آزمایشی
              </button>
            )}
          </div>
        </div>
        {isAuthenticated && <RoleSwitcher />}
      </header>

      <section className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        <DashboardLink
          href="/register"
          title="📝 ثبت‌نام رویداد"
          description="جریان‌های ثبت‌نام شرکت‌کننده، داور و سرمایه‌گذار را تکمیل کنید و مدارک/لینک‌های تیم را بارگذاری کنید."
        />
        <DashboardLink
          href="/dashboard/registration"
          title="📊 پیگیری وضعیت ثبت‌نام"
          description="با وارد کردن کد پیگیری، وضعیت تایید، اعلان‌های ارسالی و جزئیات پرداخت مرحله دوم را مشاهده کنید."
        />
        {isAdmin && (
          <DashboardLink
            href="/dashboard/admin/integrations"
            title="🔐 پیکربندی کلیدها و درگاه‌ها"
            description="کلیدهای Gemini، پنل پیامکی و درگاه‌های پرداخت را بدون ویرایش فایل پیکربندی از طریق داشبورد تنظیم کنید."
          />
        )}
        <DashboardLink
          href="/(dashboard)/global-chat"
          title="👥 چت گلوبال"
          description="پیام‌های نقش خود را در کانال اختصاصی ارسال کنید و نتیجه‌ی پایش محتوایی را به صورت لحظه‌ای ببینید."
        />
        <DashboardLink
          href="/(dashboard)/support"
          title="🛟 پشتیبانی دانشی"
          description="پرسش‌های خود را مطرح کنید تا Gemini بر اساس دانش‌پایه‌ی ادمین پاسخ دهد و در صورت نیاز انتقال به اپراتور انجام شود."
        />
        <DashboardLink
          href="/(dashboard)/knowledge-base"
          title="📚 مدیریت دانش ادمین"
          description="منابع پاسخ‌گویی را سازمان‌دهی کنید، مخاطبان هر منبع را تعیین و تاثیر تغییرات را بلافاصله در چت پشتیبانی مشاهده کنید."
        />
        <DashboardLink
          href="/appeals"
          title="⚖️ اعتراض‌های انضباطی"
          description="درخواست بازبینی برای پیام‌های مسدودشده ثبت کنید یا وضعیت اعتراض‌های قبلی را دنبال کنید."
        />
      </section>
    </div>
  );
}

function DashboardLink({
  href,
  title,
  description
}: {
  href: string;
  title: string;
  description: string;
}) {
  return (
    <Link href={href} className="rounded-xl border border-slate-800 bg-slate-900/60 p-6 transition hover:border-slate-600">
      <h2 className="text-2xl font-medium mb-2">{title}</h2>
      <p className="text-sm text-slate-300">{description}</p>
    </Link>
  );
}
