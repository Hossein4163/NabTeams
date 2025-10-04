'use client';

import { useRoleSelection } from '../lib/use-role';
import type { Role } from '../lib/api';

const roleLabels: Record<Role, string> = {
  participant: 'شرکت‌کننده',
  judge: 'داور',
  mentor: 'منتور',
  investor: 'سرمایه‌گذار',
  admin: 'ادمین'
};

export function RoleSwitcher() {
  const { role, roles, setRole } = useRoleSelection();

  return (
    <div className="flex flex-wrap items-center gap-3 text-sm">
      <span className="text-slate-400">نقش فعال:</span>
      <div className="flex flex-wrap gap-2">
        {roles.map((item) => (
          <button
            key={item}
            onClick={() => setRole(item)}
            className={`rounded-full border px-3 py-1 transition ${
              role === item ? 'border-emerald-400 bg-emerald-500/20 text-emerald-100' : 'border-slate-700 bg-slate-800'
            }`}
          >
            {roleLabels[item] ?? item}
          </button>
        ))}
      </div>
    </div>
  );
}
