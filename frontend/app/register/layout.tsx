import type { ReactNode } from 'react';
import Link from 'next/link';

export default function RegisterLayout({ children }: { children: ReactNode }) {
  return (
    <div className="mx-auto flex w-full max-w-5xl flex-col gap-10 py-10">
      <header className="space-y-3 text-center">
        <h1 className="text-3xl font-semibold">فرآیند ثبت‌نام رویداد</h1>
        <p className="text-slate-300">
          از این بخش می‌توانید اطلاعات شرکت‌کنندگان، داوران و سرمایه‌گذاران را وارد کنید، مدارک تیم را بارگذاری کنید و در انتها
          پیش‌نمایش نهایی را ببینید.
        </p>
        <p className="text-sm text-slate-400">
          برای بازگشت به داشبورد اصلی می‌توانید از لینک
          <Link href="/" className="mx-1 text-emerald-400 hover:underline">
            صفحه اصلی
          </Link>
          استفاده کنید.
        </p>
      </header>
      <main className="rounded-2xl border border-slate-800 bg-slate-900/60 p-6 shadow-xl shadow-emerald-500/5">
        {children}
      </main>
    </div>
  );
}
