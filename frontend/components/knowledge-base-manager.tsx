'use client';

import { useEffect, useMemo, useState } from 'react';
import { useSession } from 'next-auth/react';
import Link from 'next/link';
import {
  deleteKnowledgeBaseItem,
  fetchKnowledgeBase,
  KnowledgeBaseItem,
  upsertKnowledgeBaseItem
} from '../lib/api';

interface EditorState {
  id?: string;
  title: string;
  body: string;
  audience: string;
  tags: string;
}

const initialState: EditorState = {
  title: '',
  body: '',
  audience: 'all',
  tags: ''
};

export function KnowledgeBaseManager() {
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
  const [items, setItems] = useState<KnowledgeBaseItem[]>([]);
  const [editor, setEditor] = useState<EditorState>(initialState);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    if (!isAdmin) {
      setItems([]);
      return;
    }

    let cancelled = false;
    async function load() {
      setLoading(true);
      setError(null);
      try {
        const data = await fetchKnowledgeBase(auth);
        if (!cancelled) {
          setItems(data);
        }
      } catch (err) {
        if (!cancelled) {
          setError((err as Error).message);
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    }

    load();
    return () => {
      cancelled = true;
    };
  }, [auth, isAdmin]);

  const audiences = useMemo(
    () => [
      { value: 'all', label: 'همه نقش‌ها' },
      { value: 'participant', label: 'شرکت‌کنندگان' },
      { value: 'mentor', label: 'منتورها' },
      { value: 'judge', label: 'داوران' },
      { value: 'investor', label: 'سرمایه‌گذاران' },
      { value: 'admin', label: 'ادمین‌ها' }
    ],
    []
  );

  const handleReset = () => {
    setEditor(initialState);
    setSuccess(null);
    setError(null);
  };

  const handleEdit = (item: KnowledgeBaseItem) => {
    setEditor({
      id: item.id,
      title: item.title,
      body: item.body,
      audience: item.audience,
      tags: item.tags.join(', ')
    });
    setSuccess(null);
    setError(null);
  };

  const handleDelete = async (id: string) => {
    if (!confirm('آیا از حذف این منبع مطمئن هستید؟')) {
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await deleteKnowledgeBaseItem(id, auth);
      setItems((prev) => prev.filter((item) => item.id !== id));
      if (editor.id === id) {
        handleReset();
      }
      setSuccess('منبع با موفقیت حذف شد.');
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setSaving(false);
    }
  };

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!editor.title.trim() || !editor.body.trim()) {
      setError('عنوان و محتوای منبع اجباری است.');
      return;
    }

    setSaving(true);
    setError(null);
    setSuccess(null);
    try {
      const payload = {
        id: editor.id,
        title: editor.title.trim(),
        body: editor.body.trim(),
        audience: editor.audience,
        tags: editor.tags
          .split(',')
          .map((tag) => tag.trim())
          .filter(Boolean)
      };
      const saved = await upsertKnowledgeBaseItem(payload, auth);
      setItems((prev) => {
        const exists = prev.find((item) => item.id === saved.id);
        if (exists) {
          return prev.map((item) => (item.id === saved.id ? saved : item));
        }
        return [saved, ...prev];
      });
      setEditor({
        id: saved.id,
        title: saved.title,
        body: saved.body,
        audience: saved.audience,
        tags: saved.tags.join(', ')
      });
      setSuccess('منبع ذخیره شد.');
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setSaving(false);
    }
  };

  if (!isAdmin) {
    return (
      <div className="rounded-2xl border border-amber-500/40 bg-amber-500/10 p-6 text-sm text-amber-100">
        دسترسی به مدیریت دانش تنها برای ادمین‌ها فراهم است. در صورت نیاز لطفاً با تیم پشتیبانی تماس بگیرید یا{' '}
        <Link href="/auth/signin" className="underline">
          با حساب کاربری دیگری وارد شوید
        </Link>
        .
      </div>
    );
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[2fr_3fr]">
      <section className="space-y-4 rounded-2xl border border-slate-800 bg-slate-900/70 p-5">
        <header className="space-y-2">
          <h2 className="text-xl font-semibold">افزودن / ویرایش منبع دانش</h2>
          <p className="text-sm text-slate-400">
            منابع منتشرشده در اینجا به‌صورت خودکار برای چت پشتیبانی قابل استفاده است. نقش هدف و برچسب‌ها را دقیق وارد کنید.
          </p>
        </header>
        <form onSubmit={handleSubmit} className="space-y-4">
          <label className="flex flex-col gap-2 text-sm">
            <span className="text-slate-300">عنوان</span>
            <input
              className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
              value={editor.title}
              onChange={(event) => setEditor((prev) => ({ ...prev, title: event.target.value }))}
            />
          </label>
          <label className="flex flex-col gap-2 text-sm">
            <span className="text-slate-300">مخاطب</span>
            <select
              className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
              value={editor.audience}
              onChange={(event) => setEditor((prev) => ({ ...prev, audience: event.target.value }))}
            >
              {audiences.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label className="flex flex-col gap-2 text-sm">
            <span className="text-slate-300">برچسب‌ها (با کاما جدا کنید)</span>
            <input
              className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100"
              value={editor.tags}
              onChange={(event) => setEditor((prev) => ({ ...prev, tags: event.target.value }))}
            />
          </label>
          <label className="flex flex-col gap-2 text-sm">
            <span className="text-slate-300">متن پاسخ</span>
            <textarea
              className="rounded-lg border border-slate-700 bg-slate-950 px-3 py-3 text-sm text-slate-100"
              rows={6}
              value={editor.body}
              onChange={(event) => setEditor((prev) => ({ ...prev, body: event.target.value }))}
            />
          </label>
          <div className="flex items-center gap-3">
            <button
              type="submit"
              disabled={saving}
              className="rounded-lg bg-emerald-500 px-4 py-2 text-sm font-medium text-emerald-950 disabled:opacity-60"
            >
              {saving ? 'در حال ذخیره...' : 'ذخیره منبع'}
            </button>
            <button
              type="button"
              onClick={handleReset}
              className="rounded-lg border border-slate-600 px-4 py-2 text-sm text-slate-200"
            >
              ایجاد منبع جدید
            </button>
          </div>
          {success && <p className="text-sm text-emerald-300">{success}</p>}
          {error && <p className="text-sm text-rose-300">{error}</p>}
        </form>
      </section>

      <section className="space-y-4 rounded-2xl border border-slate-800 bg-slate-900/70 p-5">
        <header className="flex items-center justify-between">
          <h2 className="text-xl font-semibold">منابع موجود</h2>
          {loading && <span className="text-xs text-slate-400">در حال بارگذاری...</span>}
        </header>
        {items.length === 0 ? (
          <p className="text-sm text-slate-400">منبعی ثبت نشده است.</p>
        ) : (
          <ul className="space-y-4">
            {items.map((item) => (
              <li key={item.id} className="rounded-xl border border-slate-800 bg-slate-950/60 p-4">
                <div className="flex flex-wrap items-center justify-between gap-3 text-sm">
                  <div>
                    <h3 className="text-lg font-semibold text-slate-100">{item.title}</h3>
                    <p className="text-slate-400">مخاطب: {item.audience}</p>
                  </div>
                  <div className="flex gap-2">
                    <button
                      onClick={() => handleEdit(item)}
                      className="rounded-lg border border-slate-600 px-3 py-1 text-xs text-slate-200"
                    >
                      ویرایش
                    </button>
                    <button
                      onClick={() => handleDelete(item.id)}
                      className="rounded-lg border border-rose-600 px-3 py-1 text-xs text-rose-200"
                    >
                      حذف
                    </button>
                  </div>
                </div>
                <p className="mt-3 text-sm leading-6 text-slate-100 whitespace-pre-wrap">{item.body}</p>
                {item.tags.length > 0 && (
                  <div className="mt-3 flex flex-wrap gap-2 text-xs text-indigo-200">
                    {item.tags.map((tag) => (
                      <span key={tag} className="rounded-full bg-indigo-900/40 px-2 py-1">
                        #{tag}
                      </span>
                    ))}
                  </div>
                )}
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
