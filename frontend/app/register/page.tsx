import Link from 'next/link';

const registrationFlows = [
  {
    href: '/register/participant',
    title: '👤 ثبت‌نام شرکت‌کننده',
    description:
      'اطلاعات سرپرست تیم، اعضا، مدارک و لینک‌های مرتبط با پروژه را مرحله به مرحله کامل کنید و در پایان جمع‌بندی را ببینید.'
  },
  {
    href: '/register/judge',
    title: '⚖️ ثبت‌نام داور',
    description: 'مشخصات فردی، تحصیلات و حوزه فعالیت را ثبت کنید تا تیم اجرایی بتواند دعوت‌نامه را ارسال کند.'
  },
  {
    href: '/register/investor',
    title: '💼 ثبت‌نام سرمایه‌گذار',
    description: 'حوزه‌های علاقه‌مندی و اطلاعات تماس خود را وارد کنید تا پروژه‌های متناسب برای شما فیلتر شوند.'
  }
];

export default function RegisterIndexPage() {
  return (
    <div className="grid gap-4 md:grid-cols-3">
      {registrationFlows.map((flow) => (
        <Link
          key={flow.href}
          href={flow.href}
          className="flex h-full flex-col justify-between rounded-xl border border-slate-800 bg-slate-900/60 p-6 transition hover:border-emerald-500/60"
        >
          <div className="space-y-3">
            <h2 className="text-2xl font-semibold">{flow.title}</h2>
            <p className="text-sm text-slate-300">{flow.description}</p>
          </div>
          <span className="mt-6 inline-flex items-center justify-start text-sm font-medium text-emerald-400">
            شروع فرآیند
          </span>
        </Link>
      ))}
    </div>
  );
}
