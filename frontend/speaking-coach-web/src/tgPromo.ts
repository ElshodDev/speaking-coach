// Bosh sahifadagi "Telegramni ulang" taklifi: "Keyinroq" bosilsa, shuncha kun ko'rsatilmaydi.
const KEY = 'sc.tgPromo.dismissedAt';
export const PROMO_SNOOZE_DAYS = 7;

/** Yashirilgan vaqt (ms) yoki null; yaroqsiz qiymat — null. */
export function readPromoDismissedAt(): number | null {
  try {
    const v = Number(localStorage.getItem(KEY));
    return Number.isFinite(v) && v > 0 ? v : null;
  } catch {
    return null;
  }
}

export function savePromoDismissedAt(now: number) {
  try {
    localStorage.setItem(KEY, String(now));
  } catch {
    // Saqlab bo'lmasa — taklif shu sahifa yopilguncha yashirin turadi, xolos.
  }
}

/** Yashirilmagan yoki muddati tugagan bo'lsa — ko'rsatamiz. Kelajakdagi (soat siljishi) qiymat — ko'rsatamiz. */
export function shouldShowPromo(dismissedAt: number | null, now: number): boolean {
  if (dismissedAt === null) return true;
  if (dismissedAt > now) return true;
  return now - dismissedAt >= PROMO_SNOOZE_DAYS * 24 * 60 * 60 * 1000;
}
