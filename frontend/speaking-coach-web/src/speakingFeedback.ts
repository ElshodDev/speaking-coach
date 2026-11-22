import type { CorrectionItem, ScoreWithReasoning } from './ui';

// Gapirish natijasining 4 mezonli ko'rinishi (backend: Features:SpeakingFourCriteria).
// Yangi maydonlar IXTIYORIY: eski natijalarda (yoki flag o'chiq bo'lsa) yo'q — u holda eski ko'rinish ishlaydi.

export interface SpeakingBands {
  fluency: number;
  lexical: number;
  grammar: number;
  pronunciation: number;
}

export interface PriorityFix {
  issue: string;
  original: string;
  corrected: string;
  tip: string;
}

export interface SpeakingEvaluationLike {
  fluency: ScoreWithReasoning;
  grammar: ScoreWithReasoning;
  vocabulary: ScoreWithReasoning;
  pronunciation?: ScoreWithReasoning | null;
  bands?: SpeakingBands | null;
  priorityFix?: PriorityFix | null;
  overallBand?: number | null;
  topCorrections: CorrectionItem[];
}

type CriterionKey = 'fluency' | 'lexical' | 'grammar' | 'pronunciation';

export interface CriterionRow {
  key: CriterionKey;
  band: number;
  reasoning: string;
}

const isBand = (n: unknown): n is number => typeof n === 'number' && Number.isFinite(n) && n >= 0 && n <= 9;

/** To'rt mezon bo'yicha qatorlar; yangi ma'lumot yo'q yoki yaroqsiz bo'lsa — null (eski ko'rinish). */
export function criteriaRows(e: SpeakingEvaluationLike): CriterionRow[] | null {
  const b = e.bands;
  if (!b || !isBand(b.fluency) || !isBand(b.lexical) || !isBand(b.grammar) || !isBand(b.pronunciation)) return null;
  return [
    { key: 'fluency', band: b.fluency, reasoning: e.fluency.reasoning },
    { key: 'lexical', band: b.lexical, reasoning: e.vocabulary.reasoning },
    { key: 'grammar', band: b.grammar, reasoning: e.grammar.reasoning },
    { key: 'pronunciation', band: b.pronunciation, reasoning: e.pronunciation?.reasoning ?? '' },
  ];
}

/** Umumiy taxminiy band: serverdan; bo'lmasa — to'rt mezonning yarim bandgacha yaxlitlangan o'rtachasi. */
export function overallBandOf(e: SpeakingEvaluationLike): number | null {
  const rows = criteriaRows(e);
  if (!rows) return null;
  if (isBand(e.overallBand)) return e.overallBand;
  const avg = rows.reduce((s, r) => s + r.band, 0) / rows.length;
  return Math.floor(avg * 2 + 0.5) / 2;
}

/** "6" → "6.0", "5.5" → "5.5". */
export function formatBand(band: number): string {
  return band.toFixed(1);
}

/** Rang sinfi (mavjud txt-* sinflari): ≥6.5 yaxshi, ≥5 o'rtacha, undan past — past. */
export function bandTone(band: number): 'great' | 'mid' | 'low' {
  return band >= 6.5 ? 'great' : band >= 5 ? 'mid' : 'low';
}

/** Qolgan tuzatishlar: asosiy tuzatish alohida ko'rsatilgani uchun ro'yxatda takrorlanmaydi (asl ibora bo'yicha). */
export function otherCorrections(e: SpeakingEvaluationLike): CorrectionItem[] {
  const fix = priorityFixOf(e);
  if (!fix || !fix.original.trim()) return e.topCorrections;
  const key = fix.original.trim().toLowerCase();
  return e.topCorrections.filter((c) => c.original.trim().toLowerCase() !== key);
}

/**
 * Har urinishda BITTA asosiy tuzatish: serverdagi priorityFix; bo'lmasa — birinchi tuzatish
 * (eng muhim deb tanlangan). Hech narsa yo'q bo'lsa — null.
 */
export function priorityFixOf(e: SpeakingEvaluationLike): PriorityFix | null {
  const f = e.priorityFix;
  if (f && f.issue.trim() && f.corrected.trim() && f.tip.trim()) return f;
  const c = e.topCorrections[0];
  if (!c || !c.corrected.trim()) return null;
  return { issue: c.explanation, original: c.original, corrected: c.corrected, tip: '' };
}
