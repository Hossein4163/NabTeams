'use client';

import { useEffect, useState } from 'react';
import { fetchMessages, MessageModel, Role, sendMessage } from '../lib/api';
import { useRole } from '../lib/use-role';

const roleLabels: Record<Role, string> = {
  participant: 'شرکت‌کننده',
  judge: 'داور',
  mentor: 'منتور',
  investor: 'سرمایه‌گذار',
  admin: 'ادمین'
};

export function ChatPanel() {
  const role = useRole() as Role;
  const [userId, setUserId] = useState('user-001');
  const [content, setContent] = useState('');
  const [messages, setMessages] = useState<MessageModel[]>([]);
  const [loading, setLoading] = useState(false);
  const [feedback, setFeedback] = useState<string | null>(null);

  const loadMessages = async () => {
    try {
      const data = await fetchMessages(role);
      setMessages(data);
    } catch (error) {
      setFeedback((error as Error).message);
    }
  };

  useEffect(() => {
    loadMessages();
    const interval = setInterval(loadMessages, 5000);
    return () => clearInterval(interval);
  }, [role]);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!content.trim()) {
      return;
    }
    setLoading(true);
    setFeedback(null);
    try {
      const response = await sendMessage(role, { userId, content });
      setFeedback(
        response.rateLimitMessage ??
          `${response.moderationNotes ?? ''} (ریسک: ${response.moderationRisk}, امتیاز منفی: ${response.penaltyPoints})`
      );
      setContent('');
      await loadMessages();
    } catch (error) {
      setFeedback((error as Error).message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="space-y-6">
      <header className="space-y-1">
        <h1 className="text-2xl font-semibold">کانال {roleLabels[role]}</h1>
        <p className="text-slate-400 text-sm">
          پیام‌ها پیش از انتشار توسط موتور Gemini ارزیابی می‌شوند. خروجی شامل وضعیت، امتیاز ریسک و هشدارها است.
        </p>
      </header>

      <div className="rounded-2xl border border-slate-800 bg-slate-900/70">
        <div className="max-h-[420px] overflow-y-auto divide-y divide-slate-800/60">
          {messages.length === 0 ? (
            <p className="p-6 text-sm text-slate-400">هنوز پیامی ارسال نشده است.</p>
          ) : (
            messages.map((message) => (
              <article key={message.id} className="p-5 space-y-2">
                <div className="flex justify-between text-xs text-slate-500">
                  <span>کاربر: {message.senderUserId}</span>
                  <span>{new Date(message.createdAt).toLocaleString('fa-IR')}</span>
                </div>
                <p className="text-sm leading-6 text-slate-100 whitespace-pre-wrap">{message.content}</p>
                <footer className="flex flex-wrap gap-2 text-xs">
                  <StatusBadge status={message.status} />
                  <span className="rounded-full bg-slate-800 px-2 py-1">
                    ریسک: {(message.moderationRisk * 100).toFixed(0)}%
                  </span>
                  {message.moderationTags.map((tag) => (
                    <span key={tag} className="rounded-full bg-indigo-900/40 px-2 py-1 text-indigo-200">
                      #{tag}
                    </span>
                  ))}
                  {message.penaltyPoints !== 0 && (
                    <span className="rounded-full bg-rose-900/40 px-2 py-1 text-rose-200">امتیاز منفی: {message.penaltyPoints}</span>
                  )}
                </footer>
              </article>
            ))
          )}
        </div>
        <form onSubmit={handleSubmit} className="border-t border-slate-800 p-5 space-y-3">
          <div className="flex flex-wrap gap-4 text-sm">
            <label className="flex flex-col gap-1">
              <span className="text-slate-400">شناسه کاربر</span>
              <input
                value={userId}
                onChange={(event) => setUserId(event.target.value)}
                className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
              />
            </label>
          </div>
          <textarea
            value={content}
            onChange={(event) => setContent(event.target.value)}
            className="w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-3 text-sm text-slate-100"
            placeholder="پیام خود را بنویسید..."
            rows={3}
          />
          <div className="flex items-center justify-between text-xs text-slate-400">
            <span>حداکثر 20 پیام در 5 دقیقه برای نقش شرکت‌کننده مجاز است.</span>
            <button
              type="submit"
              disabled={loading}
              className="rounded-lg bg-emerald-500 px-4 py-2 text-sm font-medium text-emerald-950 disabled:opacity-60"
            >
              {loading ? 'در حال ارسال...' : 'ارسال پیام'}
            </button>
          </div>
        </form>
      </div>

      {feedback && <p className="text-sm text-amber-300">{feedback}</p>}
    </div>
  );
}

function StatusBadge({ status }: { status: MessageModel['status'] }) {
  const styles: Record<MessageModel['status'], string> = {
    Published: 'bg-emerald-900/50 text-emerald-200',
    Held: 'bg-amber-900/40 text-amber-200',
    Blocked: 'bg-rose-900/50 text-rose-100'
  };
  const labels: Record<MessageModel['status'], string> = {
    Published: 'منتشر شد',
    Held: 'در انتظار بررسی',
    Blocked: 'مسدود شد'
  };

  return <span className={`rounded-full px-2 py-1 text-xs ${styles[status]}`}>{labels[status]}</span>;
}
