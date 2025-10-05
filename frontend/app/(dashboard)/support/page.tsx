import Link from 'next/link';
import { SupportPanel } from '../../../components/support-panel';

export default function SupportPage() {
  return (
    <div className="space-y-6">
      <Link href="/" className="text-sm text-slate-400 hover:text-slate-200">
        ← بازگشت به داشبورد
      </Link>
      <SupportPanel />
    </div>
  );
}
