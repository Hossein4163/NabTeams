'use client';

import { type ChangeEvent, type FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import { useSession } from 'next-auth/react';
import {
  listOperationsChecklist,
  OperationsChecklistItem,
  OperationsChecklistStatus,
  updateOperationsChecklistItem,
  uploadOperationsChecklistArtifact
} from '../../../../lib/api';

const statusOptions: Array<{ value: OperationsChecklistStatus; label: string }> = [
  { value: 'Pending', label: 'در انتظار' },
  { value: 'InProgress', label: 'در حال اجرا' },
  { value: 'Completed', label: 'تکمیل شده' }
];

type FormState = Record<
  string,
  {
    status: OperationsChecklistStatus;
    notes: string;
    artifactUrl: string;
  }
>;

export default function OperationsChecklistPage() {
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

  const [items, setItems] = useState<OperationsChecklistItem[]>([]);
  const [forms, setForms] = useState<FormState>({});
  const [loading, setLoading] = useState(false);
  const [savingId, setSavingId] = useState<string | null>(null);
  const [uploadingId, setUploadingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const loadItems = useCallback(async () => {
    if (!isAdmin) {
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const result = await listOperationsChecklist(auth);
      setItems(result);
      setForms(
        result.reduce<FormState>((acc, item) => {
          acc[item.id] = {
            status: item.status,
            notes: item.notes ?? '',
            artifactUrl: item.artifactUrl ?? ''
          };
          return acc;
        }, {})
      );
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setLoading(false);
    }
  }, [auth, isAdmin]);

  useEffect(() => {
    void loadItems();
  }, [loadItems]);

  const handleChange = (id: string, field: 'status' | 'notes' | 'artifactUrl', value: string) => {
    setForms((prev) => ({
      ...prev,
      [id]: {
        status: field === 'status' ? (value as OperationsChecklistStatus) : prev[id]?.status ?? 'Pending',
        notes: field === 'notes' ? value : prev[id]?.notes ?? '',
        artifactUrl: field === 'artifactUrl' ? value : prev[id]?.artifactUrl ?? ''
      }
    }));
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>, item: OperationsChecklistItem) => {
    event.preventDefault();
    const form = forms[item.id];
    if (!form) {
      return;
    }

    setSavingId(item.id);
    setError(null);
    setSuccess(null);
    try {
      const updated = await updateOperationsChecklistItem(
        item.id,
        {
          status: form.status,
          notes: form.notes?.trim() ? form.notes.trim() : undefined,
          artifactUrl: form.artifactUrl?.trim() ? form.artifactUrl.trim() : undefined
        },
        auth
      );

      setItems((prev) => prev.map((existing) => (existing.id === updated.id ? updated : existing)));
      setForms((prev) => ({
        ...prev,
        [item.id]: {
          status: updated.status,
          notes: updated.notes ?? '',
          artifactUrl: updated.artifactUrl ?? ''
        }
      }));
      setSuccess('آیتم با موفقیت به‌روزرسانی شد.');
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setSavingId(null);
    }
  };

  const handleArtifactChange = async (
    event: ChangeEvent<HTMLInputElement>,
    item: OperationsChecklistItem
  ) => {
    const file = event.target.files?.[0];
    event.target.value = '';
    if (!file) {
      return;
    }

    setUploadingId(item.id);
    setError(null);
    setSuccess(null);
    try {
      const updated = await uploadOperationsChecklistArtifact(item.id, file, auth);
      setItems((prev) => prev.map((existing) => (existing.id === updated.id ? updated : existing)));
      setForms((prev) => ({
        ...prev,
        [item.id]: {
          status: updated.status,
          notes: updated.notes ?? '',
          artifactUrl: updated.artifactUrl ?? ''
        }
      }));
      setSuccess('فایل مستند با موفقیت بارگذاری شد.');
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setUploadingId(null);
    }
  };

  if (!isAdmin) {
    return (
      <div className="space-y-4">
        <div className="rounded-2xl border border-amber-500/40 bg-amber-500/10 p-6 text-sm text-amber-100">
          دسترسی به چک‌لیست عملیات فقط برای ادمین فعال است. لطفاً با تیم پشتیبانی هماهنگ کنید یا{' '}
          <Link href="/auth/signin" className="underline">
            با حساب دیگری وارد شوید
          </Link>
          .
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="rounded-2xl border border-slate-700 bg-slate-900/60 p-6 shadow-xl">
        <h1 className="text-lg font-semibold text-white">چک‌لیست امنیت و عملیات</h1>
        <p className="mt-2 text-sm text-slate-300">
          وضعیت اجرای اسکن امنیتی، مانیتورینگ، آموزش اپراتورها و سایر پیش‌نیازهای انتشار را از این بخش پیگیری و به‌روزرسانی کنید.
        </p>
      </div>

      {error && (
        <div className="rounded-xl border border-red-500/40 bg-red-500/10 p-4 text-sm text-red-100">{error}</div>
      )}
      {success && (
        <div className="rounded-xl border border-emerald-500/40 bg-emerald-500/10 p-4 text-sm text-emerald-100">{success}</div>
      )}

      <div className="space-y-4">
        {loading && items.length === 0 ? (
          <div className="rounded-xl border border-slate-700 bg-slate-800/60 p-6 text-sm text-slate-300">در حال بارگذاری ...</div>
        ) : items.length === 0 ? (
          <div className="rounded-xl border border-slate-700 bg-slate-800/60 p-6 text-sm text-slate-300">
            آیتمی برای نمایش وجود ندارد. چک‌لیست به صورت خودکار هنگام اجرای مهاجرت‌ها مقداردهی می‌شود.
          </div>
        ) : (
          items.map((item) => {
            const form = forms[item.id] ?? { status: item.status, notes: item.notes ?? '', artifactUrl: item.artifactUrl ?? '' };
            const isUploading = uploadingId === item.id;
            return (
              <form
                key={item.id}
                onSubmit={(event) => void handleSubmit(event, item)}
                className="rounded-2xl border border-slate-700 bg-slate-900/70 p-6 shadow"
              >
                <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
                  <div className="space-y-2">
                    <div className="text-sm font-semibold text-white">{item.title}</div>
                    <div className="text-sm text-slate-300">{item.description}</div>
                    <div className="text-xs text-slate-400">دسته‌بندی: {item.category}</div>
                    {item.completedAt && (
                      <div className="text-xs text-emerald-300">تاریخ تکمیل: {new Date(item.completedAt).toLocaleString('fa-IR')}</div>
                    )}
                  </div>
                  <div className="flex w-full max-w-md flex-col gap-3">
                    <div className="flex flex-col gap-2">
                      <label className="text-xs font-medium text-slate-200" htmlFor={`artifact-${item.id}`}>
                        مستندات پشتیبان
                      </label>
                      <input
                        id={`artifact-${item.id}`}
                        type="file"
                        className="hidden"
                        onChange={(event) => void handleArtifactChange(event, item)}
                      />
                      <div className="flex flex-wrap items-center gap-3">
                        <label
                          htmlFor={`artifact-${item.id}`}
                          className={`inline-flex cursor-pointer items-center gap-2 rounded-lg border border-slate-600 px-3 py-2 text-xs font-medium transition hover:border-slate-400 hover:text-white ${
                            isUploading ? 'pointer-events-none opacity-60' : 'text-slate-200'
                          }`}
                        >
                          {isUploading ? 'در حال آپلود ...' : item.artifactUrl ? 'بارگذاری مجدد مستند' : 'بارگذاری مستند'}
                        </label>
                        {item.artifactUrl && (
                          <a
                            href={item.artifactUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="inline-flex items-center gap-1 text-xs text-emerald-300 hover:text-emerald-200"
                          >
                            مشاهده فایل
                          </a>
                        )}
                      </div>
                      <p className="text-xs text-slate-400">
                        فرمت‌های متداول (PDF، ZIP، تصویر، متن) پشتیبانی می‌شوند. حداکثر اندازهٔ هر فایل ۲۰ مگابایت است.
                      </p>
                    </div>
                    <label className="text-xs font-medium text-slate-200" htmlFor={`status-${item.id}`}>
                      وضعیت
                    </label>
                    <select
                      id={`status-${item.id}`}
                      className="rounded-lg border border-slate-600 bg-slate-800/80 px-3 py-2 text-sm text-white"
                      value={form.status}
                      onChange={(event) => handleChange(item.id, 'status', event.target.value)}
                    >
                      {statusOptions.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>

                    <label className="text-xs font-medium text-slate-200" htmlFor={`notes-${item.id}`}>
                      یادداشت / خلاصه اقدام
                    </label>
                    <textarea
                      id={`notes-${item.id}`}
                      className="min-h-[72px] rounded-lg border border-slate-600 bg-slate-800/80 px-3 py-2 text-sm text-white"
                      value={form.notes}
                      onChange={(event) => handleChange(item.id, 'notes', event.target.value)}
                    />

                    <label className="text-xs font-medium text-slate-200" htmlFor={`artifact-${item.id}`}>
                      لینک مستندات یا گزارش
                    </label>
                    <input
                      id={`artifact-${item.id}`}
                      type="url"
                      placeholder="https://..."
                      className="rounded-lg border border-slate-600 bg-slate-800/80 px-3 py-2 text-sm text-white"
                      value={form.artifactUrl}
                      onChange={(event) => handleChange(item.id, 'artifactUrl', event.target.value)}
                    />

                    <div className="flex items-center justify-end gap-3 pt-2">
                      <button
                        type="submit"
                        className="rounded-lg bg-emerald-500 px-4 py-2 text-sm font-semibold text-emerald-950 shadow disabled:cursor-not-allowed disabled:bg-emerald-500/50"
                        disabled={savingId === item.id}
                      >
                        {savingId === item.id ? 'در حال ذخیره...' : 'ذخیره تغییرات'}
                      </button>
                    </div>
                  </div>
                </div>
              </form>
            );
          })
        )}
      </div>
    </div>
  );
}
