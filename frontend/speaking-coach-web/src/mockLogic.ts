// Mock imtihon uchun toza (sof) funksiyalar — komponentlardan ajratilgan,
// shuning uchun unit test bilan tekshiriladi.

export interface CueCard {
  topic: string;
  points: string[];
  explain: string;
}

export interface SpeakingSet {
  id: string;
  part1Topic: string;
  part1: string[];
  part2: CueCard;
  part3: string[];
}

export interface SpeakingTiming {
  part1AnswerSeconds: number;
  part2PrepSeconds: number;
  part2SpeakSeconds: number;
  part3AnswerSeconds: number;
}

/** Qadamda ko'rsatiladigan kartochka (IELTS 2-qism, CEFR rasmlari, bahs jadvali). */
export interface StepCard {
  title: string;
  bullets?: string[];
  bulletsHeading?: 'youShouldSay' | 'questions';
  footer?: string;
  pictures?: { emoji: string; caption: string }[];
  table?: { for: string[]; against: string[] };
}

export type SpeakingStep =
  | {
      kind: 'answer';
      index: number;
      part: number;
      partLabel: string;
      text: string;
      maxSeconds: number;
      numberInPart: number;
      ofInPart: number;
      /** Savol ovoz bilan o'qiladimi (uzun nutq qismlarida — yo'q, kartochka bor). */
      speak: boolean;
      /** Uzun nutq (2 daqiqa) — "Tugatdim" tugmasi va eslatmalar ko'rinadi. */
      longTurn: boolean;
      card?: StepCard;
    }
  | { kind: 'prep'; index: number; partLabel: string; seconds: number; card: StepCard };

/**
 * IELTS imtihon qadamlari tartibda: Part 1 savollari → Part 2 tayyorgarlik →
 * Part 2 nutq → Part 3 savollari. `index` — serverdagi savol raqami
 * (javob fayli shu raqam bilan yuboriladi: a0, a1 …).
 */
export function speakingSteps(set: SpeakingSet, t: SpeakingTiming): SpeakingStep[] {
  const steps: SpeakingStep[] = [];
  const card: StepCard = { title: set.part2.topic, bullets: set.part2.points, bulletsHeading: 'youShouldSay', footer: set.part2.explain };
  const q = (part: 1 | 3, i: number, text: string, of: number, max: number, index: number): SpeakingStep => ({
    kind: 'answer', index, part, partLabel: String(part), text, maxSeconds: max, numberInPart: i + 1, ofInPart: of, speak: true, longTurn: false,
  });
  set.part1.forEach((text, i) => steps.push(q(1, i, text, set.part1.length, t.part1AnswerSeconds, i)));
  const p2 = set.part1.length;
  steps.push({ kind: 'prep', index: p2, partLabel: '2', seconds: t.part2PrepSeconds, card });
  steps.push({ kind: 'answer', index: p2, part: 2, partLabel: '2', text: set.part2.topic, maxSeconds: t.part2SpeakSeconds, numberInPart: 1, ofInPart: 1, speak: false, longTurn: true, card });
  set.part3.forEach((text, i) => steps.push(q(3, i, text, set.part3.length, t.part3AnswerSeconds, p2 + 1 + i)));
  return steps;
}

// ---- CEFR (Multilevel) ----

export interface CefrSpeakingSet {
  id: string;
  part11: string[];
  pictures: { emoji: string; caption: string }[];
  part12: string[];
  part2Topic: string;
  part2Questions: string[];
  part3Statement: string;
  for: string[];
  against: string[];
}

export interface CefrSpeakingTiming {
  part11Seconds: number;
  part12Seconds: number;
  part2PrepSeconds: number;
  part2SpeakSeconds: number;
  part3PrepSeconds: number;
  part3SpeakSeconds: number;
}

/** CEFR: 1.1 (3 savol) → 1.2 (rasmlar, 3 savol) → 2 (tayyorgarlik + nutq) → 3 (tayyorgarlik + bahs). */
export function cefrSpeakingSteps(set: CefrSpeakingSet, t: CefrSpeakingTiming): SpeakingStep[] {
  const steps: SpeakingStep[] = [];
  set.part11.forEach((text, i) =>
    steps.push({ kind: 'answer', index: i, part: 1, partLabel: '1.1', text, maxSeconds: t.part11Seconds, numberInPart: i + 1, ofInPart: set.part11.length, speak: true, longTurn: false }),
  );
  const pics: StepCard = { title: '', pictures: set.pictures };
  const base = set.part11.length;
  set.part12.forEach((text, i) =>
    steps.push({ kind: 'answer', index: base + i, part: 1, partLabel: '1.2', text, maxSeconds: t.part12Seconds, numberInPart: i + 1, ofInPart: set.part12.length, speak: true, longTurn: false, card: pics }),
  );
  const i2 = base + set.part12.length;
  const card2: StepCard = { title: set.part2Topic, bullets: set.part2Questions, bulletsHeading: 'questions' };
  steps.push({ kind: 'prep', index: i2, partLabel: '2', seconds: t.part2PrepSeconds, card: card2 });
  steps.push({ kind: 'answer', index: i2, part: 2, partLabel: '2', text: set.part2Topic, maxSeconds: t.part2SpeakSeconds, numberInPart: 1, ofInPart: 1, speak: false, longTurn: true, card: card2 });
  const card3: StepCard = { title: set.part3Statement, table: { for: set.for, against: set.against } };
  steps.push({ kind: 'prep', index: i2 + 1, partLabel: '3', seconds: t.part3PrepSeconds, card: card3 });
  steps.push({ kind: 'answer', index: i2 + 1, part: 3, partLabel: '3', text: set.part3Statement, maxSeconds: t.part3SpeakSeconds, numberInPart: 1, ofInPart: 1, speak: false, longTurn: true, card: card3 });
  return steps;
}

/** CEFR 0–75 → daraja (rasmiy chegaralar, uzbmb.uz). */
export function cefrLevel(score: number): 'C1' | 'B2' | 'B1' | 'below B1' {
  return score >= 65 ? 'C1' : score >= 51 ? 'B2' : score >= 38 ? 'B1' : 'below B1';
}

/** So'z soni oraliqqa nisbatan: kam / mos / ko'p. */
export function wordRangeStatus(n: number, min: number, max: number): 'low' | 'ok' | 'high' {
  return n < min ? 'low' : n > max ? 'high' : 'ok';
}

/** Band ko'rinishi: 6.5, 7.0 (IELTS natijalaridagidek bitta kasr raqam bilan). */
export function formatBand(b: number | null | undefined): string {
  return typeof b === 'number' && Number.isFinite(b) ? b.toFixed(1) : '—';
}

/** Band nomi (IELTS shkalasi): yarim band pastga — 6.5 → "6" ning nomi. */
export function bandKey(b: number): number {
  return Math.max(0, Math.min(9, Math.floor(b)));
}

export function formatClock(totalSeconds: number): string {
  const s = Math.max(0, Math.ceil(totalSeconds));
  return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
}

/** Serverdagi IeltsBand.CountWords bilan bir xil qoida. */
export function countWords(text: string): number {
  return (text.match(/[\p{L}\p{N}]+(?:['’-][\p{L}\p{N}]+)*/gu) ?? []).length;
}

// ---- Writing qoralamasi (shu qurilmada) ----

export interface WritingDraft {
  setId: string;
  variant: 'academic' | 'general';
  startedAt: number;
  task1: string;
  task2: string;
}

export const DRAFT_KEY = 'speakingCoach.mockWriting';

/** Qoralama hali davom ettirishga yaroqlimi (vaqt + 5 daqiqa zaxira ichida). */
export function draftUsable(d: WritingDraft | null, variant: string, minutes: number, now: number): boolean {
  return !!d && d.variant === variant && now < d.startedAt + (minutes + 5) * 60_000;
}

export function readDraft(): WritingDraft | null {
  try {
    const raw = localStorage.getItem(DRAFT_KEY);
    if (!raw) return null;
    const d = JSON.parse(raw) as WritingDraft;
    return typeof d?.setId === 'string' && typeof d.startedAt === 'number' ? d : null;
  } catch {
    return null;
  }
}

export function writeDraft(d: WritingDraft | null) {
  try {
    if (d) localStorage.setItem(DRAFT_KEY, JSON.stringify(d));
    else localStorage.removeItem(DRAFT_KEY);
  } catch {
    // Maxfiy rejim yoki to'lgan xotira — qoralamasiz ham ishlayveradi.
  }
}
