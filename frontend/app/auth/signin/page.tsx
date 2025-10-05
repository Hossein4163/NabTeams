'use client';

import { signIn } from 'next-auth/react';

export default function SignInPage() {
  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-6 text-center">
      <h1 className="text-3xl font-semibold text-slate-100">ورود به سامانه NabTeams</h1>
      <p className="max-w-md text-sm text-slate-400">
        برای دسترسی به چت گلوبال نقش‌محور و پشتیبانی دانشی، لطفاً از SSO سازمانی استفاده کنید. در محیط‌های آزمایشی می‌توانید از ورود آزمایشی نیز بهره ببرید.
      </p>
      <div className="flex flex-col gap-3">
        <button
          onClick={() => signIn('nabteams-sso')}
          className="rounded-lg bg-emerald-500 px-6 py-3 text-sm font-semibold text-emerald-950 shadow-lg shadow-emerald-500/20"
        >
          ورود با SSO سازمانی
        </button>
        <button
          onClick={() => signIn('dev-login')}
          className="rounded-lg border border-slate-600 px-6 py-3 text-sm font-semibold text-slate-200"
        >
          ورود آزمایشی
        </button>
      </div>
    </div>
  );
}
