// "Javobimni yaxshila" — toza funksiyalar (upgradeLogic.test.ts tekshiradi).

export interface UpgradeItem {
  original: string;
  upgraded: string;
  kind: 'vocabulary' | 'grammar' | 'linking' | 'structure';
  why: string;
}

export interface UpgradeResult {
  target: Target;
  upgradedText: string;
  items: UpgradeItem[];
  summary: string;
}

/** Server bilan bir xil (AnswerUpgrade.MinWords): bundan qisqa javob yaxshilanmaydi. */
export const MIN_WORDS = 15;

export const TARGETS = ['B1', 'B2', 'C1'] as const;
export type Target = (typeof TARGETS)[number];

/** Taxminiy IELTS mosligi (maqsad tugmalarida ko'rsatiladi). */
export const IELTS_HINT: Record<Target, string> = { B1: '4–5', B2: '5.5–6.5', C1: '7–8' };

/** Standart maqsad — o'quvchi darajasidan bir pog'ona yuqori (server bilan bir xil qoida). */
export function defaultTarget(level: string): Target {
  return level === 'A2' ? 'B1' : level === 'B1' ? 'B2' : 'C1';
}

export interface Segment {
  text: string;
  /** Yaxshilangan ibora bo'lsa — items ichidagi raqami. */
  item?: number;
}

/**
 * O'quvchi matnini bo'laklarga ajratadi: yaxshilangan iboralar (katta-kichik harfsiz, birinchi uchrashuvi)
 * belgilanadi — asl matnda nima o'zgarganini ko'rsatish uchun. Kesishgan iboralardan avvalgisi qoladi.
 */
export function highlight(text: string, items: UpgradeItem[]): Segment[] {
  const lower = text.toLowerCase();
  const ranges: { start: number; end: number; item: number }[] = [];
  items.forEach((it, i) => {
    const needle = it.original.trim().replace(/[.!?,]+$/, '').toLowerCase();
    if (!needle) return;
    const start = lower.indexOf(needle);
    if (start < 0) return;
    const end = start + needle.length;
    if (ranges.some((r) => start < r.end && end > r.start)) return;
    ranges.push({ start, end, item: i });
  });
  ranges.sort((a, b) => a.start - b.start);
  const out: Segment[] = [];
  let pos = 0;
  for (const r of ranges) {
    if (r.start > pos) out.push({ text: text.slice(pos, r.start) });
    out.push({ text: text.slice(r.start, r.end), item: r.item });
    pos = r.end;
  }
  if (pos < text.length) out.push({ text: text.slice(pos) });
  return out;
}

/** So'zlar soni (uzunlikni solishtirish uchun). */
export function wordCount(text: string): number {
  return (text.match(/[\p{L}\p{M}'’]+/gu) ?? []).length;
}
