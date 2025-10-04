'use client';

import { HubConnection } from '@microsoft/signalr';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { useSession } from 'next-auth/react';
import Link from 'next/link';
import { fetchMessages, MessageModel, Role, sendMessage } from '../lib/api';
import { createChatConnection } from '../lib/chat-hub';
import { useRole } from '../lib/use-role';

const roleLabels: Record<Role, string> = {
  participant: 'شرکت‌کننده',
  judge: 'داور',
  mentor: 'منتور',
  investor: 'سرمایه‌گذار',
  admin: 'ادمین'
};

export function ChatPanel() {
  const role = useRole();
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
  const [content, setContent] = useState('');
  const [messages, setMessages] = useState<MessageModel[]>([]);
  const [loading, setLoading] = useState(false);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [appealId, setAppealId] = useState<string | null>(null);
  const [connectionState, setConnectionState] = useState<'disconnected' | 'connecting' | 'connected'>('disconnected');

  const hasAuth = Boolean(accessToken) || Boolean(sessionUser?.id);
  const canSend = isAuthenticated && hasAuth && content.trim().length > 0 && !loading;
  const auth = useMemo(
    () => ({ accessToken, sessionUser }),
    [accessToken, sessionUser?.id, sessionUser?.email, sessionUser?.name, sessionUser?.roles?.join(',')]
  );
  const authSignature = useMemo(() => {
    if (accessToken) {
      return `token:${accessToken}`;
    }
    if (!sessionUser) {
      return 'guest';
    }
    const descriptor = [sessionUser.id, sessionUser.email, sessionUser.name, (sessionUser.roles ?? []).join(',')]
      .filter(Boolean)
      .join('|');
    return `debug:${descriptor}`;
  }, [accessToken, sessionUser?.id, sessionUser?.email, sessionUser?.name, sessionUser?.roles?.join(',')]);

  const currentUserId = useMemo(
    () => sessionUser?.id ?? sessionUser?.email ?? sessionUser?.name ?? null,
    [sessionUser?.id, sessionUser?.email, sessionUser?.name]
  );

  const upsertMessage = useCallback(
    (incoming: MessageModel) => {
      setMessages((prev) => {
        const next = prev.filter((m) => m.id !== incoming.id);
        if (incoming.status !== 'Blocked') {
          next.push(incoming);
          next.sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
        }
        return next;
      });

      if (currentUserId && incoming.senderUserId === currentUserId) {
        setFeedback(
          `${incoming.moderationNotes ?? 'نتیجه بررسی در دسترس است.'} (ریسک: ${(incoming.moderationRisk * 100).toFixed(0)}%, امتیاز منفی: ${incoming.penaltyPoints})`
        );
        setAppealId(incoming.status === 'Blocked' ? incoming.id : null);
      }
    },
    [currentUserId]
  );

  useEffect(() => {
    setMessages([]);
    setFeedback(null);
    setAppealId(null);

    if (!hasAuth) {
      setConnectionState('disconnected');
      setHistoryLoading(false);
      return;
    }

    let active = true;
    let connection: HubConnection | null = null;

    const bootstrap = async () => {
      setConnectionState('connecting');
      setHistoryLoading(true);
      try {
        const history = await fetchMessages(role, auth);
        if (active) {
          setMessages(history);
        }
      } catch (error) {
        if (active) {
          setFeedback((error as Error).message);
        }
      } finally {
        if (active) {
          setHistoryLoading(false);
        }
      }

      try {
        connection = await createChatConnection(role, auth, upsertMessage);
        if (!active) {
          await connection.stop();
          return;
        }

        setConnectionState('connected');
        connection.onclose(() => setConnectionState('disconnected'));
        connection.onreconnecting(() => setConnectionState('connecting'));
        connection.onreconnected(() => setConnectionState('connected'));
      } catch (error) {
        if (active) {
          setConnectionState('disconnected');
          setFeedback((error as Error).message);
        }
      }
    };

    bootstrap();

    return () => {
      active = false;
      setConnectionState('disconnected');
      if (connection) {
        connection.off('MessageUpserted', upsertMessage);
        connection.stop().catch(() => undefined);
      }
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [role, hasAuth, authSignature, upsertMessage]);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!canSend) {
      return;
    }
    setLoading(true);
    setFeedback(null);
    setAppealId(null);
    try {
      const response = await sendMessage(role, content, auth);
      const placeholder: MessageModel = {
        id: response.messageId,
        channel: role,
        senderUserId: currentUserId ?? 'self',
        content,
        createdAt: new Date().toISOString(),
        status: response.status,
        moderationRisk: response.moderationRisk,
        moderationTags: response.moderationTags,
        moderationNotes: response.moderationNotes,
        penaltyPoints: response.penaltyPoints
      };
      upsertMessage(placeholder);
      setFeedback(
        response.rateLimitMessage ??
          `${response.moderationNotes ?? 'پیام برای بررسی ارسال شد.'} (ریسک: ${(response.moderationRisk * 100).toFixed(0)}%, امتیاز منفی: ${response.penaltyPoints})`
      );
      setContent('');
    } catch (error) {
      setFeedback((error as Error).message);
    } finally {
      setLoading(false);
    }
  };

  const appealBanner = useMemo(() => {
    if (!appealId) {
      return null;
    }
    return (
      <div className="rounded-xl border border-rose-500/40 bg-rose-500/10 p-4 text-sm text-rose-100">
        پیام اخیر شما مسدود شد. برای ثبت اعتراض می‌توانید شناسه <code className="font-mono">{appealId}</code> را در صفحه{' '}
        <Link href="/appeals" className="font-semibold text-rose-200 underline">
          مدیریت اعتراض‌ها
        </Link>{' '}
        وارد کنید.
      </div>
    );
  }, [appealId]);

  return (
    <div className="space-y-6">
      <header className="space-y-2">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-semibold">کانال {roleLabels[role]}</h1>
            <p className="text-slate-400 text-sm">
              پیام‌ها پیش از انتشار توسط موتور Gemini ارزیابی می‌شوند. خروجی شامل وضعیت، امتیاز ریسک و هشدارها است.
            </p>
          </div>
          <div className="text-right text-xs text-slate-400">
            <p>کاربر: {session?.user?.email ?? session?.user?.name ?? '---'}</p>
          </div>
        </div>
        {appealBanner}
      </header>

      <div className="rounded-2xl border border-slate-800 bg-slate-900/70">
        <div className="max-h-[420px] overflow-y-auto divide-y divide-slate-800/60">
          {historyLoading && messages.length === 0 ? (
            <p className="p-6 text-sm text-slate-400">در حال بارگذاری پیام‌های قبلی...</p>
          ) : messages.length === 0 ? (
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
                  <span className="rounded-full bg-slate-800 px-2 py-1">ریسک: {(message.moderationRisk * 100).toFixed(0)}%</span>
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
          <textarea
            value={content}
            onChange={(event) => setContent(event.target.value)}
            className="w-full rounded-lg border border-slate-700 bg-slate-950 px-3 py-3 text-sm text-slate-100"
            placeholder="پیام خود را بنویسید..."
            rows={3}
            disabled={!hasAuth}
          />
          <div className="flex items-center justify-between text-xs text-slate-400">
            <span>حداکثر 20 پیام در 5 دقیقه برای نقش شرکت‌کننده مجاز است.</span>
            <button
              type="submit"
              disabled={!canSend}
              className="rounded-lg bg-emerald-500 px-4 py-2 text-sm font-medium text-emerald-950 disabled:opacity-60"
            >
              {loading ? 'در حال ارسال...' : 'ارسال پیام'}
            </button>
          </div>
        </form>
      </div>

      {feedback && <p className="text-sm text-amber-300">{feedback}</p>}
      <p className="text-xs text-slate-500">
        وضعیت اتصال: {
          {
            connected: 'متصل',
            connecting: 'در حال اتصال...',
            disconnected: 'قطع'
          }[connectionState]
        }
      </p>
      {(!isAuthenticated || !hasAuth) && (
        <p className="text-sm text-rose-200">
          برای ارسال پیام لازم است ابتدا از طریق SSO وارد شوید.{' '}
          <Link href="/auth/signin" className="underline">
            صفحه ورود
          </Link>
        </p>
      )}
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
