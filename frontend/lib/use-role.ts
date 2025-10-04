'use client';

import { useEffect, useState } from 'react';

export function useRole() {
  const [role, setRole] = useState('participant');

  useEffect(() => {
    const stored = localStorage.getItem('nabteams:role');
    if (stored) {
      setRole(stored);
    }
  }, []);

  return role;
}
