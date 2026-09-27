// Ekran kengligi: telefon (asosiy), planshet (760 px+) va kompyuter (1024 px+).
// Kompyuterda sahifalar ekranni to'liq egallaydi: chapda menyu, mazmun ikki
// ustunda, imtihon matni va savollar yonma-yon. CSS o'zi yetmaydigan joyda
// (masalan tablar o'rniga ikki panel) komponent shu hook bilan tanlaydi.
import { useSyncExternalStore } from 'react';

export const WIDE_QUERY = '(min-width: 1024px)';

function subscribe(onChange: () => void) {
  if (typeof window === 'undefined' || !window.matchMedia) return () => undefined;
  const mq = window.matchMedia(WIDE_QUERY);
  mq.addEventListener('change', onChange);
  return () => mq.removeEventListener('change', onChange);
}

const snapshot = () => (typeof window !== 'undefined' && !!window.matchMedia && window.matchMedia(WIDE_QUERY).matches);

/** Kompyuter ekranimi (1024 px va kengroq). */
export function useWide(): boolean {
  return useSyncExternalStore(subscribe, snapshot, () => false);
}

/** Klaviatura yorlig'i matn kiritish joyida bosilganmi (u holda tugmalar ishlamasin). */
export function typingTarget(e: KeyboardEvent): boolean {
  const el = e.target as HTMLElement | null;
  if (!el) return false;
  const tag = el.tagName;
  return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT' || el.isContentEditable;
}

/**
 * Probel/Enter fokusdagi tugmani o'zi bosadi — bunday holda sahifaning
 * yorlig'i ishlamasin. Menyu tugmasi bundan mustasno: sahifaga o'tgandan
 * keyin fokus menyuda qolgan bo'ladi, Probel esa sahifa uchun mo'ljallangan.
 */
export function focusedButton(e: KeyboardEvent): boolean {
  const el = e.target as HTMLElement | null;
  return !!el && el.tagName === 'BUTTON' && !el.closest('nav');
}
