'use client';

import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import { useSession } from 'next-auth/react';
import {
  EventRequest,
  EventResponse,
  createEvent,
  deleteEvent,
  listEvents,
  updateEvent
} from '../../../../lib/api';

interface EventFormState {
  id?: string;
  name: string;
  description: string;
  startsAt: string;
  endsAt: string;
  aiTaskManagerEnabled: boolean;
}

const emptyForm: EventFormState = {
  name: '',
  description: '',
  startsAt: '',
  endsAt: '',
  aiTaskManagerEnabled: true
};

function toDateTimeLocal(value?: string | null): string {
  if (!value) {
    return '';
  }
  const date = new Date(value);
  date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
  return date.toISOString().slice(0, 16);
}

function toIsoString(value: string): string | null {
  if (!value.trim()) {
    return null;
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return null;
  }
  return date.toISOString();
}

export default function AdminEventsPage() {
  const { data: session } = useSession();
  const accessToken = session?.accessToken;
  const sessionUser = useMemo(() => {
    if (!session?.user) {
      return undefined;
    }

    return {
      id: session.user.id ?? session.user.email ?? null,
      email: session.user.email ?? null,
      name: session.user.name ?? null,
      roles: session.user.roles ?? null
    };
  }, [session?.user]);

  const auth = useMemo(
    () => ({ accessToken, sessionUser }),
    [accessToken, sessionUser]
  );

  const isAdmin = useMemo(() => (session?.user?.roles ?? []).includes('admin'), [session?.user?.roles?.join(',')]);

  const [events, setEvents] = useState<EventResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [form, setForm] = useState<EventFormState>(emptyForm);

  const loadEvents = useCallback(async () => {
    if (!isAdmin) {
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const data = await listEvents(auth);
      setEvents(data);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setLoading(false);
    }
  }, [auth, isAdmin]);

  useEffect(() => {
    void loadEvents();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [loadEvents]);

  const handleEdit = (event: EventResponse) => {
    setForm({
      id: event.id,
      name: event.name,
      description: event.description ?? '',
      startsAt: toDateTimeLocal(event.startsAt ?? undefined),
      endsAt: toDateTimeLocal(event.endsAt ?? undefined),
      aiTaskManagerEnabled: event.aiTaskManagerEnabled
    });
    setSuccess(null);
    setError(null);
  };

  const handleReset = () => {
    setForm(emptyForm);
    setSuccess(null);
    setError(null);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!form.name.trim()) {
      setError('نام رویداد الزامی است.');
      return;
    }

    setSaving(true);
    setError(null);
    try {
      const payload: EventRequest = {
        name: form.name.trim(),
        description: form.description.trim() ? form.description.trim() : null,
        startsAt: toIsoString(form.startsAt),
        endsAt: toIsoString(form.endsAt),
        aiTaskManagerEnabled: form.aiTaskManagerEnabled
      };

      const saved = form.id
        ? await updateEvent(form.id, payload, auth)
        : await createEvent(payload, auth);

      await loadEvents();
      setForm({
        id: saved.id,
        name: saved.name,
        description: saved.description ?? '',
        startsAt: toDateTimeLocal(saved.startsAt ?? undefined),
        endsAt: toDateTimeLocal(saved.endsAt ?? undefined),
        aiTaskManagerEnabled: saved.aiTaskManagerEnabled
      });
      setSuccess('رویداد با موفقیت ذخیره شد.');
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setSaving(false);
    }
  };

  const handleToggle = async (event: EventResponse) => {
    setSaving(true);
    setError(null);
    try {
      const payload: EventRequest = {
        name: event.name,
        description: event.description,
        startsAt: event.startsAt,
        endsAt: event.endsAt,
        aiTaskManagerEnabled: !event.aiTaskManagerEnabled
      };
      await updateEvent(event.id, payload, auth);
      await loadEvents();
      setSuccess(`وضعیت تسک‌منیجر برای «${event.name}» به‌روزرسانی شد.`);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('آیا از حذف این رویداد مطمئن هستید؟')) {
      return;
    }

    setSaving(true);
    setError(null);
    try {
      await deleteEvent(id, auth);
      await loadEvents();
      if (form.id === id) {
        handleReset();
      }
      setSuccess('رویداد حذف شد.');
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setSaving(false);
    }
  };

  if (!isAdmin) {
    return (
      <div className="space-y-4">
        <div className="rounded-2xl border border-amber-500/40 bg-amber-500/10 p-6 text-sm text-amber-100">
          دسترسی به مدیریت رویداد فقط برای ادمین فعال است. لطفاً با تیم پشتیبانی هماهنگ کنید یا{' '}
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
      <header className="space-y-2">
        <h1 className="text-2xl font-semibold text-slate-100">مدیریت رویدادها و تسک‌منیجر</h1>
        <p className="text-sm text-slate-300">
          در این بخش می‌توانید رویدادهای فعال را تعریف کنید، بازه زمانی آن‌ها را مشخص و قابلیت تسک‌منیجر هوش مصنوعی را برای هر
          رویداد فعال یا غیرفعال نمایید.
        </p>
      </header>

      {error && <div className="rounded-lg border border-red-500/40 bg-red-500/10 p-3 text-sm text-red-200">{error}</div>}
      {success && <div className="rounded-lg border border-emerald-500/40 bg-emerald-500/10 p-3 text-sm text-emerald-200">{success}</div>}

      <form onSubmit={handleSubmit} className="space-y-4 rounded-xl border border-slate-800 bg-slate-950/60 p-6">
        <h2 className="text-lg font-medium text-slate-100">{form.id ? 'ویرایش رویداد' : 'رویداد جدید'}</h2>
        <div className="grid gap-4 md:grid-cols-2">
          <label className="space-y-1 text-sm text-slate-200">
            <span>نام رویداد</span>
            <input
              type="text"
              value={form.name}
              onChange={(event) => setForm((prev) => ({ ...prev, name: event.target.value }))}
              className="w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 focus:border-emerald-500 focus:outline-none"
              required
            />
          </label>
          <label className="space-y-1 text-sm text-slate-200">
            <span>تاریخ شروع</span>
            <input
              type="datetime-local"
              value={form.startsAt}
              onChange={(event) => setForm((prev) => ({ ...prev, startsAt: event.target.value }))}
              className="w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 focus:border-emerald-500 focus:outline-none"
            />
          </label>
          <label className="space-y-1 text-sm text-slate-200">
            <span>تاریخ پایان</span>
            <input
              type="datetime-local"
              value={form.endsAt}
              onChange={(event) => setForm((prev) => ({ ...prev, endsAt: event.target.value }))}
              className="w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 focus:border-emerald-500 focus:outline-none"
            />
          </label>
          <label className="space-y-1 text-sm text-slate-200 md:col-span-2">
            <span>توضیحات</span>
            <textarea
              value={form.description}
              onChange={(event) => setForm((prev) => ({ ...prev, description: event.target.value }))}
              rows={3}
              className="w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-slate-100 focus:border-emerald-500 focus:outline-none"
            />
          </label>
          <label className="flex items-center gap-2 text-sm text-slate-200">
            <input
              type="checkbox"
              checked={form.aiTaskManagerEnabled}
              onChange={(event) => setForm((prev) => ({ ...prev, aiTaskManagerEnabled: event.target.checked }))}
              className="h-4 w-4 rounded border border-slate-600 bg-slate-900"
            />
            تسک‌منیجر هوش مصنوعی فعال باشد
          </label>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <button
            type="submit"
            disabled={saving}
            className="rounded-lg bg-emerald-500 px-4 py-2 text-sm font-medium text-emerald-950 transition hover:bg-emerald-400 disabled:opacity-50"
          >
            {saving ? 'در حال ذخیره...' : form.id ? 'به‌روزرسانی رویداد' : 'ایجاد رویداد'}
          </button>
          {form.id && (
            <button
              type="button"
              onClick={handleReset}
              className="rounded-lg border border-slate-700 px-4 py-2 text-sm text-slate-200 hover:bg-slate-900"
            >
              انصراف از ویرایش
            </button>
          )}
        </div>
      </form>

      <section className="space-y-3 rounded-xl border border-slate-800 bg-slate-950/50 p-6">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-medium text-slate-100">رویدادهای ثبت‌شده</h2>
          {loading && <span className="text-xs text-slate-400">در حال بارگذاری...</span>}
        </div>
        {events.length === 0 ? (
          <p className="text-sm text-slate-300">رویدادی ثبت نشده است. اولین رویداد را ایجاد کنید.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-800 text-sm">
              <thead>
                <tr className="text-slate-300">
                  <th className="px-3 py-2 text-right">نام</th>
                  <th className="px-3 py-2 text-right">بازه زمانی</th>
                  <th className="px-3 py-2 text-right">تسک‌منیجر AI</th>
                  <th className="px-3 py-2 text-right">تسک نمونه</th>
                  <th className="px-3 py-2 text-right">اقدامات</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800 text-slate-200">
                {events
                  .slice()
                  .sort((a, b) => (a.startsAt ?? '').localeCompare(b.startsAt ?? ''))
                  .map((event) => (
                    <tr key={event.id}>
                      <td className="px-3 py-2 font-medium">{event.name}</td>
                      <td className="px-3 py-2 text-xs text-slate-400">
                        {event.startsAt
                          ? `${new Date(event.startsAt).toLocaleDateString('fa-IR')} تا ${
                              event.endsAt ? new Date(event.endsAt).toLocaleDateString('fa-IR') : 'نامشخص'
                            }`
                          : '—'}
                      </td>
                      <td className="px-3 py-2">
                        <span
                          className={`inline-flex items-center rounded-full px-2 py-1 text-xs ${
                            event.aiTaskManagerEnabled
                              ? 'bg-emerald-500/10 text-emerald-200'
                              : 'bg-slate-800 text-slate-300'
                          }`}
                        >
                          {event.aiTaskManagerEnabled ? 'فعال' : 'غیرفعال'}
                        </span>
                      </td>
                      <td className="px-3 py-2 text-xs text-slate-300">
                        {event.sampleTasks.length > 0 ? (
                          <ul className="space-y-1">
                            {event.sampleTasks.map((task, index) => (
                              <li key={`${event.id}-${index}`}>{task.title}</li>
                            ))}
                          </ul>
                        ) : (
                          '—'
                        )}
                      </td>
                      <td className="px-3 py-2">
                        <div className="flex flex-wrap items-center gap-2">
                          <button
                            className="rounded border border-slate-700 px-3 py-1 text-xs text-slate-200 hover:bg-slate-900"
                            onClick={() => handleEdit(event)}
                          >
                            ویرایش
                          </button>
                          <button
                            className="rounded border border-emerald-600 px-3 py-1 text-xs text-emerald-200 hover:bg-emerald-600/20"
                            onClick={() => handleToggle(event)}
                            disabled={saving}
                          >
                            {event.aiTaskManagerEnabled ? 'غیرفعال کردن AI' : 'فعال‌سازی AI'}
                          </button>
                          <button
                            className="rounded border border-red-600 px-3 py-1 text-xs text-red-300 hover:bg-red-600/20"
                            onClick={() => handleDelete(event.id)}
                            disabled={saving}
                          >
                            حذف
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </div>
  );
}
