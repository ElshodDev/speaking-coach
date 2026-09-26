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

export type SpeakingStep =
  | { kind: 'answer'; index: number; part: 1 | 2 | 3; text: string; maxSeconds: number; numberInPart: number; ofInPart: number }
  | { kind: 'prep'; index: number; seconds: number };

/**
 * Imtihon qadamlari tartibda: Part 1 savollari → Part 2 tayyorgarlik →
 * Part 2 nutq → Part 3 savollari. `index` — serverdagi savol raqami
 * (javob fayli shu raqam bilan yuboriladi: a0, a1 …).
 */
export function speakingSteps(set: SpeakingSet, t: SpeakingTiming): SpeakingStep[] {
  const steps: SpeakingStep[] = [];
  set.part1.forEach((text, i) =>
    steps.push({ kind: 'answer', index: i, part: 1, text, maxSeconds: t.part1AnswerSeconds, numberInPart: i + 1, ofInPart: set.part1.length }),
  );
  const p2 = set.part1.length;
  steps.push({ kind: 'prep', index: p2, seconds: t.part2PrepSeconds });
  steps.push({ kind: 'answer', index: p2, part: 2, text: set.part2.topic, maxSeconds: t.part2SpeakSeconds, numberInPart: 1, ofInPart: 1 });
  set.part3.forEach((text, i) =>
    steps.push({ kind: 'answer', index: p2 + 1 + i, part: 3, text, maxSeconds: t.part3AnswerSeconds, numberInPart: i + 1, ofInPart: set.part3.length }),
  );
  return steps;
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
