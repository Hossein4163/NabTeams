'use client';

import { useState } from 'react';
import {
  JudgeRegistrationPayload,
  JudgeRegistrationResponse,
  submitJudgeRegistration
} from '../../../lib/api';

const initialForm: JudgeRegistrationPayload & { birthDate: string } = {
  firstName: '',
  lastName: '',
  nationalId: '',
  phoneNumber: '',
  email: '',
  birthDate: '',
  fieldOfExpertise: '',
  highestDegree: '',
  biography: ''
};

export default function JudgeRegistrationPage() {
  const [form, setForm] = useState(() => ({ ...initialForm }));
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState<JudgeRegistrationResponse | null>(null);

  function updateField<K extends keyof typeof initialForm>(key: K, value: (typeof initialForm)[K]) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  function validate(): string | null {
    if (!form.firstName.trim() || !form.lastName.trim()) {
      return 'نام و نام خانوادگی را کامل کنید.';
    }
    if (!form.nationalId.trim() || !form.phoneNumber.trim()) {
      return 'کد ملی و شماره تماس الزامی است.';
    }
    if (!form.fieldOfExpertise.trim() || !form.highestDegree.trim()) {
      return 'حوزه تخصص و آخرین مدرک تحصیلی را وارد کنید.';
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

    const payload: JudgeRegistrationPayload = {
      firstName: form.firstName.trim(),
      lastName: form.lastName.trim(),
      nationalId: form.nationalId.trim(),
      phoneNumber: form.phoneNumber.trim(),
      email: form.email?.trim() ? form.email.trim() : null,
      birthDate: form.birthDate ? new Date(form.birthDate).toISOString() : null,
      fieldOfExpertise: form.fieldOfExpertise.trim(),
      highestDegree: form.highestDegree.trim(),
      biography: form.biography?.trim() ? form.biography.trim() : null
    };

    try {
      const response = await submitJudgeRegistration(payload);
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
    setForm({ ...initialForm });
    setResult(null);
    setError(null);
  }

  if (result) {
    return (
      <div className="space-y-6">
        <div className="rounded-xl border border-emerald-600/40 bg-emerald-500/10 p-6">
          <h2 className="text-2xl font-semibold text-emerald-300">درخواست داوری ثبت شد</h2>
          <p className="mt-3 text-sm text-emerald-100/90">
            تیم اجرایی پس از بررسی اطلاعات با شما تماس خواهد گرفت. شناسه پیگیری: <span className="font-mono">{result.id}</span>
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
        <TextField label="ایمیل" value={form.email ?? ''} onChange={(value) => updateField('email', value)} type="email" />
        <TextField label="تاریخ تولد" value={form.birthDate} onChange={(value) => updateField('birthDate', value)} type="date" />
        <TextField
          label="حوزه تخصص"
          value={form.fieldOfExpertise}
          onChange={(value) => updateField('fieldOfExpertise', value)}
          required
        />
        <TextField
          label="آخرین مدرک تحصیلی"
          value={form.highestDegree}
          onChange={(value) => updateField('highestDegree', value)}
          required
        />
      </div>
      <TextArea
        label="بیوگرافی یا رزومه کوتاه"
        placeholder="تجربیات مهم، رویدادهای داوری شده یا حوزه‌های تخصصی خود را شرح دهید."
        value={form.biography ?? ''}
        onChange={(value) => updateField('biography', value)}
      />
      <div className="flex justify-end">
        <button
          type="submit"
          disabled={submitting}
          className="rounded-lg bg-emerald-500 px-5 py-2 text-sm font-medium text-emerald-950 transition hover:bg-emerald-400 disabled:opacity-60"
        >
          {submitting ? 'در حال ارسال...' : 'ثبت درخواست داوری'}
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
  type = 'text'
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  required?: boolean;
  type?: string;
}) {
  return (
    <label className="text-sm text-slate-300">
      {label}
      <input
        value={value}
        onChange={(event) => onChange(event.target.value)}
        type={type}
        required={required}
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
