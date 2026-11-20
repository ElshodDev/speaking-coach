// "Ona tili tuzoqlari" testi — toza funksiyalar (l1Logic.test.ts tekshiradi).
import type { Lang } from './i18n';
import { ALL_TRAPS, TRAP_SETS, type TrapItem } from './content/l1traps';

export const BLANK = '___';

/** Variantlarni aralashtiradi (urug' bilan — test va qayta chizishda barqaror). To'g'ri javob — options[0]. */
export function shuffled<T>(arr: T[], seed: number): T[] {
  const a = [...arr];
  let x = seed >>> 0 || 1;
  for (let i = a.length - 1; i > 0; i--) {
    x = (x * 1664525 + 1013904223) >>> 0;
    const j = x % (i + 1);
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

/** Gapdagi "___" o'rniga javob; "(nothing)" — bo'sh joy olib tashlanadi. */
export function fill(q: string, option: string): string {
  if (option === '(nothing)') return q.replace(` ${BLANK}`, '').replace(BLANK, '');
  return q.replace(BLANK, option);
}

/** To'plam yoki xato turi bo'yicha savollar: "cat-prepositions" — barcha to'plamlardan shu tur. */
export function itemsFor(key: string): TrapItem[] {
  if (key.startsWith('cat-')) return ALL_TRAPS.filter((t) => t.cat === key.slice(4));
  return TRAP_SETS.find((s) => s.id === key)?.items ?? [];
}

/** Xato turi bo'yicha test bormi ("Mening xatolarim" sahifasida tugma ko'rsatish uchun). */
export function hasTrapsFor(category: string): boolean {
  return ALL_TRAPS.some((t) => t.cat === category);
}

export interface Answer {
  id: string;
  chosen: string;
  correct: boolean;
}

export function score(answers: Answer[]): { right: number; total: number; pct: number } {
  const right = answers.filter((a) => a.correct).length;
  const total = answers.length;
  return { right, total, pct: total ? Math.round((100 * right) / total) : 0 };
}

/** Xato javoblar → takrorlash kartalari: old — gap (ikki variant bilan), orqa — to'g'ri gap, izoh — tushuntirish. */
export function cardsFromWrong(answers: Answer[], lang: Lang): { front: string; back: string; note: string }[] {
  return answers
    .filter((a) => !a.correct)
    .map((a) => ALL_TRAPS.find((t) => t.id === a.id))
    .filter((t): t is TrapItem => !!t)
    .map((t) => ({
      front: `${t.q} (${t.options.join(' / ')})`,
      back: fill(t.q, t.options[0]),
      note: t.explain[lang],
    }));
}

const BEST_KEY = 'speakingCoach.l1best';

/** Eng yaxshi natija (foiz) — to'plam bo'yicha, brauzerda. */
export function loadBest(): Record<string, number> {
  try {
    const v = JSON.parse(localStorage.getItem(BEST_KEY) ?? '{}');
    return v && typeof v === 'object' ? v : {};
  } catch {
    return {};
  }
}

export function saveBest(key: string, pct: number): void {
  try {
    const all = loadBest();
    if ((all[key] ?? -1) < pct) localStorage.setItem(BEST_KEY, JSON.stringify({ ...all, [key]: pct }));
  } catch {
    // shaxsiy rejim / bloklangan — muhim emas
  }
}
