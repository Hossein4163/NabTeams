'use client';

import { useEffect, useMemo, useState } from 'react';
import { useSession } from 'next-auth/react';
import {
  Appeal,
  AppealStatus,
  createAppeal,
  fetchAppeals,
  queryAppeals,
  resolveAppeal,
  Role
} from '../lib/api';
import { useRole } from '../lib/use-role';

export function AppealsPanel() {
  const { data: session, status } = useSession();
  const accessToken = session?.accessToken;
  const isAuthenticated = status === 'authenticated';
  const currentRole = useRole();
  const isAdmin = (session?.user?.roles ?? []).includes('admin');
  const sessionUser = session?.user
    ? {
        id: session.user.id ?? session.user.email ?? null,
        email: session.user.email ?? null,
        name: session.user.name ?? null,
        roles: session.user.roles ?? null
      }
    : undefined;
  const auth = useMemo(
    () => ({ accessToken, sessionUser }),
    [accessToken, sessionUser?.id, sessionUser?.email, sessionUser?.name, sessionUser?.roles?.join(',')]
  );
  const hasAuth = Boolean(accessToken) || Boolean(sessionUser?.id);
  const [messageId, setMessageId] = useState('');
  const [reason, setReason] = useState('');
  const [appeals, setAppeals] = useState<Appeal[]>([]);
  const [adminAppeals, setAdminAppeals] = useState<Appeal[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [adminFilters, setAdminFilters] = useState<{ role?: Role; status?: AppealStatus }>({ status: 'Pending' });

  useEffect(() => {
    if (!isAuthenticated || !hasAuth) {
      setAppeals([]);
      return;
    }
    let cancelled = false;
    async function load() {
      try {
        const data = await fetchAppeals(auth);
        if (!cancelled) {
          setAppeals(data);
        }
      } catch (err) {
        if (!cancelled) {
          setError((err as Error).message);
        }
      }
    }
    load();
    return () => {
      cancelled = true;
    };
  }, [auth, accessToken, hasAuth, isAuthenticated]);

  useEffect(() => {
    if (!isAdmin || !hasAuth) {
      setAdminAppeals([]);
      return;
    }
    let cancelled = false;
    async function loadAdmin() {
      try {
        const data = await queryAppeals(auth, adminFilters);
        if (!cancelled) {
          setAdminAppeals(data);
        }
      } catch (err) {
        if (!cancelled) {
          setError((err as Error).message);
        }
      }
    }
    loadAdmin();
    return () => {
      cancelled = true;
    };
  }, [auth, accessToken, hasAuth, isAdmin, adminFilters]);

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!isAuthenticated || !hasAuth || !messageId.trim() || !reason.trim()) {
      setError('شناسه پیام و دلیل اعتراض الزامی است.');
      return;
    }
    setLoading(true);
    setError(null);
    setSuccess(null);
    try {
      const saved = await createAppeal({ messageId: messageId.trim(), reason: reason.trim() }, auth);
      setAppeals((prev) => [saved, ...prev]);
      setSuccess('اعتراض ثبت شد و در انتظار بررسی است.');
      setMessageId('');
      setReason('');
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setLoading(false);
    }
  };

  const handleResolve = async (id: string, status: AppealStatus) => {
    if (!isAuthenticated || !hasAuth) {
      return;
    }
    try {
      const resolved = await resolveAppeal(id, { status }, auth);
      setAdminAppeals((prev) => prev.map((item) => (item.id === id ? resolved : item)));
      setAppeals((prev) => prev.map((item) => (item.id === id ? resolved : item)));
    } catch (err) {
      setError((err as Error).message);
    }
  };

  return (
    <div className="space-y-8">
      <section className="space-y-4 rounded-2xl border border-slate-800 bg-slate-900/70 p-5">
        <header className="space-y-1">
          <h1 className="text-2xl font-semibold">ثبت اعتراض جدید</h1>
          <p className="text-slate-400 text-sm">
            شناسه پیام را از پاسخ چت یا لاگ انضباطی دریافت کرده و دلیل اعتراض را به طور مختصر بیان کنید. اعتراض برای ادمین ارسال می‌شود.
          </p>
        </header>
        <form onSubmit={handleSubmit} className="space-y-3">
          <div className="grid gap-4 md:grid-cols-2">
            <label className="flex flex-col gap-2 text-sm">
              <span className="text-slate-300">شناسه پیام</span>
              <input
                value={messageId}
                onChange={(event) => setMessageId(event.target.value)}
                className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
                placeholder="00000000-0000-0000-0000-000000000000"
                disabled={!hasAuth}
              />
            </label>
            <label className="flex flex-col gap-2 text-sm">
              <span className="text-slate-300">نقش فعال</span>
              <input
                value={currentRole}
                disabled
                className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
              />
            </label>
          </div>
          <label className="flex flex-col gap-2 text-sm">
            <span className="text-slate-300">دلیل اعتراض</span>
            <textarea
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              rows={4}
              className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-3 text-sm text-slate-100"
              disabled={!hasAuth}
            />
          </label>
          <button
            type="submit"
            disabled={loading || !hasAuth}
            className="rounded-lg bg-emerald-500 px-4 py-2 text-sm font-medium text-emerald-950 disabled:opacity-60"
          >
            {loading ? 'در حال ارسال...' : 'ثبت اعتراض'}
          </button>
          {success && <p className="text-sm text-emerald-300">{success}</p>}
          {error && <p className="text-sm text-rose-300">{error}</p>}
        </form>
        {(!isAuthenticated || !hasAuth) && (
          <p className="text-sm text-rose-200">برای ثبت اعتراض ابتدا وارد حساب کاربری شوید.</p>
        )}
      </section>

      <section className="space-y-3 rounded-2xl border border-slate-800 bg-slate-900/70 p-5">
        <header className="space-y-1">
          <h2 className="text-xl font-semibold">اعتراض‌های من</h2>
          <p className="text-sm text-slate-400">وضعیت اعتراض‌های ثبت‌شده توسط شما در این بخش نمایش داده می‌شود.</p>
        </header>
        {appeals.length === 0 ? (
          <p className="text-sm text-slate-400">اعتراضی ثبت نشده است.</p>
        ) : (
          <ul className="space-y-4">
            {appeals.map((appeal) => (
              <li key={appeal.id} className="rounded-xl border border-slate-800 bg-slate-950/60 p-4 text-sm text-slate-200">
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <span>پیام: {appeal.messageId}</span>
                  <StatusBadge status={appeal.status} />
                </div>
                <p className="mt-2 text-slate-300">دلیل: {appeal.reason}</p>
                {appeal.resolutionNotes && (
                  <p className="mt-2 text-slate-400">یادداشت ادمین: {appeal.resolutionNotes}</p>
                )}
              </li>
            ))}
          </ul>
        )}
      </section>

      {isAdmin && (
        <section className="space-y-4 rounded-2xl border border-slate-800 bg-slate-900/70 p-5">
          <header className="space-y-2">
            <h2 className="text-xl font-semibold">پنل بررسی ادمین</h2>
            <div className="flex flex-wrap gap-3 text-sm">
              <select
                value={adminFilters.role ?? ''}
                onChange={(event) =>
                  setAdminFilters((prev) => ({ ...prev, role: event.target.value ? (event.target.value as Role) : undefined }))
                }
                className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
              >
                <option value="">همه نقش‌ها</option>
                <option value="participant">شرکت‌کننده</option>
                <option value="mentor">منتور</option>
                <option value="judge">داور</option>
                <option value="investor">سرمایه‌گذار</option>
                <option value="admin">ادمین</option>
              </select>
              <select
                value={adminFilters.status ?? ''}
                onChange={(event) =>
                  setAdminFilters((prev) => ({
                    ...prev,
                    status: event.target.value ? (event.target.value as AppealStatus) : undefined
                  }))
                }
                className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
              >
                <option value="">همه وضعیت‌ها</option>
                <option value="Pending">در انتظار بررسی</option>
                <option value="Accepted">پذیرفته شد</option>
                <option value="Rejected">رد شد</option>
              </select>
            </div>
          </header>
          {adminAppeals.length === 0 ? (
            <p className="text-sm text-slate-400">اعتراضی برای این فیلتر یافت نشد.</p>
          ) : (
            <ul className="space-y-4">
              {adminAppeals.map((appeal) => (
                <li key={appeal.id} className="rounded-xl border border-slate-800 bg-slate-950/60 p-4 text-sm text-slate-200">
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <div>
                      <p>پیام: {appeal.messageId}</p>
                      <p className="text-slate-400">کاربر: {appeal.userId}</p>
                    </div>
                    <StatusBadge status={appeal.status} />
                  </div>
                  <p className="mt-2 text-slate-300">دلیل کاربر: {appeal.reason}</p>
                  <div className="mt-3 flex gap-2">
                    <button
                      onClick={() => handleResolve(appeal.id, 'Accepted')}
                      className="rounded-lg border border-emerald-600 px-3 py-1 text-xs text-emerald-200"
                    >
                      تایید اعتراض
                    </button>
                    <button
                      onClick={() => handleResolve(appeal.id, 'Rejected')}
                      className="rounded-lg border border-rose-600 px-3 py-1 text-xs text-rose-200"
                    >
                      رد اعتراض
                    </button>
                  </div>
                  {appeal.reviewedBy && (
                    <p className="mt-2 text-xs text-slate-500">
                      بررسی توسط: {appeal.reviewedBy} — {appeal.reviewedAt && new Date(appeal.reviewedAt).toLocaleString('fa-IR')}
                    </p>
                  )}
                </li>
              ))}
            </ul>
          )}
        </section>
      )}
    </div>
  );
}

function StatusBadge({ status }: { status: AppealStatus }) {
  const styles: Record<AppealStatus, string> = {
    Pending: 'bg-amber-900/40 text-amber-200',
    Accepted: 'bg-emerald-900/40 text-emerald-200',
    Rejected: 'bg-rose-900/40 text-rose-200'
  };
  const labels: Record<AppealStatus, string> = {
    Pending: 'در انتظار بررسی',
    Accepted: 'پذیرفته شد',
    Rejected: 'رد شد'
  };
  return <span className={`rounded-full px-2 py-1 text-xs ${styles[status]}`}>{labels[status]}</span>;
}
