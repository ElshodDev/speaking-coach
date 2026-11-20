// IELTS Speaking Part 2 trener — toza funksiyalar (part2Logic.test.ts tekshiradi).
import type { CorrectionItem, ScoreWithReasoning } from './ui';
import type { FluencyStats } from './fluencyLogic';

export interface Part2Card {
  id: string;
  topic: string;
  points: string[];
  explain: string;
  done?: boolean;
}

export interface CoverageItem {
  point: string;
  status: 'covered' | 'partly' | 'missed';
  evidence: string;
}

export interface Part2Evaluation {
  transcript: string;
  fluency: ScoreWithReasoning;
  grammar: ScoreWithReasoning;
  vocabulary: ScoreWithReasoning;
  topCorrections: CorrectionItem[];
  encouragement: string;
  nextFocus: string;
  coverage: CoverageItem[];
  estimatedBand: number;
}

export interface Part2Result {
  submissionId: string;
  card: Part2Card;
  evaluation: Part2Evaluation;
  metrics?: FluencyStats;
  saved: boolean;
  newCards: number;
}

export type Phase = 'pick' | 'prep' | 'speak' | 'uploading' | 'result' | 'error';

/** Imtihonchi kamida shuncha gapirishni kutadi; 1 daqiqadan kam — sezilarli kamchilik. */
export const MIN_GOOD_SECONDS = 100;
export const MIN_OK_SECONDS = 60;

/** Gapirish vaqti bo'yicha xulosa: qisqa / o'rtacha / yaxshi. */
export function timeVerdict(seconds: number): 'short' | 'fair' | 'good' {
  if (seconds < MIN_OK_SECONDS) return 'short';
  return seconds < MIN_GOOD_SECONDS ? 'fair' : 'good';
}

/** "1:05" ko'rinishi. */
export function clock(seconds: number): string {
  const s = Math.max(0, Math.floor(seconds));
  return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
}

/** Bandlar qamrovi foizda: covered — 1, partly — 0.5 (server bilan bir xil qoida). */
export function coveragePercent(items: CoverageItem[]): number {
  if (items.length === 0) return 0;
  const sum = items.reduce((a, c) => a + (c.status === 'covered' ? 1 : c.status === 'partly' ? 0.5 : 0), 0);
  return Math.round((100 * sum) / items.length);
}

/** Keyingi kartochka: hali bajarilmaganlardan (bo'lmasa — hammasidan), joriysidan boshqa, tasodifiy. */
export function nextCard(cards: Part2Card[], currentId: string | null, random = Math.random): Part2Card | null {
  if (cards.length === 0) return null;
  const others = cards.filter((c) => c.id !== currentId);
  const fresh = others.filter((c) => !c.done);
  const pool = fresh.length ? fresh : others.length ? others : cards;
  return pool[Math.floor(random() * pool.length)] ?? null;
}

/** Gapirish paytidagi maslahat: 1 daqiqagacha — davom eting; 1:40 gacha — yana bir fikr; keyin — yakunlashga tayyorlaning. */
export function speakingHint(elapsed: number, total: number): 'keepGoing' | 'addIdea' | 'wrapUp' {
  if (elapsed < MIN_OK_SECONDS) return 'keepGoing';
  return total - elapsed > 20 ? 'addIdea' : 'wrapUp';
}
