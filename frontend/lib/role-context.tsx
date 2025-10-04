'use client';

import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { useSession } from 'next-auth/react';
import { Role } from './api';

interface RoleContextValue {
  role: Role;
  roles: Role[];
  setRole: (role: Role) => void;
}

const RoleContext = createContext<RoleContextValue | undefined>(undefined);

export function RoleProvider({ children }: { children: React.ReactNode }) {
  const { data } = useSession();
  const rawRoles = useMemo(() => {
    const claimRoles = (data?.user?.roles ?? []) as string[];
    if (claimRoles.length > 0) {
      return claimRoles.map((role) => role.toLowerCase()) as Role[];
    }
    return ['participant'] as Role[];
  }, [data?.user?.roles]);

  const defaultRole = rawRoles[0] ?? 'participant';
  const [role, setRole] = useState<Role>(defaultRole);

  useEffect(() => {
    setRole(defaultRole);
  }, [defaultRole]);

  const value = useMemo(
    () => ({
      role,
      roles: rawRoles,
      setRole: (next: Role) => {
        if (rawRoles.includes(next)) {
          setRole(next);
        }
      }
    }),
    [role, rawRoles]
  );

  return <RoleContext.Provider value={value}>{children}</RoleContext.Provider>;
}

export function useRoleContext() {
  const context = useContext(RoleContext);
  if (!context) {
    throw new Error('useRoleContext must be used within a RoleProvider');
  }
  return context;
}
