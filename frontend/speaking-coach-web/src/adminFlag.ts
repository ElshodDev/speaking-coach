// Joriy foydalanuvchi admin'mi — profil yuklangach App.tsx setIsAdmin() ni
// chaqiradi. Ichki (tekshiruv) vositalari, masalan AI bahosining
// barqarorlik testi, faqat admin'ga ko'rsatiladi. Xavfsizlik chegarasi
// emas (server o'zi tekshiradi) — faqat interfeysni soddalashtirish uchun.
import { useSyncExternalStore } from 'react';

let isAdmin = false;
const listeners = new Set<() => void>();

export function setIsAdmin(v: boolean) {
  if (isAdmin === v) return;
  isAdmin = v;
  listeners.forEach((l) => l());
}

export const getIsAdmin = (): boolean => isAdmin;

function subscribe(listener: () => void) {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

/** Admin bo'lsa true; profil o'zgarganda komponent qayta chiziladi. */
export function useIsAdmin(): boolean {
  return useSyncExternalStore(subscribe, getIsAdmin, () => false);
}
