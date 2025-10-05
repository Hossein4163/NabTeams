'use client';

import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import { useSession } from 'next-auth/react';
import {
  AuditLogFilters,
  AuditLogRecord,
  listAuditLogs
} from '../../../../lib/api';

const entityOptions = [
  { value: '', label: 'همه موجودیت‌ها' },
  { value: 'IntegrationSetting', label: 'تنظیمات یکپارچه‌سازی' },
  { value: 'OperationsChecklistItemEntity', label: 'چک‌لیست عملیات' },
  { value: 'ParticipantRegistration', label: 'ثبت‌نام شرکت‌کننده' }
];

export default function AuditLogsPage() {
  const { data: session } = useSession();
  const accessToken = session?.accessToken;
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

  const isAdmin = (session?.user?.roles ?? []).includes('admin');

  const [filters, setFilters] = useState<AuditLogFilters>({ take: 50 });
  const [logs, setLogs] = useState<AuditLogRecord[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadLogs = useCallback(async () => {
    if (!isAdmin) {
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const result = await listAuditLogs(auth, filters);
      setLogs(result);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setLoading(false);
    }
  }, [auth, filters, isAdmin]);

  useEffect(() => {
    loadLogs();
  }, [loadLogs]);

  if (!isAdmin) {
    return (
      <div className="space-y-4">
        <h1 className="text-2xl font-semibold">دسترسی محدود</h1>
        <p className="text-sm text-slate-300">
          فقط ادمین‌ها می‌توانند لاگ‌های ممیزی را مشاهده کنند. لطفاً با حساب دارای نقش ادمین وارد شوید.
        </p>
      </div>
    );
  }

  const handleFilterChange = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    const entityType = (formData.get('entityType') as string) ?? '';
    const entityId = ((formData.get('entityId') as string) ?? '').trim();
    const takeRaw = formData.get('take') as string;
    const nextFilters: AuditLogFilters = {
      entityType: entityType || undefined,
      entityId: entityId || undefined,
      take: takeRaw ? Number(takeRaw) : undefined,
      skip: 0
    };
    setFilters(nextFilters);
  };

  const formatMetadata = (metadata: unknown) => {
    if (!metadata) {
      return '-';
    }

    try {
      return JSON.stringify(metadata, null, 2);
    } catch {
      return String(metadata);
    }
  };

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <h1 className="text-3xl font-semibold">📜 لاگ ممیزی سامانه</h1>
        <p className="text-sm text-slate-300">
          این صفحه کلیهٔ فعالیت‌های مدیریتی (ویرایش کلیدها، به‌روزرسانی چک‌لیست، تایید ثبت‌نام و ...) را برای ممیزی امنیتی نگه‌داری
          می‌کند. برای کاهش حجم خروجی می‌توانید موجودیت یا شناسه خاصی را فیلتر کنید.
        </p>
      </div>

      <form onSubmit={handleFilterChange} className="grid gap-4 rounded-xl border border-slate-800 bg-slate-900/60 p-4 md:grid-cols-4">
        <label className="flex flex-col text-sm text-slate-200">
          نوع موجودیت
          <select
            name="entityType"
            defaultValue={filters.entityType ?? ''}
            className="mt-1 rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
          >
            {entityOptions.map((option) => (
              <option key={option.value || 'all'} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col text-sm text-slate-200">
          شناسه موجودیت
          <input
            name="entityId"
            defaultValue={filters.entityId ?? ''}
            className="mt-1 rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
            placeholder="GUID یا شناسه دلخواه"
          />
        </label>
        <label className="flex flex-col text-sm text-slate-200">
          تعداد ردیف
          <input
            name="take"
            type="number"
            min={10}
            max={200}
            step={10}
            defaultValue={filters.take ?? 50}
            className="mt-1 rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
          />
        </label>
        <div className="flex items-end">
          <button
            type="submit"
            className="w-full rounded-md bg-emerald-500 px-4 py-2 text-sm font-medium text-emerald-950"
            disabled={loading}
          >
            {loading ? 'در حال بارگذاری...' : 'اعمال فیلتر'}
          </button>
        </div>
      </form>

      {error && <div className="rounded-md border border-red-500 bg-red-500/10 p-3 text-sm text-red-200">{error}</div>}

      <div className="overflow-auto rounded-xl border border-slate-800 bg-slate-900/60">
        <table className="min-w-full divide-y divide-slate-800 text-sm">
          <thead className="bg-slate-900/80 text-slate-200">
            <tr>
              <th className="px-4 py-3 text-left font-medium">زمان</th>
              <th className="px-4 py-3 text-left font-medium">کاربر</th>
              <th className="px-4 py-3 text-left font-medium">اقدام</th>
              <th className="px-4 py-3 text-left font-medium">موجودیت</th>
              <th className="px-4 py-3 text-left font-medium">شناسه</th>
              <th className="px-4 py-3 text-left font-medium">جزییات</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-800 text-slate-100">
            {logs.length === 0 && !loading ? (
              <tr>
                <td colSpan={6} className="px-4 py-6 text-center text-slate-400">
                  هیچ رکورد ممیزی برای بازه انتخابی یافت نشد.
                </td>
              </tr>
            ) : (
              logs.map((log) => (
                <tr key={log.id} className="align-top">
                  <td className="px-4 py-3 whitespace-nowrap text-slate-300">
                    {new Date(log.createdAt).toLocaleString('fa-IR')}
                  </td>
                  <td className="px-4 py-3">
                    <div className="font-medium">{log.actorName || log.actorId}</div>
                    <div className="text-xs text-slate-400">{log.actorId}</div>
                  </td>
                  <td className="px-4 py-3 whitespace-pre-wrap text-slate-200">{log.action}</td>
                  <td className="px-4 py-3 text-slate-300">{log.entityType}</td>
                  <td className="px-4 py-3 text-xs text-slate-400 break-all">{log.entityId}</td>
                  <td className="px-4 py-3 font-mono text-xs text-slate-300 whitespace-pre-wrap">
                    {formatMetadata(log.metadata)}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
