import Link from 'next/link';
import { ChatPanel } from '../../../components/chat-panel';

export default function GlobalChatPage() {
  return (
    <div className="space-y-6">
      <Link href="/" className="text-sm text-slate-400 hover:text-slate-200">
        ← بازگشت به داشبورد
      </Link>
      <ChatPanel />
    </div>
  );
}
