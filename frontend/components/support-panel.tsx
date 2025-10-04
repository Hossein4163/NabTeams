'use client';

import { useMemo, useState } from 'react';
import { useSession } from 'next-auth/react';
import Link from 'next/link';
import { askSupport, MAX_SUPPORT_QUESTION_LENGTH, Role, SupportAnswer } from '../lib/api';
import { useRole } from '../lib/use-role';

export function SupportPanel() {
  const role = useRole() as Role;
  const { data: session, status } = useSession();
  const accessToken = session?.accessToken;
  const sessionUser = session?.user
    ? {
        id: session.user.id ?? session.user.email ?? null,
        email: session.user.email ?? null,
        name: session.user.name ?? null,
        roles: session.user.roles ?? null
      }
    : undefined;
  const isAuthenticated = status === 'authenticated';
  const [question, setQuestion] = useState('قوانین عمومی رویداد چیست؟');
  const [answer, setAnswer] = useState<SupportAnswer | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const hasAuth = Boolean(accessToken) || Boolean(sessionUser?.id);
  const trimmedQuestion = question.trim();
  const questionLength = trimmedQuestion.length;
  const canSubmit =
    isAuthenticated && hasAuth && questionLength > 0 && questionLength <= MAX_SUPPORT_QUESTION_LENGTH && !loading;
  const auth = useMemo(
    () => ({ accessToken, sessionUser }),
    [accessToken, sessionUser?.id, sessionUser?.email, sessionUser?.name, sessionUser?.roles?.join(',')]
  );

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!trimmedQuestion || !isAuthenticated || !hasAuth || questionLength > MAX_SUPPORT_QUESTION_LENGTH) {
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const result = await askSupport({ role, question: trimmedQuestion }, auth);
      setAnswer(result);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      <header className="space-y-1">
        <h1 className="text-2xl font-semibold">پشتیبانی هوشمند</h1>
        <p className="text-slate-400 text-sm">
          پاسخ‌ها از دانش‌پایه‌ی منتشر شده توسط ادمین و منابع داخلی تامین می‌شود. در صورت اطمینان پایین، پیشنهاد تماس انسانی ارائه می‌گردد.
        </p>
      </header>

      <form onSubmit={handleSubmit} className="space-y-4 rounded-2xl border border-slate-800 bg-slate-900/70 p-5">
        <div className="grid gap-4 md:grid-cols-2 text-sm text-slate-400">
          <div>
            <span>کاربر جاری</span>
            <p className="mt-1 rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100">
              {session?.user?.email ?? session?.user?.name ?? '---'}
            </p>
          </div>
          <div>
            <span>نقش فعال</span>
            <p className="mt-1 rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100">{role}</p>
          </div>
        </div>
        <label className="flex flex-col gap-2 text-sm">
          <span className="text-slate-400">سوال شما</span>
          <textarea
            value={question}
            onChange={(event) => setQuestion(event.target.value)}
            rows={4}
            className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-3 text-sm text-slate-100"
            disabled={!isAuthenticated || !hasAuth}
          />
          <span className={`text-xs ${questionLength > MAX_SUPPORT_QUESTION_LENGTH ? 'text-rose-300' : 'text-slate-500'}`}>
            {questionLength.toLocaleString()} / {MAX_SUPPORT_QUESTION_LENGTH.toLocaleString()} نویسه مجاز
          </span>
        </label>
        <button
          type="submit"
          disabled={!canSubmit}
          className="rounded-lg bg-sky-500 px-4 py-2 text-sm font-medium text-sky-950 disabled:opacity-60"
        >
          {loading ? 'در حال تحلیل...' : 'ارسال سوال'}
        </button>
      </form>

      {answer && (
        <section className="space-y-3 rounded-2xl border border-slate-800 bg-slate-900/70 p-5">
          <header className="flex flex-wrap items-center justify-between gap-3 text-sm">
            <div className="flex items-center gap-2">
              <span className="rounded-full bg-emerald-900/40 px-2 py-1 text-emerald-200">
                اطمینان: {(answer.confidence * 100).toFixed(0)}%
              </span>
              {answer.escalateToHuman && (
                <span className="rounded-full bg-amber-900/40 px-2 py-1 text-amber-200">پیشنهاد ارتباط انسانی</span>
              )}
            </div>
            {answer.sources?.length > 0 && (
              <div className="text-xs text-slate-400">منابع: {answer.sources.join(', ')}</div>
            )}
          </header>
          <p className="leading-7 text-slate-100 whitespace-pre-wrap">{answer.answer}</p>
        </section>
      )}

      {error && <p className="text-sm text-rose-300">{error}</p>}
      {(!isAuthenticated || !hasAuth) && (
        <p className="text-sm text-rose-200">
          برای استفاده از پشتیبانی لازم است ابتدا{' '}
          <Link href="/auth/signin" className="underline">
            وارد سامانه شوید
          </Link>
          .
        </p>
      )}
    </div>
  );
}
