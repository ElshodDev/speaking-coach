// Bir bosishli baholar: kalitlar backend'dagi RatingRules bilan BIR XIL (qat'iy ro'yxat, erkin matn yo'q).
//
// Qoidalar:
// - Feature flag: VITE_FEEDBACK_PROMPTS=off — hamma so'rovlar o'chadi (qo'lda "Fikr bildirish" qoladi).
// - Har so'rov bir qurilmada bir marta (localStorage), ovoz va shaxsiy ma'lumot yuborilmaydi.
// - Xato bo'lsa ham ilova buzilmaydi: yuborib bo'lmasa, jimgina o'tkazib yuboriladi.
import { apiEvent, routeToPath } from './analytics';

export type AiSkill = 'speaking' | 'writing' | 'part2' | 'mock';
export type AiVerdict = 'fair' | 'high' | 'low';
export const AI_SKILLS: AiSkill[] = ['speaking', 'writing', 'part2', 'mock'];
export const AI_VERDICTS: AiVerdict[] = ['fair', 'high', 'low'];

export type SourceId = 'instagram' | 'telegram' | 'friend' | 'teacher' | 'search' | 'other';
export const SOURCES: SourceId[] = ['instagram', 'telegram', 'friend', 'teacher', 'search', 'other'];

const RATING_PATTERN =
  /^(first_practice:(up|down)|ai_fair:(speaking|writing|part2|mock):(fair|high|low)|lesson:(up|down)|source:(instagram|telegram|friend|teacher|search|other))$/;

export const isRatingKey = (key: string): boolean => RATING_PATTERN.test(key);
export const firstPracticeKey = (up: boolean) => `first_practice:${up ? 'up' : 'down'}`;
export const aiKey = (skill: AiSkill, verdict: AiVerdict) => `ai_fair:${skill}:${verdict}`;
export const lessonKey = (up: boolean) => `lesson:${up ? 'up' : 'down'}`;
export const sourceKey = (source: SourceId) => `source:${source}`;

/** Flag: env'da VITE_FEEDBACK_PROMPTS=off bo'lsa — hamma so'rovlar o'chiq. */
export function promptsEnabled(): boolean {
  return (import.meta.env.VITE_FEEDBACK_PROMPTS as string | undefined)?.trim().toLowerCase() !== 'off';
}

// ---- "Bir qurilmada bir marta" belgisi ----

const FLAG_PREFIX = 'sc.rate.';
export type AskName = 'first' | 'source';

export function wasAsked(name: AskName): boolean {
  try {
    return localStorage.getItem(FLAG_PREFIX + name) === '1';
  } catch {
    return false;
  }
}

export function markAsked(name: AskName) {
  try {
    localStorage.setItem(FLAG_PREFIX + name, '1');
  } catch {
    // localStorage yo'q — so'rov qayta chiqishi mumkin; muhim emas.
  }
}

// ---- Birinchi mashqdan keyin so'rash ----

export const RATE_FIRST_EVENT = 'sc:rate-first';

/** Birinchi mashq tugaganini anglatadigan so'rovmi (analitikadagi bilan bir xil yo'llar)? */
export function isFirstPracticeRequest(method: string, path: string): boolean {
  return apiEvent(method, path)?.name === 'first_practice_completed';
}

/** apiJson muvaffaqiyatidan keyin chaqiriladi: kerak bo'lsa, so'rovni ochish hodisasini yuboradi. Hech qachon xato tashlamaydi. */
export function noteRatingTrigger(method: string, path: string) {
  try {
    if (typeof window === 'undefined' || !promptsEnabled() || wasAsked('first')) return;
    if (isFirstPracticeRequest(method, path)) window.dispatchEvent(new Event(RATE_FIRST_EVENT));
  } catch {
    // e'tiborsiz
  }
}

// ---- Yuborish ----

/** Sahifa: faqat bo'lim nomi ("#/practice"), identifikatorlarsiz. */
export function ratingPage(hash: string): string {
  return '#/' + routeToPath(hash.replace(/^#\/?/, '') || 'home');
}

/** Bahoni yuboradi; true — yuborildi. Noto'g'ri kalit yuborilmaydi. Hech qachon xato tashlamaydi. */
export async function sendRating(key: string, post: (path: string, body: unknown) => Promise<unknown>): Promise<boolean> {
  if (!isRatingKey(key)) return false;
  try {
    const hash = typeof window === 'undefined' ? '' : window.location.hash;
    await post('/api/rating', { key, page: ratingPage(hash) });
    return true;
  } catch {
    return false;
  }
}

// ---- Admin xulosasi ----

export interface RatingItem {
  key: string;
  count: number;
}

interface AiCounts {
  fair: number;
  high: number;
  low: number;
  total: number;
}

export interface RatingsSummary {
  firstPractice: { up: number; down: number; total: number };
  ai: Record<AiSkill, AiCounts>;
  aiAll: AiCounts;
  source: Record<SourceId, number>;
  sourceTotal: number;
}

const emptyAi = (): AiCounts => ({ fair: 0, high: 0, low: 0, total: 0 });

/** Server qaytargan (kalit → soni) ro'yxatdan xulosa. Noma'lum kalitlar e'tiborsiz qoldiriladi. */
export function ratingsSummary(items: RatingItem[]): RatingsSummary {
  const out: RatingsSummary = {
    firstPractice: { up: 0, down: 0, total: 0 },
    ai: { speaking: emptyAi(), writing: emptyAi(), part2: emptyAi(), mock: emptyAi() },
    aiAll: emptyAi(),
    source: { instagram: 0, telegram: 0, friend: 0, teacher: 0, search: 0, other: 0 },
    sourceTotal: 0,
  };
  for (const { key, count } of items) {
    if (!isRatingKey(key) || !Number.isFinite(count) || count < 0) continue;
    const parts = key.split(':');
    if (parts[0] === 'first_practice') {
      if (parts[1] === 'up') out.firstPractice.up += count;
      else out.firstPractice.down += count;
      out.firstPractice.total += count;
    } else if (parts[0] === 'ai_fair') {
      const skill = parts[1] as AiSkill;
      const verdict = parts[2] as AiVerdict;
      out.ai[skill][verdict] += count;
      out.ai[skill].total += count;
      out.aiAll[verdict] += count;
      out.aiAll.total += count;
    } else if (parts[0] === 'source') {
      out.source[parts[1] as SourceId] += count;
      out.sourceTotal += count;
    }
  }
  return out;
}

/** Foiz (butun son); total = 0 bo'lsa 0. */
export const percent = (n: number, total: number): number => (total > 0 ? Math.round((n / total) * 100) : 0);
