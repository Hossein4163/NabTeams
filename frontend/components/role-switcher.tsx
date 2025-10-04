'use client';

import { useState } from 'react';

const roles = [
  { value: 'participant', label: 'شرکت‌کننده' },
  { value: 'judge', label: 'داور' },
  { value: 'mentor', label: 'منتور' },
  { value: 'investor', label: 'سرمایه‌گذار' },
  { value: 'admin', label: 'ادمین' }
];

export function RoleSwitcher() {
  const [role, setRole] = useState('participant');

  return (
    <div className="flex flex-wrap items-center gap-3 text-sm">
      <span className="text-slate-400">نقش آزمایشی:</span>
      <div className="flex gap-2">
        {roles.map((item) => (
          <button
            key={item.value}
            onClick={() => {
              localStorage.setItem('nabteams:role', item.value);
              setRole(item.value);
            }}
            className={`rounded-full border px-3 py-1 transition ${
              role === item.value ? 'border-emerald-400 bg-emerald-500/20 text-emerald-100' : 'border-slate-700 bg-slate-800'
            }`}
          >
            {item.label}
          </button>
        ))}
      </div>
    </div>
  );
}
