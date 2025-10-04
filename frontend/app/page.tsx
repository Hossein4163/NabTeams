import Link from 'next/link';
import { RoleSwitcher } from '../components/role-switcher';

export default function HomePage() {
  return (
    <div className="space-y-8">
      <header className="space-y-3">
        <h1 className="text-3xl font-semibold">داشبورد تعاملی نب‌تیمز</h1>
        <p className="text-slate-300 max-w-3xl">
          این نسخه‌ی اولیه شامل چت گلوبال نقش‌محور با پایش محتوایی هوشمند و چت پشتیبانی مبتنی بر دانش ادمین است.
          برای تست می‌توانید نقش دلخواه را انتخاب کرده و پیام ارسال کنید.
        </p>
        <RoleSwitcher />
      </header>

      <section className="grid gap-4 md:grid-cols-2">
        <Link
          href="/(dashboard)/global-chat"
          className="rounded-xl border border-slate-800 bg-slate-900/60 p-6 hover:border-slate-600 transition"
        >
          <h2 className="text-2xl font-medium mb-2">👥 چت گلوبال</h2>
          <p className="text-sm text-slate-300">
            پیام‌های نقش خود را در کانال اختصاصی ارسال کنید و نتیجه‌ی پایش محتوایی را به صورت لحظه‌ای ببینید.
          </p>
        </Link>
        <Link
          href="/(dashboard)/support"
          className="rounded-xl border border-slate-800 bg-slate-900/60 p-6 hover:border-slate-600 transition"
        >
          <h2 className="text-2xl font-medium mb-2">🛟 پشتیبانی دانشی</h2>
          <p className="text-sm text-slate-300">
            پرسش‌های خود را مطرح کنید تا Gemini بر اساس دانش‌پایه‌ی ادمین پاسخ دهد و در صورت نیاز انتقال به اپراتور انجام شود.
          </p>
        </Link>
      </section>
    </div>
  );
}
