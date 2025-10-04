import { KnowledgeBaseManager } from '../../../components/knowledge-base-manager';

export default function KnowledgeBasePage() {
  return (
    <div className="space-y-6">
      <header className="space-y-2">
        <h1 className="text-3xl font-semibold">مدیریت دانش ادمین</h1>
        <p className="text-slate-300 max-w-3xl text-sm leading-6">
          در این بخش می‌توانید منابع دانش را برای نقش‌های مختلف اضافه یا به‌روزرسانی کنید. تغییرات بلافاصله در چت پشتیبانی قابل
          استفاده خواهد بود.
        </p>
      </header>
      <KnowledgeBaseManager />
    </div>
  );
}
