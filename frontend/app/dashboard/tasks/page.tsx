'use client';

import { FormEvent, useEffect, useMemo, useState } from 'react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import {
  ParticipantRegistrationResponse,
  ParticipantTaskAdviceResult,
  ParticipantTaskModel,
  ParticipantTaskStatus,
  createParticipantTask,
  deleteParticipantTask,
  getParticipantRegistration,
  listParticipantTasks,
  requestTaskAdvice,
  updateParticipantTask,
  updateParticipantTaskStatus
} from '../../../lib/api';

const statusLabels: Record<ParticipantTaskStatus, string> = {
  Todo: 'در صف انجام',
  InProgress: 'در حال انجام',
  Blocked: 'مسدود',
  Completed: 'انجام شده',
  Archived: 'آرشیو'
};

const statusOptions: ParticipantTaskStatus[] = ['Todo', 'InProgress', 'Blocked', 'Completed', 'Archived'];

interface TaskFormState {
  title: string;
  description: string;
  assignedTo: string;
  dueAt: string;
}

const emptyForm: TaskFormState = {
  title: '',
  description: '',
  assignedTo: '',
  dueAt: ''
};

function toDateTimeLocal(value?: string | null): string {
  if (!value) {
    return '';
  }
  const date = new Date(value);
  date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
  return date.toISOString().slice(0, 16);
}

function formatDate(value?: string | null): string {
  if (!value) {
    return '—';
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '—';
  }
  return date.toLocaleString('fa-IR');
}

export default function ParticipantTasksDashboardPage() {
  const searchParams = useSearchParams();
  const initialId = searchParams.get('id') ?? '';

  const [lookupId, setLookupId] = useState(initialId);
  const [registration, setRegistration] = useState<ParticipantRegistrationResponse | null>(null);
  const [tasks, setTasks] = useState<ParticipantTaskModel[]>([]);
  const [loadingRegistration, setLoadingRegistration] = useState(false);
  const [tasksLoading, setTasksLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [taskError, setTaskError] = useState<string | null>(null);
  const [taskSuccess, setTaskSuccess] = useState<string | null>(null);
  const [form, setForm] = useState<TaskFormState>(emptyForm);
  const [editingTaskId, setEditingTaskId] = useState<string | null>(null);
  const [adviceContext, setAdviceContext] = useState('');
  const [adviceFocus, setAdviceFocus] = useState('');
  const [adviceLoading, setAdviceLoading] = useState(false);
  const [adviceError, setAdviceError] = useState<string | null>(null);
  const [adviceResult, setAdviceResult] = useState<ParticipantTaskAdviceResult | null>(null);

  const aiTaskManagerEnabled = registration?.event?.aiTaskManagerEnabled ?? false;
  const eventName = registration?.event?.name ?? '—';

  useEffect(() => {
    if (initialId) {
      void handleLookup(initialId);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initialId]);

  const sortedTasks = useMemo(() => {
    return tasks
      .slice()
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
  }, [tasks]);

  async function handleLookup(idOverride?: string) {
    const idToFetch = (idOverride ?? lookupId).trim();
    if (!idToFetch) {
      setError('شناسه ثبت‌نام را وارد کنید.');
      setRegistration(null);
      setTasks([]);
      return;
    }

    setLoadingRegistration(true);
    setError(null);
    setTaskError(null);
    setTaskSuccess(null);
    setAdviceError(null);
    setAdviceResult(null);
    try {
      const response = await getParticipantRegistration(idToFetch);
      setRegistration(response);
      setForm(emptyForm);
      setEditingTaskId(null);
      if (response.event?.aiTaskManagerEnabled) {
        await refreshTasks(response.id, true);
      } else {
        setTasks([]);
        setTaskError('قابلیت تسک‌منیجر هوش مصنوعی برای این رویداد فعال نشده است.');
      }
    } catch (err) {
      const message = err instanceof Error ? err.message : 'امکان بازیابی اطلاعات وجود ندارد.';
      setError(message);
      setRegistration(null);
      setTasks([]);
    } finally {
      setLoadingRegistration(false);
    }
  }

  async function refreshTasks(participantId: string, isEnabled: boolean) {
    if (!isEnabled) {
      setTasks([]);
      return;
    }

    setTasksLoading(true);
    setTaskError(null);
    try {
      const response = await listParticipantTasks(participantId);
      setTasks(response);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'امکان بارگذاری تسک‌ها وجود ندارد.';
      setTaskError(message);
      setTasks([]);
    } finally {
      setTasksLoading(false);
    }
  }

  function handleEditTask(task: ParticipantTaskModel) {
    setForm({
      title: task.title,
      description: task.description ?? '',
      assignedTo: task.assignedTo ?? '',
      dueAt: toDateTimeLocal(task.dueAt ?? undefined)
    });
    setEditingTaskId(task.id);
    setTaskSuccess(null);
    setTaskError(null);
  }

  function resetTaskForm() {
    setForm(emptyForm);
    setEditingTaskId(null);
  }

  async function handleTaskSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!registration) {
      setTaskError('ابتدا شناسه ثبت‌نام را جست‌وجو کنید.');
      return;
    }

    if (!aiTaskManagerEnabled) {
      setTaskError('این رویداد از تسک‌منیجر هوش مصنوعی پشتیبانی نمی‌کند.');
      return;
    }

    const trimmedTitle = form.title.trim();
    if (!trimmedTitle) {
      setTaskError('عنوان تسک الزامی است.');
      return;
    }

    setTasksLoading(true);
    setTaskError(null);
    setTaskSuccess(null);
    try {
      const payload = {
        eventId: registration.eventId,
        title: trimmedTitle,
        description: form.description.trim() || undefined,
        assignedTo: form.assignedTo.trim() || undefined,
        dueAt: form.dueAt ? new Date(form.dueAt).toISOString() : null
      };

      if (editingTaskId) {
        await updateParticipantTask(registration.id, editingTaskId, payload);
        setTaskSuccess('تسک با موفقیت به‌روزرسانی شد.');
      } else {
        await createParticipantTask(registration.id, payload);
        setTaskSuccess('تسک جدید ثبت شد.');
      }

      resetTaskForm();
      await refreshTasks(registration.id, true);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'ثبت یا ویرایش تسک با خطا روبه‌رو شد.';
      setTaskError(message);
    } finally {
      setTasksLoading(false);
    }
  }

  async function handleStatusChange(taskId: string, status: ParticipantTaskStatus) {
    if (!registration) {
      return;
    }

    setTasksLoading(true);
    setTaskError(null);
    setTaskSuccess(null);
    try {
      await updateParticipantTaskStatus(registration.id, taskId, { status });
      await refreshTasks(registration.id, true);
      setTaskSuccess('وضعیت تسک به‌روزرسانی شد.');
    } catch (err) {
      const message = err instanceof Error ? err.message : 'به‌روزرسانی وضعیت تسک با خطا روبه‌رو شد.';
      setTaskError(message);
    } finally {
      setTasksLoading(false);
    }
  }

  async function handleDeleteTask(taskId: string) {
    if (!registration) {
      return;
    }

    if (!confirm('آیا از حذف این تسک مطمئن هستید؟')) {
      return;
    }

    setTasksLoading(true);
    setTaskError(null);
    setTaskSuccess(null);
    try {
      await deleteParticipantTask(registration.id, taskId);
      await refreshTasks(registration.id, true);
      setTaskSuccess('تسک حذف شد.');
    } catch (err) {
      const message = err instanceof Error ? err.message : 'حذف تسک با خطا روبه‌رو شد.';
      setTaskError(message);
    } finally {
      setTasksLoading(false);
    }
  }

  async function handleAdviceRequest() {
    if (!registration) {
      setAdviceError('ابتدا شناسه ثبت‌نام را جست‌وجو کنید.');
      return;
    }

    if (!aiTaskManagerEnabled) {
      setAdviceError('این رویداد از پیشنهادهای هوش مصنوعی پشتیبانی نمی‌کند.');
      return;
    }

    setAdviceLoading(true);
    setAdviceError(null);
    setAdviceResult(null);
    try {
      const result = await requestTaskAdvice(registration.id, {
        context: adviceContext.trim() || undefined,
        focusArea: adviceFocus.trim() || undefined
      });
      setAdviceResult(result);
    } catch (err) {
      const message = err instanceof Error ? err.message : 'دریافت پیشنهادهای هوش مصنوعی با خطا روبه‌رو شد.';
      setAdviceError(message);
    } finally {
      setAdviceLoading(false);
    }
  }

  return (
    <div className="space-y-8">
      <header className="space-y-2">
        <h1 className="text-2xl font-semibold text-slate-100">تسک‌منیجر شرکت‌کنندگان</h1>
        <p className="text-sm text-slate-300">
          پس از فعال‌سازی این قابلیت برای رویداد شما، می‌توانید تسک‌های تیم را مدیریت کنید، وضعیت آن‌ها را تغییر دهید و از پیشنهادهای هوش مصنوعی Gemini برای برنامه‌ریزی بهتر استفاده کنید.
        </p>
      </header>

      <section className="space-y-4 rounded-xl border border-slate-800 bg-slate-950/50 p-6">
        <div className="flex flex-col gap-3 md:flex-row md:items-end">
          <label className="flex-1 text-sm text-slate-300">
            شناسه ثبت‌نام
            <input
              value={lookupId}
              onChange={(event) => setLookupId(event.target.value)}
              placeholder="مثلاً 58f3fc6f-0f73-4c81-8f5a-64f0d3d1bebf"
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-900/70 p-2 text-sm text-slate-100 placeholder:text-slate-500 focus:border-emerald-500 focus:outline-none"
            />
          </label>
          <button
            type="button"
            onClick={() => void handleLookup()}
            disabled={loadingRegistration}
            className="inline-flex items-center justify-center rounded-lg bg-emerald-500 px-4 py-2 text-sm font-medium text-emerald-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:bg-emerald-900/50 disabled:text-emerald-100/40"
          >
            {loadingRegistration ? 'در حال بررسی...' : 'جست‌وجو'}
          </button>
        </div>
        {error && <p className="rounded-lg border border-red-500/40 bg-red-500/10 p-3 text-sm text-red-200">{error}</p>}
      </section>

      {registration && (
        <section className="space-y-4 rounded-xl border border-slate-800 bg-slate-950/60 p-6">
          <div className="flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
            <div>
              <h2 className="text-lg font-semibold text-slate-100">تیم {registration.teamName}</h2>
              <p className="text-sm text-slate-300">
                سرپرست: {registration.headFirstName} {registration.headLastName} — شماره تماس: {registration.phoneNumber}
              </p>
            </div>
            <div className="text-xs text-slate-400">
              <p>رویداد: {eventName}</p>
              <p>وضعیت ثبت‌نام: {registration.status}</p>
            </div>
          </div>
          {!aiTaskManagerEnabled && (
            <p className="rounded-lg border border-amber-500/40 bg-amber-500/10 p-3 text-sm text-amber-100">
              ادمین هنوز تسک‌منیجر هوش مصنوعی را برای این رویداد فعال نکرده است. برای استفاده از قابلیت تسک‌ها با برگزارکننده تماس بگیرید.
            </p>
          )}
        </section>
      )}

      {registration && (
        <section className="space-y-6">
          {taskError && <p className="rounded-lg border border-red-500/40 bg-red-500/10 p-3 text-sm text-red-200">{taskError}</p>}
          {taskSuccess && <p className="rounded-lg border border-emerald-500/40 bg-emerald-500/10 p-3 text-sm text-emerald-200">{taskSuccess}</p>}

          <form onSubmit={handleTaskSubmit} className="space-y-4 rounded-xl border border-slate-800 bg-slate-950/60 p-6">
            <div className="flex items-center justify-between gap-3">
              <h2 className="text-lg font-medium text-slate-100">{editingTaskId ? 'ویرایش تسک' : 'تسک جدید'}</h2>
              {editingTaskId && (
                <button
                  type="button"
                  onClick={resetTaskForm}
                  className="text-xs text-slate-300 hover:text-slate-100"
                >
                  انصراف از ویرایش
                </button>
              )}
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="space-y-1 text-sm text-slate-200">
                <span>عنوان تسک</span>
                <input
                  value={form.title}
                  onChange={(event) => setForm((prev) => ({ ...prev, title: event.target.value }))}
                  className="w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 focus:border-emerald-500 focus:outline-none"
                  required
                />
              </label>
              <label className="space-y-1 text-sm text-slate-200">
                <span>مسئول (اختیاری)</span>
                <input
                  value={form.assignedTo}
                  onChange={(event) => setForm((prev) => ({ ...prev, assignedTo: event.target.value }))}
                  className="w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 focus:border-emerald-500 focus:outline-none"
                />
              </label>
              <label className="space-y-1 text-sm text-slate-200 md:col-span-2">
                <span>توضیحات</span>
                <textarea
                  value={form.description}
                  onChange={(event) => setForm((prev) => ({ ...prev, description: event.target.value }))}
                  rows={3}
                  className="w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 focus:border-emerald-500 focus:outline-none"
                />
              </label>
              <label className="space-y-1 text-sm text-slate-200">
                <span>تاریخ سررسید (اختیاری)</span>
                <input
                  type="datetime-local"
                  value={form.dueAt}
                  onChange={(event) => setForm((prev) => ({ ...prev, dueAt: event.target.value }))}
                  className="w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 focus:border-emerald-500 focus:outline-none"
                />
              </label>
            </div>
            <div className="flex flex-wrap items-center gap-3">
              <button
                type="submit"
                disabled={tasksLoading || !aiTaskManagerEnabled}
                className="rounded-lg bg-emerald-500 px-5 py-2 text-sm font-medium text-emerald-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:bg-emerald-900/50 disabled:text-emerald-100/40"
              >
                {tasksLoading ? 'در حال ذخیره...' : editingTaskId ? 'ثبت ویرایش' : 'ایجاد تسک'}
              </button>
            </div>
          </form>

          <section className="space-y-4 rounded-xl border border-slate-800 bg-slate-950/60 p-6">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-medium text-slate-100">لیست تسک‌های تیم</h2>
              {tasksLoading && <span className="text-xs text-slate-400">در حال به‌روزرسانی...</span>}
            </div>
            {sortedTasks.length === 0 ? (
              <p className="text-sm text-slate-300">تسکی ثبت نشده است. اولین تسک را ایجاد کنید.</p>
            ) : (
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-slate-800 text-sm">
                  <thead>
                    <tr className="text-slate-300">
                      <th className="px-3 py-2 text-right">عنوان</th>
                      <th className="px-3 py-2 text-right">وضعیت</th>
                      <th className="px-3 py-2 text-right">سررسید</th>
                      <th className="px-3 py-2 text-right">مسئول</th>
                      <th className="px-3 py-2 text-right">اقدامات</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-800 text-slate-200">
                    {sortedTasks.map((task) => (
                      <tr key={task.id}>
                        <td className="px-3 py-2">
                          <div className="font-medium text-slate-100">{task.title}</div>
                          {task.description && <p className="mt-1 text-xs text-slate-400">{task.description}</p>}
                          {task.aiRecommendation && (
                            <p className="mt-2 rounded border border-emerald-500/40 bg-emerald-500/10 p-2 text-xs text-emerald-200">
                              پیشنهاد AI: {task.aiRecommendation}
                            </p>
                          )}
                        </td>
                        <td className="px-3 py-2">
                          <span className="inline-flex items-center rounded-full bg-slate-800 px-3 py-1 text-xs text-slate-200">
                            {statusLabels[task.status]}
                          </span>
                        </td>
                        <td className="px-3 py-2 text-xs text-slate-300">{formatDate(task.dueAt)}</td>
                        <td className="px-3 py-2 text-xs text-slate-300">{task.assignedTo ?? '—'}</td>
                        <td className="px-3 py-2">
                          <div className="flex flex-wrap items-center gap-2">
                            <select
                              value={task.status}
                              onChange={(event) => void handleStatusChange(task.id, event.target.value as ParticipantTaskStatus)}
                              className="rounded border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-100"
                            >
                              {statusOptions.map((status) => (
                                <option key={status} value={status}>
                                  {statusLabels[status]}
                                </option>
                              ))}
                            </select>
                            <button
                              type="button"
                              onClick={() => handleEditTask(task)}
                              className="rounded border border-slate-700 px-3 py-1 text-xs text-slate-200 hover:bg-slate-900"
                            >
                              ویرایش
                            </button>
                            <button
                              type="button"
                              onClick={() => handleDeleteTask(task.id)}
                              className="rounded border border-red-600 px-3 py-1 text-xs text-red-300 hover:bg-red-600/20"
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

          <section className="space-y-4 rounded-xl border border-slate-800 bg-slate-950/60 p-6">
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-medium text-slate-100">پیشنهادهای هوش مصنوعی برای تسک‌ها</h2>
              <Link href="/dashboard/admin/events" className="text-xs text-slate-400 underline">
                مدیریت فعال‌سازی توسط ادمین
              </Link>
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="space-y-1 text-sm text-slate-200 md:col-span-2">
                <span>زمینه یا مشکل فعلی تیم</span>
                <textarea
                  value={adviceContext}
                  onChange={(event) => setAdviceContext(event.target.value)}
                  rows={3}
                  className="w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 focus:border-emerald-500 focus:outline-none"
                  placeholder="مثلاً: تسک فرانت‌اند به دلیل تاخیر API متوقف شده است."
                />
              </label>
              <label className="space-y-1 text-sm text-slate-200">
                <span>حوزه مورد تمرکز (اختیاری)</span>
                <input
                  value={adviceFocus}
                  onChange={(event) => setAdviceFocus(event.target.value)}
                  className="w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 focus:border-emerald-500 focus:outline-none"
                  placeholder="مثلاً مارکتینگ، توسعه بک‌اند و ..."
                />
              </label>
            </div>
            <div className="flex flex-wrap items-center gap-3">
              <button
                type="button"
                onClick={() => void handleAdviceRequest()}
                disabled={adviceLoading || !aiTaskManagerEnabled}
                className="rounded-lg bg-emerald-500 px-4 py-2 text-sm font-medium text-emerald-950 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:bg-emerald-900/50 disabled:text-emerald-100/40"
              >
                {adviceLoading ? 'در حال تحلیل...' : 'دریافت پیشنهادها'}
              </button>
              <button
                type="button"
                onClick={() => {
                  setAdviceContext('');
                  setAdviceFocus('');
                  setAdviceError(null);
                  setAdviceResult(null);
                }}
                className="rounded-lg border border-slate-700 px-4 py-2 text-sm text-slate-200 hover:bg-slate-900"
              >
                پاک‌سازی فرم
              </button>
            </div>
            {adviceError && <p className="rounded-lg border border-red-500/40 bg-red-500/10 p-3 text-sm text-red-200">{adviceError}</p>}
            {adviceResult && (
              <div className="space-y-3 rounded-lg border border-emerald-500/40 bg-emerald-500/10 p-4 text-sm text-emerald-100">
                <p className="font-medium text-emerald-200">خلاصه:</p>
                <p>{adviceResult.summary}</p>
                {adviceResult.suggestedTasks.length > 0 && (
                  <div>
                    <p className="font-medium text-emerald-200">تسک‌های پیشنهادی:</p>
                    <ul className="list-disc space-y-1 pr-5">
                      {adviceResult.suggestedTasks.map((task, index) => (
                        <li key={index}>{task}</li>
                      ))}
                    </ul>
                  </div>
                )}
                {adviceResult.risks && (
                  <p>
                    <span className="font-medium text-emerald-200">ریسک‌ها:</span> {adviceResult.risks}
                  </p>
                )}
                {adviceResult.nextSteps && (
                  <p>
                    <span className="font-medium text-emerald-200">گام‌های بعدی:</span> {adviceResult.nextSteps}
                  </p>
                )}
              </div>
            )}
          </section>
        </section>
      )}
    </div>
  );
}
