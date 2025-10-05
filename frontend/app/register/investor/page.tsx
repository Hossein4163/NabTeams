'use client';

import { useState } from 'react';
import {
  InvestorRegistrationPayload,
  InvestorRegistrationResponse,
  submitInvestorRegistration
} from '../../../lib/api';

interface InvestorFormState {
  firstName: string;
  lastName: string;
  nationalId: string;
  phoneNumber: string;
  email: string;
  additionalNotes: string;
  interestAreas: string[];
}

const initialFormState: InvestorFormState = {
  firstName: '',
  lastName: '',
  nationalId: '',
  phoneNumber: '',
  email: '',
  additionalNotes: '',
  interestAreas: ['']
};

export default function InvestorRegistrationPage() {
  const [form, setForm] = useState<InvestorFormState>(() => ({ ...initialFormState }));
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState<InvestorRegistrationResponse | null>(null);

  function updateField<K extends keyof InvestorFormState>(key: K, value: InvestorFormState[K]) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  function updateInterest(index: number, value: string) {
    setForm((prev) => ({
      ...prev,
      interestAreas: prev.interestAreas.map((area, idx) => (idx === index ? value : area))
    }));
  }

  function addInterest() {
    setForm((prev) => ({
      ...prev,
      interestAreas: [...prev.interestAreas, '']
    }));
  }

  function removeInterest(index: number) {
    setForm((prev) => ({
      ...prev,
      interestAreas: prev.interestAreas.filter((_, idx) => idx !== index)
    }));
  }

  function validate(): string | null {
    if (!form.firstName.trim() || !form.lastName.trim()) {
      return 'نام و نام خانوادگی را وارد کنید.';
    }
    if (!form.nationalId.trim() || !form.phoneNumber.trim()) {
      return 'کد ملی و شماره تماس الزامی است.';
    }
    const filledInterests = form.interestAreas.map((area) => area.trim()).filter(Boolean);
    if (filledInterests.length === 0) {
      return 'حداقل یک حوزه علاقه‌مندی را مشخص کنید.';
    }
    return null;
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const validationError = validate();
    if (validationError) {
      setError(validationError);
      return;
    }

    setSubmitting(true);
    setError(null);

    const interestAreas = form.interestAreas
      .map((area) => area.trim())
      .filter(Boolean)
      .map((area) => area.replace(/\s+/g, ' '));

    const payload: InvestorRegistrationPayload = {
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
      nationalId: form.nationalId.trim(),
      phoneNumber: form.phoneNumber.trim(),
      email: form.email.trim() ? form.email.trim() : null,
      additionalNotes: form.additionalNotes.trim() ? form.additionalNotes.trim() : null,
      interestAreas
    };

    try {
      const response = await submitInvestorRegistration(payload);
      setResult(response);
    } catch (err) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('ثبت اطلاعات با خطا مواجه شد.');
      }
    } finally {
      setSubmitting(false);
    }
  }

  function resetForm() {
    setForm({ ...initialFormState });
    setError(null);
    setResult(null);
  }

  if (result) {
    return (
      <div className="space-y-6">
        <div className="rounded-xl border border-emerald-600/40 bg-emerald-500/10 p-6">
          <h2 className="text-2xl font-semibold text-emerald-300">درخواست سرمایه‌گذاری ثبت شد</h2>
          <p className="mt-3 text-sm text-emerald-100/90">
            از طریق داشبورد سرمایه‌گذاران، پروژه‌های متناسب با حوزه‌های انتخابی شما معرفی می‌شوند. شناسه پیگیری:{' '}
            <span className="font-mono">{result.id}</span>
          </p>
        </div>
        <button
          type="button"
          onClick={resetForm}
          className="inline-flex items-center justify-center rounded-lg border border-emerald-500 px-4 py-2 text-sm font-medium text-emerald-200 transition hover:bg-emerald-500/10"
        >
          ثبت‌نام جدید
        </button>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {error && <p className="rounded-lg border border-red-500/40 bg-red-500/10 p-3 text-sm text-red-200">{error}</p>}
      <div className="grid gap-4 md:grid-cols-2">
        <TextField label="نام" value={form.firstName} onChange={(value) => updateField('firstName', value)} required />
        <TextField label="نام خانوادگی" value={form.lastName} onChange={(value) => updateField('lastName', value)} required />
        <TextField label="کد ملی" value={form.nationalId} onChange={(value) => updateField('nationalId', value)} required />
        <TextField label="شماره تماس" value={form.phoneNumber} onChange={(value) => updateField('phoneNumber', value)} required />
        <TextField label="ایمیل" value={form.email} onChange={(value) => updateField('email', value)} type="email" />
      </div>
      <TextArea
        label="توضیحات تکمیلی"
        value={form.additionalNotes}
        onChange={(value) => updateField('additionalNotes', value)}
        placeholder="شرایط مدنظر، مبالغ هدف یا انواع مشارکت مورد علاقه خود را بنویسید."
      />
      <section className="space-y-3">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold text-slate-200">حوزه‌های علاقه‌مندی</h2>
          <button
            type="button"
            onClick={addInterest}
            className="rounded-lg border border-emerald-500 px-3 py-1 text-sm text-emerald-200 transition hover:bg-emerald-500/10"
          >
            افزودن حوزه جدید
          </button>
        </div>
        <p className="text-xs text-slate-400">
          حوزه‌ها می‌تواند شامل فین‌تک، سلامت دیجیتال، روباتیک، آموزش یا سایر حوزه‌های نوآورانه باشد.
        </p>
        <div className="space-y-3">
          {form.interestAreas.map((area, index) => (
            <div key={index} className="flex items-center gap-3">
              <TextField
                label={`حوزه ${index + 1}`}
                value={area}
                onChange={(value) => updateInterest(index, value)}
                className="flex-1"
                placeholder="مثلاً سلامت دیجیتال"
              />
              {form.interestAreas.length > 1 && (
                <button
                  type="button"
                  onClick={() => removeInterest(index)}
                  className="rounded-lg border border-red-500/40 px-3 py-2 text-xs text-red-200 transition hover:bg-red-500/10"
                >
                  حذف
                </button>
              )}
            </div>
          ))}
        </div>
      </section>
      <div className="flex justify-end">
        <button
          type="submit"
          disabled={submitting}
          className="rounded-lg bg-emerald-500 px-5 py-2 text-sm font-medium text-emerald-950 transition hover:bg-emerald-400 disabled:opacity-60"
        >
          {submitting ? 'در حال ارسال...' : 'ثبت اطلاعات سرمایه‌گذار'}
        </button>
      </div>
    </form>
  );
}

function TextField({
  label,
  value,
  onChange,
  required,
  type = 'text',
  className = '',
  placeholder
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  required?: boolean;
  type?: string;
  className?: string;
  placeholder?: string;
}) {
  return (
    <label className={`text-sm text-slate-300 ${className}`}>
      {label}
      <input
        value={value}
        onChange={(event) => onChange(event.target.value)}
        type={type}
        required={required}
        placeholder={placeholder}
        className="mt-1 w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 focus:border-emerald-500 focus:outline-none"
      />
    </label>
  );
}

function TextArea({
  label,
  value,
  onChange,
  placeholder
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}) {
  return (
    <label className="block text-sm text-slate-300">
      {label}
      <textarea
        value={value}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
        rows={4}
        className="mt-1 w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100 focus:border-emerald-500 focus:outline-none"
      />
    </label>
  );
}
