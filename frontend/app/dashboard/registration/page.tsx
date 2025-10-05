'use client';

import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import {
  NotificationChannel,
  ParticipantRegistrationResponse,
  RegistrationPaymentStatus,
  RegistrationStatus,
  getParticipantRegistration
} from '../../../lib/api';

function formatStatus(status: RegistrationStatus) {
  switch (status) {
    case 'Finalized':
      return 'در انتظار تایید ادمین';
    case 'Approved':
      return 'در انتظار ارسال لینک پرداخت';
    case 'PaymentRequested':
      return 'منتظر پرداخت تیم';
    case 'PaymentCompleted':
      return 'پرداخت تکمیل شد';
    case 'Rejected':
      return 'رد شده توسط داور';
    case 'Cancelled':
      return 'لغو شده';
    default:
      return 'ثبت‌نام ارسال شده';
  }
}

function formatPaymentStatus(status: RegistrationPaymentStatus) {
  switch (status) {
    case 'Completed':
      return 'پرداخت موفق';
    case 'Failed':
      return 'پرداخت ناموفق';
    case 'Cancelled':
      return 'پرداخت لغو شد';
    default:
      return 'در انتظار پرداخت';
  }
}

function formatChannel(channel: NotificationChannel) {
  return channel === 'Sms' ? 'پیامک' : 'ایمیل';
}

export default function RegistrationDashboardPage() {
  const searchParams = useSearchParams();
  const initialId = searchParams.get('id') ?? '';
  const [lookupId, setLookupId] = useState(initialId);
  const [registration, setRegistration] = useState<ParticipantRegistrationResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (initialId) {
      void handleLookup(initialId);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initialId]);

  const formattedAmount = useMemo(() => {
    if (!registration?.payment) {
      return null;
    }
    return new Intl.NumberFormat('fa-IR').format(registration.payment.amount);
  }, [registration?.payment]);

  async function handleLookup(idOverride?: string) {
    const idToFetch = (idOverride ?? lookupId).trim();
    if (!idToFetch) {
      setError('شناسه ثبت‌نام را وارد کنید.');
      setRegistration(null);
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const response = await getParticipantRegistration(idToFetch);
      setRegistration(response);
    } catch (err) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('امکان بازیابی وضعیت وجود ندارد.');
      }
      setRegistration(null);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="space-y-8">
      <header className="space-y-2">
        <h1 className="text-2xl font-semibold text-slate-100">داشبورد وضعیت ثبت‌نام</h1>
        <p className="text-sm text-slate-400">
          با وارد کردن کد پیگیری، آخرین وضعیت تیم، اعلان‌های ارسال‌شده و وضعیت پرداخت مرحله دوم را مشاهده کنید.
        </p>
      </header>

      <section className="space-y-4 rounded-xl border border-slate-800 bg-slate-950/40 p-6">
        <div className="flex flex-col gap-3 md:flex-row md:items-center">
          <label className="flex-1 text-sm text-slate-300">
            شناسه ثبت‌نام
            <input
              value={lookupId}
              onChange={(event) => setLookupId(event.target.value)}
              placeholder="مثلاً 58f3fc6f-0f73-4c81-8f5a-64f0d3d1bebf"
              className="mt-2 w-full rounded-lg border border-slate-700 bg-slate-900/60 p-2 text-sm text-slate-100 placeholder:text-slate-500 focus:border-emerald-500 focus:outline-none"
            />
          </label>
          <button
            type="button"
            onClick={() => void handleLookup()}
            disabled={loading}
            className="inline-flex items-center justify-center rounded-lg bg-emerald-500 px-4 py-2 text-sm font-medium text-emerald-900 transition hover:bg-emerald-400 disabled:cursor-not-allowed disabled:bg-emerald-900/50 disabled:text-emerald-100/50"
          >
            {loading ? 'در حال بررسی...' : 'بررسی وضعیت'}
          </button>
        </div>
        {error && <p className="rounded-lg border border-red-500/40 bg-red-500/10 p-3 text-sm text-red-200">{error}</p>}
      </section>

      {registration && (
        <section className="space-y-6 rounded-xl border border-slate-800 bg-slate-950/50 p-6">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div>
              <h2 className="text-xl font-semibold text-slate-100">وضعیت تیم</h2>
              <p className="text-sm text-slate-300">
                وضعیت فعلی: <span className="font-medium text-emerald-300">{formatStatus(registration.status)}</span>
              </p>
              <p className="text-xs text-slate-400">
                تاریخ ثبت: {new Date(registration.submittedAt).toLocaleString('fa-IR')}
              </p>
              {registration.finalizedAt && (
                <p className="text-xs text-slate-400">
                  تأیید نهایی توسط تیم در {new Date(registration.finalizedAt).toLocaleString('fa-IR')}
                </p>
              )}
            </div>
            <button
              type="button"
              onClick={() => void handleLookup(registration.id)}
              className="inline-flex items-center justify-center rounded-lg border border-slate-700 px-3 py-1 text-xs text-slate-300 transition hover:bg-slate-800"
            >
              به‌روزرسانی وضعیت
            </button>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2 rounded-lg border border-slate-800/70 bg-slate-900/60 p-4 text-sm text-slate-200">
              <h3 className="font-medium text-slate-100">مشخصات سرپرست</h3>
              <p>نام: {registration.headFirstName} {registration.headLastName}</p>
              <p>شماره تماس: {registration.phoneNumber}</p>
              {registration.email && <p>ایمیل: {registration.email}</p>}
              {registration.birthDate && <p>تاریخ تولد: {registration.birthDate}</p>}
              <p>مدرک تحصیلی: {registration.educationDegree}</p>
              <p>رشته تحصیلی: {registration.fieldOfStudy}</p>
            </div>
            <div className="space-y-2 rounded-lg border border-slate-800/70 bg-slate-900/60 p-4 text-sm text-slate-200">
              <h3 className="font-medium text-slate-100">وضعیت تیم</h3>
              <p>نام تیم: {registration.teamName}</p>
              <p>تیم تشکیل شده است: {registration.hasTeam ? 'بله' : 'خیر'}</p>
              <p>تیم تکمیل است: {registration.teamCompleted ? 'بله' : 'خیر'}</p>
              {registration.additionalNotes && <p>توضیحات: {registration.additionalNotes}</p>}
            </div>
          </div>

          {registration.payment && (
            <div className="rounded-lg border border-emerald-600/40 bg-emerald-500/5 p-4 text-sm">
              <h3 className="mb-3 text-base font-semibold text-emerald-200">پرداخت مرحله دوم</h3>
              <div className="grid gap-2 md:grid-cols-2">
                <div>
                  <span className="text-emerald-200/70">مبلغ قابل پرداخت</span>
                  <p className="font-semibold text-emerald-100">
                    {formattedAmount} {registration.payment.currency}
                  </p>
                </div>
                <div>
                  <span className="text-emerald-200/70">وضعیت تراکنش</span>
                  <p className="font-semibold text-emerald-100">{formatPaymentStatus(registration.payment.status)}</p>
                </div>
                <div className="md:col-span-2">
                  <span className="text-emerald-200/70">لینک پرداخت</span>
                  <p>
                    <a
                      href={registration.payment.paymentUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="break-all text-emerald-300 underline-offset-2 hover:underline"
                    >
                      {registration.payment.paymentUrl}
                    </a>
                  </p>
                </div>
                <div>
                  <span className="text-emerald-200/70">درخواست پرداخت</span>
                  <p className="text-emerald-100/80">
                    {new Date(registration.payment.requestedAt).toLocaleString('fa-IR')}
                  </p>
                </div>
                {registration.payment.completedAt && (
                  <div>
                    <span className="text-emerald-200/70">تاریخ تایید پرداخت</span>
                    <p className="text-emerald-100/80">
                      {new Date(registration.payment.completedAt).toLocaleString('fa-IR')}
                    </p>
                  </div>
                )}
              </div>
              {registration.payment.status === 'Pending' && (
                <p className="mt-3 text-xs text-emerald-200/80">
                  پس از پرداخت، وضعیت به صورت خودکار در همین صفحه به‌روزرسانی می‌شود.
                </p>
              )}
            </div>
          )}

          {registration.notifications.length > 0 && (
            <div className="space-y-2">
              <h3 className="text-base font-semibold text-slate-100">اعلان‌های ارسال‌شده</h3>
              <ul className="space-y-2 text-xs text-slate-300">
                {registration.notifications.map((notification) => (
                  <li key={notification.id} className="rounded-lg border border-slate-800/70 bg-slate-900/60 p-3">
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <span className="font-medium text-slate-100">{notification.subject}</span>
                      <span className="text-slate-400">
                        {formatChannel(notification.channel)} — {new Date(notification.sentAt).toLocaleString('fa-IR')}
                      </span>
                    </div>
                    <p className="mt-1 leading-relaxed text-slate-300">{notification.message}</p>
                    <p className="mt-1 text-slate-400">گیرنده: {notification.recipient}</p>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </section>
      )}
    </div>
  );
}
