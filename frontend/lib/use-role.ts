'use client';

import { useRoleContext } from './role-context';

export function useRole() {
  return useRoleContext().role;
}

export function useRoleSelection() {
  const { role, setRole, roles } = useRoleContext();
  return { role, setRole, roles };
}
