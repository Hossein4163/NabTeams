'use client';

import { useMemo, useState } from 'react';
import {
  ParticipantDocumentInput,
  ParticipantLinkInput,
  ParticipantRegistrationPayload,
  ParticipantRegistrationResponse,
  ParticipantTeamMemberInput,
  RegistrationDocumentCategory,
  RegistrationLinkType,
  submitParticipantRegistration
} from '../../../lib/api';

const stepLabels = [
  'اطلاعات سرپرست',
  'اعضای تیم',
  'مدارک آپلود شده',
  'شبکه‌های اجتماعی و لینک‌ها',
  'بازبینی و ارسال'
];

type MemberDraft = ParticipantTeamMemberInput;
type DocumentDraft = ParticipantDocumentInput;
type LinkDraft = ParticipantLinkInput;

type ParticipantFormState = {
  headFirstName: string;
  headLastName: string;
  nationalId: string;
  phoneNumber: string;
  email: string;
  birthDate: string;
  educationDegree: string;
  fieldOfStudy: string;
  teamName: string;
  hasTeam: boolean;
  teamCompleted: boolean;
  additionalNotes: string;
  members: MemberDraft[];
  documents: DocumentDraft[];
  links: LinkDraft[];
};

const documentCategoryOptions: Array<{ value: RegistrationDocumentCategory; label: string }> = [
  { value: 'ProjectArchive', label: 'آرشیو پروژه' },
  { value: 'TeamResume', label: 'رزومه تیم' },
  { value: 'Presentation', label: 'ارائه/اسلاید' },
  { value: 'BusinessModel', label: 'مدل کسب‌وکار' },
  { value: 'Other', label: 'سایر مدارک' }
];

const linkTypeOptions: Array<{ value: RegistrationLinkType; label: string }> = [
  { value: 'LinkedIn', label: 'لینکدین' },
  { value: 'GitHub', label: 'گیت‌هاب' },
  { value: 'Website', label: 'وب‌سایت' },
  { value: 'Demo', label: 'دمو/ویدئو' },
  { value: 'Other', label: 'سایر' }
];

function cleanupObjectUrl(url: string) {
  if (typeof url === 'string' && url.startsWith('blob:')) {
    URL.revokeObjectURL(url);
  }
}

function createInitialState(): ParticipantFormState {
  return {
    headFirstName: '',
    headLastName: '',
    nationalId: '',
    phoneNumber: '',
    email: '',
    birthDate: '',
    educationDegree: '',
    fieldOfStudy: '',
    teamName: '',
    hasTeam: true,
    teamCompleted: false,
    additionalNotes: '',
    members: [
      {
        fullName: '',
        role: '',
        focusArea: ''
      }
    ],
    documents: [],
    links: []
  };
}

export default function ParticipantRegistrationPage() {
  const [step, setStep] = useState(0);
  const [form, setForm] = useState<ParticipantFormState>(() => createInitialState());
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState<ParticipantRegistrationResponse | null>(null);

  const filteredMembers = useMemo(() => {
    if (!form.hasTeam) {
      return [];
    }
    return form.members.filter(
      (member) => member.fullName.trim() || member.role.trim() || member.focusArea.trim()
    );
  }, [form.hasTeam, form.members]);

  const filteredDocuments = useMemo(
    () => form.documents.filter((doc) => doc.fileName.trim() && doc.fileUrl.trim()),
    [form.documents]
  );

  const filteredLinks = useMemo(
    () => form.links.filter((link) => link.url.trim()),
    [form.links]
  );

  const totalSteps = stepLabels.length;

  function updateField<K extends keyof ParticipantFormState>(key: K, value: ParticipantFormState[K]) {
    setForm((prev) => ({ ...prev, [key]: value }));
  }

  function updateMember(index: number, key: keyof MemberDraft, value: string) {
    setForm((prev) => {
      const nextMembers = prev.members.map((member, idx) =>
        idx === index ? { ...member, [key]: value } : member
      );
      return { ...prev, members: nextMembers };
    });
  }

  function addMember() {
    setForm((prev) => ({
      ...prev,
      members: [...prev.members, { fullName: '', role: '', focusArea: '' }]
    }));
  }

  function removeMember(index: number) {
    setForm((prev) => ({
      ...prev,
      members: prev.members.filter((_, idx) => idx !== index)
    }));
  }

  function updateDocument(index: number, key: keyof DocumentDraft, value: string) {
    setForm((prev) => {
      const nextDocuments = prev.documents.map((doc, idx) =>
        idx === index ? { ...doc, [key]: value } : doc
      );
      return { ...prev, documents: nextDocuments };
    });
  }

  function addDocument() {
    setForm((prev) => ({
      ...prev,
      documents: [
        ...prev.documents,
        { category: 'ProjectArchive', fileName: '', fileUrl: '' }
      ]
    }));
  }

  function removeDocument(index: number) {
    setForm((prev) => {
      const target = prev.documents[index];
      if (target?.fileUrl) {
        cleanupObjectUrl(target.fileUrl);
      }
      return {
        ...prev,
        documents: prev.documents.filter((_, idx) => idx !== index)
      };
    });
  }

  function handleDocumentFile(index: number, file: File | null) {
    if (!file) {
      return;
    }
    setForm((prev) => {
      const target = prev.documents[index];
      if (!target) {
        return prev;
      }
      if (target.fileUrl) {
        cleanupObjectUrl(target.fileUrl);
      }
      const nextDocuments = prev.documents.map((doc, idx) =>
        idx === index ? { ...doc, fileName: file.name, fileUrl: URL.createObjectURL(file) } : doc
      );
      return { ...prev, documents: nextDocuments };
    });
  }

  function updateLink(index: number, key: keyof LinkDraft, value: string) {
    setForm((prev) => {
      const nextLinks = prev.links.map((link, idx) =>
        idx === index ? { ...link, [key]: value } : link
      );
      return { ...prev, links: nextLinks };
    });
  }

  function addLink() {
    setForm((prev) => ({
      ...prev,
      links: [...prev.links, { type: 'LinkedIn', label: '', url: '' }]
    }));
  }

  function removeLink(index: number) {
    setForm((prev) => ({
      ...prev,
      links: prev.links.filter((_, idx) => idx !== index)
    }));
  }

  function validateStep(index: number): boolean {
    if (index === 0) {
      if (!form.headFirstName.trim() || !form.headLastName.trim()) {
        setError('لطفاً نام و نام خانوادگی سرپرست را وارد کنید.');
        return false;
      }
      if (!form.nationalId.trim() || !form.phoneNumber.trim()) {
        setError('کد ملی و شماره تماس سرپرست اجباری است.');
        return false;
      }
      if (!form.educationDegree.trim() || !form.fieldOfStudy.trim()) {
        setError('مدرک و رشته تحصیلی را کامل کنید.');
        return false;
      }
      if (!form.teamName.trim()) {
        setError('نام تیم نمی‌تواند خالی باشد.');
        return false;
      }
    }

    if (index === 1 && form.hasTeam) {
      if (filteredMembers.length === 0) {
        setError('حداقل یک عضو تیم را ثبت کنید یا گزینه «بدون تیم» را فعال کنید.');
        return false;
      }
      const hasIncompleteMember = filteredMembers.some(
        (member) => !member.fullName.trim() || !member.role.trim() || !member.focusArea.trim()
      );
      if (hasIncompleteMember) {
        setError('تمام فیلدهای اعضای تیم باید تکمیل شوند.');
        return false;
      }
    }

    if (index === 2) {
      const hasPartialDocument = form.documents.some(
        (doc) => doc.fileName.trim() !== '' && doc.fileUrl.trim() === ''
      );
      if (hasPartialDocument) {
        setError('برای هر مدرک، آدرس فایل نیز باید تعیین شود.');
        return false;
      }
    }

    if (index === 3) {
      const hasInvalidLink = form.links.some((link) => link.url.trim() === '' && link.label?.trim());
      if (hasInvalidLink) {
        setError('اگر برای لینک برچسب مشخص می‌کنید، آدرس آن نیز باید وارد شود.');
        return false;
      }
    }

    setError(null);
    return true;
  }

  async function handleNext() {
    if (!validateStep(step)) {
      return;
    }
    setStep((prev) => Math.min(prev + 1, totalSteps - 1));
  }

  function handlePrevious() {
    setError(null);
    setStep((prev) => Math.max(prev - 1, 0));
  }

  async function handleSubmit() {
    if (!validateStep(step)) {
      return;
    }

    setSubmitting(true);
    setError(null);

    const payload: ParticipantRegistrationPayload = {
      headFirstName: form.headFirstName.trim(),
      headLastName: form.headLastName.trim(),
      nationalId: form.nationalId.trim(),
      phoneNumber: form.phoneNumber.trim(),
      email: form.email.trim() ? form.email.trim() : null,
      birthDate: form.birthDate ? new Date(form.birthDate).toISOString() : null,
      educationDegree: form.educationDegree.trim(),
      fieldOfStudy: form.fieldOfStudy.trim(),
      teamName: form.teamName.trim(),
      hasTeam: form.hasTeam,
      teamCompleted: form.hasTeam ? form.teamCompleted : false,
      additionalNotes: form.additionalNotes.trim() ? form.additionalNotes.trim() : null,
      members: form.hasTeam
        ? filteredMembers.map((member) => ({
            fullName: member.fullName.trim(),
            role: member.role.trim(),
            focusArea: member.focusArea.trim()
          }))
        : [],
      documents: filteredDocuments.map((doc) => ({
        category: doc.category,
        fileName: doc.fileName.trim(),
        fileUrl: doc.fileUrl.trim()
      })),
      links: filteredLinks.map((link) => ({
        type: link.type,
        label: link.label?.trim() ? link.label.trim() : null,
        url: link.url.trim()
      }))
    };

    try {
      const response = await submitParticipantRegistration(payload);
      setResult(response);
    } catch (err) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('ارسال اطلاعات با خطا مواجه شد.');
      }
    } finally {
      setSubmitting(false);
    }
  }

  function handleReset() {
    form.documents.forEach((doc) => cleanupObjectUrl(doc.fileUrl));
    setForm(createInitialState());
    setResult(null);
    setError(null);
    setStep(0);
  }

  if (result) {
    return (
      <div className="space-y-6">
        <div className="rounded-xl border border-emerald-600/50 bg-emerald-500/10 p-6">
          <h2 className="text-2xl font-semibold text-emerald-300">ثبت‌نام با موفقیت انجام شد</h2>
          <p className="mt-3 text-sm text-emerald-100/90">
            کد پیگیری شما <span className="font-mono">{result.id}</span> است. در صورت نیاز به ویرایش، به بخش مدیریت تیم مراجعه کنید.
          </p>
        </div>
        <div className="space-y-4 rounded-xl border border-slate-800 bg-slate-950/40 p-6">
          <h3 className="text-lg font-semibold">خلاصه اطلاعات ارسال‌شده</h3>
          <dl className="grid gap-2 text-sm text-slate-200 md:grid-cols-2">
            <div>
              <dt className="text-slate-400">نام سرپرست</dt>
              <dd>
                {result.headFirstName} {result.headLastName}
              </dd>
            </div>
            <div>
              <dt className="text-slate-400">نام تیم</dt>
              <dd>{result.teamName}</dd>
            </div>
            <div>
              <dt className="text-slate-400">تاریخ ثبت</dt>
              <dd>{new Date(result.submittedAt).toLocaleString('fa-IR')}</dd>
            </div>
            <div>
              <dt className="text-slate-400">وضعیت تکمیل تیم</dt>
              <dd>{result.teamCompleted ? 'تیم تکمیل است' : 'در حال تکمیل'}</dd>
            </div>
          </dl>
          {result.members.length > 0 && (
            <div>
              <h4 className="font-medium text-slate-200">اعضای ثبت‌شده</h4>
              <ul className="mt-2 space-y-2 text-sm text-slate-300">
                {result.members.map((member) => (
                  <li key={member.id} className="rounded-lg border border-slate-800/70 bg-slate-900/50 p-3">
                    <div className="font-medium">{member.fullName}</div>
                    <div className="text-xs text-slate-400">
                      {member.role} — {member.focusArea}
                    </div>
                  </li>
                ))}
              </ul>
            </div>
          )}
          {result.documents.length > 0 && (
            <div>
              <h4 className="font-medium text-slate-200">مدارک</h4>
              <ul className="mt-2 space-y-2 text-sm text-emerald-200">
                {result.documents.map((doc) => (
                  <li key={doc.id} className="truncate">
                    <span className="text-slate-400">[{doc.category}]</span> {doc.fileName}
                  </li>
                ))}
              </ul>
            </div>
          )}
          {result.links.length > 0 && (
            <div>
              <h4 className="font-medium text-slate-200">لینک‌ها</h4>
              <ul className="mt-2 space-y-2 text-sm text-emerald-200">
                {result.links.map((link) => (
                  <li key={link.id} className="truncate">
                    <span className="text-slate-400">[{link.type}]</span> {link.label ?? link.url}
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
        <button
          type="button"
          onClick={handleReset}
          className="inline-flex items-center justify-center rounded-lg border border-emerald-500 px-4 py-2 text-sm font-medium text-emerald-200 transition hover:bg-emerald-500/10"
        >
          ثبت‌نام جدید
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <StepIndicator currentStep={step} />

      {error && <p className="rounded-lg border border-red-500/40 bg-red-500/10 p-3 text-sm text-red-200">{error}</p>}

      {step === 0 && (
        <section className="space-y-6">
          <div className="grid gap-4 md:grid-cols-2">
            <TextField
              label="نام"
              value={form.headFirstName}
              onChange={(value) => updateField('headFirstName', value)}
              required
            />
            <TextField
              label="نام خانوادگی"
              value={form.headLastName}
              onChange={(value) => updateField('headLastName', value)}
              required
            />
            <TextField
              label="کد ملی"
              value={form.nationalId}
              onChange={(value) => updateField('nationalId', value)}
              required
            />
            <TextField
              label="شماره تماس"
              value={form.phoneNumber}
              onChange={(value) => updateField('phoneNumber', value)}
              required
            />
            <TextField label="ایمیل" value={form.email} onChange={(value) => updateField('email', value)} type="email" />
            <TextField
              label="تاریخ تولد"
              value={form.birthDate}
              onChange={(value) => updateField('birthDate', value)}
              type="date"
            />
            <TextField
              label="مدرک تحصیلی"
              value={form.educationDegree}
              onChange={(value) => updateField('educationDegree', value)}
              required
            />
            <TextField
              label="رشته تحصیلی"
              value={form.fieldOfStudy}
              onChange={(value) => updateField('fieldOfStudy', value)}
              required
            />
            <TextField
              label="نام تیم"
              value={form.teamName}
              onChange={(value) => updateField('teamName', value)}
              className="md:col-span-2"
              required
            />
          </div>
          <div className="flex flex-wrap items-center gap-6 text-sm text-slate-200">
            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                checked={form.hasTeam}
                onChange={(event) => {
                  const checked = event.target.checked;
                  updateField('hasTeam', checked);
                  if (!checked) {
                    updateField('teamCompleted', false);
                  }
                }}
                className="h-4 w-4 rounded border border-slate-600 bg-slate-900"
              />
              تیم تشکیل شده است
            </label>
            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                checked={form.teamCompleted && form.hasTeam}
                onChange={(event) => updateField('teamCompleted', event.target.checked)}
                disabled={!form.hasTeam}
                className="h-4 w-4 rounded border border-slate-600 bg-slate-900 disabled:opacity-40"
              />
              تیم تکمیل شده است
            </label>
          </div>
          <TextArea
            label="توضیحات تکمیلی"
            placeholder="در صورت نیاز توضیحاتی درباره وضعیت تیم یا پروژه وارد کنید."
            value={form.additionalNotes}
            onChange={(value) => updateField('additionalNotes', value)}
          />
        </section>
      )}

      {step === 1 && (
        <section className="space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold text-slate-200">اعضای تیم</h2>
            <button
              type="button"
              onClick={addMember}
              className="rounded-lg border border-emerald-500 px-3 py-1 text-sm text-emerald-200 transition hover:bg-emerald-500/10"
            >
              افزودن عضو
            </button>
          </div>
          {!form.hasTeam && (
            <p className="text-sm text-slate-400">
              گزینه «تیم تشکیل شده است» را در مرحله قبل فعال کنید تا بتوانید اعضا را اضافه کنید.
            </p>
          )}
          {form.hasTeam && (
            <div className="space-y-4">
              {form.members.map((member, index) => (
                <div key={index} className="space-y-3 rounded-xl border border-slate-800 bg-slate-950/40 p-4">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <span className="text-sm font-medium text-slate-300">عضو {index + 1}</span>
                    {form.members.length > 1 && (
                      <button
                        type="button"
                        onClick={() => removeMember(index)}
                        className="text-xs text-red-300 transition hover:text-red-200"
                      >
                        حذف عضو
                      </button>
                    )}
                  </div>
                  <div className="grid gap-3 md:grid-cols-3">
                    <TextField
                      label="نام و نام خانوادگی"
                      value={member.fullName}
                      onChange={(value) => updateMember(index, 'fullName', value)}
                    />
                    <TextField
                      label="سمت در تیم"
                      value={member.role}
                      onChange={(value) => updateMember(index, 'role', value)}
                    />
                    <TextField
                      label="حوزه فعالیت"
                      value={member.focusArea}
                      onChange={(value) => updateMember(index, 'focusArea', value)}
                    />
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      )}

      {step === 2 && (
        <section className="space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold text-slate-200">مدارک و فایل‌ها</h2>
            <button
              type="button"
              onClick={addDocument}
              className="rounded-lg border border-emerald-500 px-3 py-1 text-sm text-emerald-200 transition hover:bg-emerald-500/10"
            >
              افزودن مدرک
            </button>
          </div>
          {form.documents.length === 0 && (
            <p className="text-sm text-slate-400">در صورت داشتن رزومه، بیزینس پلن یا فایل پروژه می‌توانید آن را در این بخش ثبت کنید.</p>
          )}
          {form.documents.length > 0 && (
            <div className="space-y-4">
              {form.documents.map((document, index) => (
                <div key={index} className="space-y-3 rounded-xl border border-slate-800 bg-slate-950/40 p-4">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <span className="text-sm font-medium text-slate-300">فایل {index + 1}</span>
                    <button
                      type="button"
                      onClick={() => removeDocument(index)}
                      className="text-xs text-red-300 transition hover:text-red-200"
                    >
                      حذف فایل
                    </button>
                  </div>
                  <div className="grid gap-3 md:grid-cols-2">
                    <label className="text-sm text-slate-300">
                      دسته‌بندی فایل
                      <select
                        className="mt-1 w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100"
                        value={document.category}
                        onChange={(event) =>
                          updateDocument(index, 'category', event.target.value as RegistrationDocumentCategory)
                        }
                      >
                        {documentCategoryOptions.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </label>
                    <div className="text-sm text-slate-300">
                      <label className="block">انتخاب فایل (اختیاری)</label>
                      <input
                        type="file"
                        className="mt-1 w-full text-xs text-slate-200"
                        onChange={(event) => handleDocumentFile(index, event.target.files?.[0] ?? null)}
                      />
                      <p className="mt-1 text-xs text-slate-500">در محیط آزمایشی، فایل به صورت محلی ثبت می‌شود.</p>
                    </div>
                    <TextField
                      label="نام فایل"
                      value={document.fileName}
                      onChange={(value) => updateDocument(index, 'fileName', value)}
                    />
                    <TextField
                      label="آدرس فایل یا لینک دانلود"
                      value={document.fileUrl}
                      onChange={(value) => updateDocument(index, 'fileUrl', value)}
                    />
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      )}

      {step === 3 && (
        <section className="space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold text-slate-200">لینک‌ها و شبکه‌های اجتماعی</h2>
            <button
              type="button"
              onClick={addLink}
              className="rounded-lg border border-emerald-500 px-3 py-1 text-sm text-emerald-200 transition hover:bg-emerald-500/10"
            >
              افزودن لینک
            </button>
          </div>
          {form.links.length === 0 && (
            <p className="text-sm text-slate-400">لینک‌های مرتبط با پروژه (لینکدین، گیت‌هاب، وب‌سایت و ...) را ثبت کنید.</p>
          )}
          {form.links.length > 0 && (
            <div className="space-y-4">
              {form.links.map((link, index) => (
                <div key={index} className="space-y-3 rounded-xl border border-slate-800 bg-slate-950/40 p-4">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <span className="text-sm font-medium text-slate-300">لینک {index + 1}</span>
                    <button
                      type="button"
                      onClick={() => removeLink(index)}
                      className="text-xs text-red-300 transition hover:text-red-200"
                    >
                      حذف لینک
                    </button>
                  </div>
                  <div className="grid gap-3 md:grid-cols-3">
                    <label className="text-sm text-slate-300">
                      نوع لینک
                      <select
                        className="mt-1 w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-100"
                        value={link.type}
                        onChange={(event) =>
                          updateLink(index, 'type', event.target.value as RegistrationLinkType)
                        }
                      >
                        {linkTypeOptions.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                    </label>
                    <TextField
                      label="برچسب (اختیاری)"
                      value={link.label ?? ''}
                      onChange={(value) => updateLink(index, 'label', value)}
                    />
                    <TextField
                      label="آدرس لینک"
                      value={link.url}
                      onChange={(value) => updateLink(index, 'url', value)}
                    />
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      )}

      {step === 4 && (
        <section className="space-y-4">
          <h2 className="text-lg font-semibold text-slate-200">بازبینی اطلاعات</h2>
          <div className="space-y-3 text-sm text-slate-200">
            <p>
              <span className="text-slate-400">سرپرست:</span> {form.headFirstName || '—'} {form.headLastName || ''} — {form.phoneNumber || 'بدون شماره'}
            </p>
            <p>
              <span className="text-slate-400">نام تیم:</span> {form.teamName || '—'} |{' '}
              {form.hasTeam ? (form.teamCompleted ? 'تیم تکمیل شده' : 'در حال تکمیل') : 'بدون تیم'}
            </p>
            <p>
              <span className="text-slate-400">مدرک / رشته:</span> {form.educationDegree || '—'} — {form.fieldOfStudy || '—'}
            </p>
            <p>
              <span className="text-slate-400">توضیحات:</span> {form.additionalNotes ? form.additionalNotes : 'بدون توضیحات'}
            </p>
          </div>
          <div className="grid gap-4 md:grid-cols-3">
            <SummaryCard title="اعضای تیم" count={filteredMembers.length} description="اعضای دارای اطلاعات کامل" />
            <SummaryCard title="مدارک بارگذاری‌شده" count={filteredDocuments.length} description="فایل‌های دارای لینک" />
            <SummaryCard title="لینک‌های ثبت‌شده" count={filteredLinks.length} description="لینک‌های دارای آدرس معتبر" />
          </div>
          <p className="text-xs text-slate-400">
            با فشردن دکمه «تأیید و ارسال» اطلاعات شما ثبت می‌شود و امکان مشاهده در داشبورد تیم فعال خواهد شد.
          </p>
        </section>
      )}

      <div className="flex flex-wrap items-center justify-between gap-3">
        <button
          type="button"
          onClick={handlePrevious}
          disabled={step === 0 || submitting}
          className="rounded-lg border border-slate-700 px-4 py-2 text-sm text-slate-200 transition enabled:hover:bg-slate-800 disabled:opacity-40"
        >
          مرحله قبل
        </button>
        {step < totalSteps - 1 && (
          <button
            type="button"
            onClick={handleNext}
            disabled={submitting}
            className="rounded-lg bg-emerald-500 px-4 py-2 text-sm font-medium text-emerald-950 transition hover:bg-emerald-400 disabled:opacity-60"
          >
            مرحله بعد
          </button>
        )}
        {step === totalSteps - 1 && (
          <button
            type="button"
            onClick={handleSubmit}
            disabled={submitting}
            className="rounded-lg bg-emerald-500 px-6 py-2 text-sm font-medium text-emerald-950 transition hover:bg-emerald-400 disabled:opacity-60"
          >
            {submitting ? 'در حال ارسال...' : 'تأیید و ارسال' }
          </button>
        )}
      </div>
    </div>
  );
}

function StepIndicator({ currentStep }: { currentStep: number }) {
  return (
    <ol className="flex flex-wrap items-center gap-3 text-sm text-slate-400">
      {stepLabels.map((label, index) => {
        const isActive = index === currentStep;
        const isCompleted = index < currentStep;
        return (
          <li
            key={label}
            className={`flex items-center gap-2 rounded-full border px-3 py-1 ${
              isActive
                ? 'border-emerald-500/60 bg-emerald-500/10 text-emerald-300'
                : isCompleted
                ? 'border-emerald-500/20 bg-emerald-500/5 text-emerald-200'
                : 'border-slate-700 bg-slate-900/80 text-slate-400'
            }`}
          >
            <span className="text-xs font-semibold">{index + 1}</span>
            <span>{label}</span>
          </li>
        );
      })}
    </ol>
  );
}

function TextField({
  label,
  value,
  onChange,
  type = 'text',
  className = '',
  required
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
  className?: string;
  required?: boolean;
}) {
  return (
    <label className={`text-sm text-slate-300 ${className}`}>
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

function SummaryCard({
  title,
  count,
  description
}: {
  title: string;
  count: number;
  description: string;
}) {
  return (
    <div className="rounded-xl border border-slate-800 bg-slate-950/40 p-4 text-slate-200">
      <h3 className="text-sm font-medium text-slate-300">{title}</h3>
      <div className="mt-3 text-3xl font-semibold text-emerald-300">{count}</div>
      <p className="mt-2 text-xs text-slate-400">{description}</p>
    </div>
  );
}
