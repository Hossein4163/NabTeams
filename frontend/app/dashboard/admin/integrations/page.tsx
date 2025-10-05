'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import { useSession } from 'next-auth/react';
import Link from 'next/link';
import {
  activateIntegrationSetting,
  deleteIntegrationSetting,
  IntegrationProviderType,
  IntegrationSetting,
  IntegrationSettingUpsertPayload,
  listIntegrationSettings,
  upsertIntegrationSetting
} from '../../../../lib/api';

interface FormState {
  id?: string;
  type: IntegrationProviderType;
  providerKey: string;
  displayName: string;
  configuration: string;
  activate: boolean;
}

const providerOptions: Array<{ value: IntegrationProviderType; label: string }> = [
  { value: 'Gemini', label: 'هوش مصنوعی Gemini' },
  { value: 'PaymentGateway', label: 'درگاه پرداخت' },
  { value: 'Sms', label: 'پیامک' },
  { value: 'Email', label: 'ایمیل' }
];

const emptyForm: FormState = {
  type: 'Gemini',
  providerKey: '',
  displayName: '',
  configuration: '{\n  \n}\n',
  activate: false
};

const filterOptions: Array<{ value: 'all' | IntegrationProviderType; label: string }> = [
  { value: 'all', label: 'همه انواع' },
  ...providerOptions
];

export default function IntegrationSettingsPage() {
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

  const [items, setItems] = useState<IntegrationSetting[]>([]);
  const [filter, setFilter] = useState<'all' | IntegrationProviderType>('all');
  const [form, setForm] = useState<FormState>(emptyForm);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const loadSettings = useCallback(async () => {
    if (!isAdmin) {
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const result = await listIntegrationSettings(auth, filter === 'all' ? undefined : filter);
      setItems(result);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setLoading(false);
    }
  }, [auth, filter, isAdmin]);

  useEffect(() => {
    loadSettings();
  }, [loadSettings]);

  const handleSelect = (item: IntegrationSetting) => {
    setForm({
      id: item.id,
      type: item.type,
      providerKey: item.providerKey,
      displayName: item.displayName,
      configuration: tryFormatJson(item.configuration),
      activate: false
    });
    setSuccess(null);
    setError(null);
  };

  const handleReset = () => {
    setForm(emptyForm);
    setSuccess(null);
    setError(null);
  };

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!form.providerKey.trim()) {
      setError('کلید ارائه‌دهنده الزامی است.');
      return;
    }

    setSaving(true);
    setError(null);
    try {
      const payload: IntegrationSettingUpsertPayload = {
        id: form.id,
        type: form.type,
        providerKey: form.providerKey.trim(),
        displayName: form.displayName.trim() ? form.displayName.trim() : undefined,
        configuration: form.configuration.trim(),
        activate: form.activate
      };

      const saved = await upsertIntegrationSetting(payload, auth);
      await loadSettings();
      setForm({
        id: saved.id,
        type: saved.type,
        providerKey: saved.providerKey,
        displayName: saved.displayName,
        configuration: tryFormatJson(saved.configuration),
        activate: false
      });
      setSuccess('تنظیمات با موفقیت ذخیره شد.');
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setSaving(false);
    }
  };

  const handleActivate = async (id: string) => {
    setSaving(true);
    setError(null);
    try {
      await activateIntegrationSetting(id, auth);
      await loadSettings();
      setSuccess('تنظیم فعال شد.');
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('آیا از حذف این تنظیم مطمئن هستید؟')) {
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await deleteIntegrationSetting(id, auth);
      await loadSettings();
      if (form.id === id) {
        handleReset();
      }
      setSuccess('تنظیم حذف شد.');
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
          دسترسی به مدیریت تنظیمات یکپارچه‌سازی فقط برای ادمین فعال است. لطفاً با تیم پشتیبانی هماهنگ کنید یا{' '}
          <Link href="/auth/signin" className="underline">
            با حساب دیگری وارد شوید
          </Link>
          .
        </div>
      </div>
    );
  }

  const filteredItems = filter === 'all' ? items : items.filter((item) => item.type === filter);

  return (
    <div className="space-y-6">
      <header className="space-y-2">
        <h1 className="text-3xl font-semibold">تنظیمات یکپارچه‌سازی و کلیدهای دسترسی</h1>
        <p className="text-slate-300 max-w-3xl text-sm leading-6">
          از این بخش می‌توانید کلیدهای API هوش مصنوعی، تنظیمات درگاه پرداخت، پنل پیامکی و SMTP را وارد کنید تا سیستم بدون تغییر فایل‌های پیکربندی اجرا شود.
        </p>
      </header>

      <section className="space-y-3 rounded-2xl border border-slate-800 bg-slate-900/70 p-5">
        <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
          <label className="flex items-center gap-3 text-sm text-slate-300">
            <span>فیلتر بر اساس نوع:</span>
            <select
              value={filter}
              onChange={(event) => setFilter(event.target.value as 'all' | IntegrationProviderType)}
              className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
            >
              {filterOptions.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <button
            onClick={handleReset}
            className="self-start rounded-lg border border-slate-700 px-3 py-2 text-xs text-slate-200"
          >
            تنظیم جدید
          </button>
        </div>
        {loading ? (
          <div className="text-sm text-slate-300">در حال بارگذاری...</div>
        ) : filteredItems.length === 0 ? (
          <div className="text-sm text-slate-400">هنوز تنظیمی برای این نوع ثبت نشده است.</div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-800 text-sm">
              <thead>
                <tr className="text-left text-slate-300">
                  <th className="px-3 py-2">نوع</th>
                  <th className="px-3 py-2">کلید ارائه‌دهنده</th>
                  <th className="px-3 py-2">نام نمایشی</th>
                  <th className="px-3 py-2">وضعیت</th>
                  <th className="px-3 py-2 text-right">عملیات</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800">
                {filteredItems.map((item) => (
                  <tr key={item.id} className={item.isActive ? 'bg-emerald-500/5' : ''}>
                    <td className="px-3 py-2 text-slate-200">{providerLabel(item.type)}</td>
                    <td className="px-3 py-2 text-slate-300 font-mono">{item.providerKey}</td>
                    <td className="px-3 py-2 text-slate-200">{item.displayName}</td>
                    <td className="px-3 py-2 text-slate-300">{item.isActive ? 'فعال' : 'غیرفعال'}</td>
                    <td className="px-3 py-2 text-right space-x-2 rtl:space-x-reverse">
                      <button
                        onClick={() => handleSelect(item)}
                        className="rounded border border-slate-700 px-2 py-1 text-xs text-slate-200"
                      >
                        ویرایش
                      </button>
                      <button
                        onClick={() => handleActivate(item.id)}
                        className="rounded border border-emerald-500 px-2 py-1 text-xs text-emerald-200"
                        disabled={saving}
                      >
                        فعال‌سازی
                      </button>
                      <button
                        onClick={() => handleDelete(item.id)}
                        className="rounded border border-rose-500 px-2 py-1 text-xs text-rose-200"
                        disabled={saving}
                      >
                        حذف
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        {error && <p className="text-sm text-rose-300">{error}</p>}
        {success && <p className="text-sm text-emerald-300">{success}</p>}
      </section>

      <section className="rounded-2xl border border-slate-800 bg-slate-900/70 p-5">
        <h2 className="text-xl font-semibold mb-3">{form.id ? 'ویرایش تنظیم' : 'افزودن تنظیم جدید'}</h2>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            <label className="flex flex-col gap-2 text-sm text-slate-200">
              <span>نوع یکپارچه‌سازی</span>
              <select
                value={form.type}
                onChange={(event) => setForm((prev) => ({ ...prev, type: event.target.value as IntegrationProviderType }))}
                className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
              >
                {providerOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="flex flex-col gap-2 text-sm text-slate-200">
              <span>کلید ارائه‌دهنده</span>
              <input
                value={form.providerKey}
                onChange={(event) => setForm((prev) => ({ ...prev, providerKey: event.target.value }))}
                className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
                placeholder="مثلاً gemini یا idpay"
              />
            </label>
            <label className="flex flex-col gap-2 text-sm text-slate-200">
              <span>نام نمایشی</span>
              <input
                value={form.displayName}
                onChange={(event) => setForm((prev) => ({ ...prev, displayName: event.target.value }))}
                className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-sm text-slate-100"
                placeholder="عنوانی برای نمایش در داشبورد"
              />
            </label>
            <label className="flex items-center gap-2 text-sm text-slate-200">
              <input
                type="checkbox"
                checked={form.activate}
                onChange={(event) => setForm((prev) => ({ ...prev, activate: event.target.checked }))}
                className="h-4 w-4 rounded border-slate-700 bg-slate-900"
              />
              <span>پس از ذخیره فعال شود</span>
            </label>
          </div>

          <label className="flex flex-col gap-2 text-sm text-slate-200">
            <span>پیکربندی JSON</span>
            <textarea
              value={form.configuration}
              onChange={(event) => setForm((prev) => ({ ...prev, configuration: event.target.value }))}
              rows={12}
              className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 font-mono text-xs text-emerald-100"
              spellCheck={false}
            />
            <span className="text-xs text-slate-400">
              مقادیر حساس مانند API Key را اینجا وارد کنید. داده‌ها در پایگاه‌داده رمزنگاری نشده‌اند، بنابراین حتماً از دسترسی ادمین محافظت کنید.
            </span>
          </label>

          <div className="flex items-center gap-3">
            <button
              type="submit"
              disabled={saving}
              className="rounded-lg bg-emerald-500 px-4 py-2 text-sm font-medium text-emerald-950 disabled:opacity-70"
            >
              ذخیره تنظیمات
            </button>
            <button
              type="button"
              onClick={handleReset}
              className="rounded-lg border border-slate-700 px-4 py-2 text-sm text-slate-200"
              disabled={saving}
            >
              پاک‌سازی فرم
            </button>
          </div>
        </form>
      </section>
    </div>
  );
}

function providerLabel(type: IntegrationProviderType): string {
  const map: Record<IntegrationProviderType, string> = {
    Gemini: 'هوش مصنوعی Gemini',
    PaymentGateway: 'درگاه پرداخت',
    Sms: 'پیامک',
    Email: 'ایمیل'
  };
  return map[type];
}

function tryFormatJson(raw: string): string {
  if (!raw) {
    return '{\n  \n}\n';
  }

  try {
    const parsed = JSON.parse(raw);
    return JSON.stringify(parsed, null, 2);
  } catch {
    return raw;
  }
}
