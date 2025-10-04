'use client';

import { useState } from 'react';
import { askSupport, Role, SupportAnswer } from '../lib/api';
import { useRole } from '../lib/use-role';

export function SupportPanel() {
  const role = useRole() as Role;
  const [userId, setUserId] = useState('user-001');
  const [question, setQuestion] = useState('قوانین عمومی رویداد چیست؟');
  const [answer, setAnswer] = useState<SupportAnswer | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!question.trim()) {
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const result = await askSupport({
        userId,
        role,
        question
      });
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
        <div className="grid gap-4 md:grid-cols-2">
          <label className="flex flex-col gap-2 text-sm">
            <span className="text-slate-400">شناسه کاربر</span>
            <input
              value={userId}
              onChange={(event) => setUserId(event.target.value)}
              className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
            />
          </label>
          <label className="flex flex-col gap-2 text-sm">
            <span className="text-slate-400">نقش فعال</span>
            <input value={role} disabled className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100" />
          </label>
        </div>
        <label className="flex flex-col gap-2 text-sm">
          <span className="text-slate-400">سوال شما</span>
          <textarea
            value={question}
            onChange={(event) => setQuestion(event.target.value)}
            rows={4}
            className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-3 text-sm text-slate-100"
          />
        </label>
        <button
          type="submit"
          disabled={loading}
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
    </div>
  );
}
