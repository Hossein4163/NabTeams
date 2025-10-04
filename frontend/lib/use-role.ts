'use client';

import { useEffect, useState } from 'react';

export function useRole() {
  const [role, setRole] = useState('participant');

  useEffect(() => {
    const stored = localStorage.getItem('nabteams:role');
    if (stored) {
      setRole(stored);
    }

    const handleStorage = (event: StorageEvent) => {
      if (event.key === 'nabteams:role') {
        setRole(event.newValue || 'participant');
      }
    };

    window.addEventListener('storage', handleStorage);

    return () => {
      window.removeEventListener('storage', handleStorage);
    };
  }, []);

  return role;
}
