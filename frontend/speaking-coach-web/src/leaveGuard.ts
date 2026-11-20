import { useLayoutEffect } from 'react';

// Imtihon davomida sahifadan chiqishdan himoya. Faqat "beforeunload" yetarli emas:
// ilova hash bilan marshrutlanadi, shuning uchun Android "Orqaga", Telegram'ning
// BackButton'i, pastki menyu va havolalar sahifani yopmaydi — faqat #/... ni
// o'zgartiradi. Himoya yoqilganda useRoute har bir o'tishdan oldin so'raydi.

const active = new Map<symbol, string>();

/** Himoya yoqilganmi (ochiq imtihon bormi). */
export function isGuarded(): boolean {
  return active.size > 0;
}

/** Himoya xabari (oxirgi yoqilgani) yoki null. */
export function guardMessage(): string | null {
  let last: string | null = null;
  for (const m of active.values()) last = m;
  return last;
}

/** Himoyani qo'lda yoqadi; qaytgan funksiya o'chiradi. Testlar va hook uchun. */
export function addLeaveGuard(message: string): () => void {
  const token = Symbol('leave-guard');
  active.set(token, message);
  return () => {
    active.delete(token);
  };
}

/**
 * Chiqishga ruxsat bormi: himoya yo'q bo'lsa — ha; bo'lsa — foydalanuvchidan
 * so'raladi. Rozi bo'lsa himoya olib tashlanadi (keyingi hashchange qayta so'ramasin).
 */
export function confirmLeave(ask: (message: string) => boolean = (m) => window.confirm(m)): boolean {
  const message = guardMessage();
  if (message === null) return true;
  if (!ask(message)) return false;
  active.clear();
  return true;
}

/**
 * Imtihon komponenti uchun: `on` rost bo'lganda ichki o'tishlar so'raladi va
 * sahifani yopish/yangilashda brauzer ogohlantiradi. useLayoutEffect —
 * holat o'zgargan zahoti (natijaga o'tishdan oldin) himoya o'chadi.
 */
export function useLeaveGuard(on: boolean, message: string): void {
  useLayoutEffect(() => {
    if (!on) return;
    const remove = addLeaveGuard(message);
    const onBeforeUnload = (e: BeforeUnloadEvent) => {
      if (!isGuarded()) return;
      e.preventDefault();
      e.returnValue = message;
    };
    window.addEventListener('beforeunload', onBeforeUnload);
    return () => {
      remove();
      window.removeEventListener('beforeunload', onBeforeUnload);
    };
  }, [on, message]);
}
