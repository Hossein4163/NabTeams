import './globals.css';
import type { Metadata } from 'next';
import { Rubik } from 'next/font/google';

const rubik = Rubik({ subsets: ['latin', 'arabic'] });

export const metadata: Metadata = {
  title: 'NabTeams Dashboard',
  description: 'Role-based collaboration with AI moderation and support'
};

export default function RootLayout({
  children
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="fa" dir="rtl">
      <body className={`${rubik.className} bg-slate-950 text-slate-100 min-h-screen`}>
        <main className="max-w-6xl mx-auto px-4 py-6">{children}</main>
      </body>
    </html>
  );
}
