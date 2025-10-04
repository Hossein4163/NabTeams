import Link from 'next/link';
import { AppealsPanel } from '../../components/appeals-panel';

export default function AppealsPage() {
  return (
    <div className="space-y-6">
      <Link href="/" className="text-sm text-slate-400 hover:text-slate-200">
        ← بازگشت به داشبورد
      </Link>
      <AppealsPanel />
    </div>
  );
}
