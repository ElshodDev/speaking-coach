// Shadowing: sof mantiq (brauzer API'larisiz) — Vitest bilan tekshiriladi.

export type ShadowKind = 'youtube' | 'character';
export type CharacterName = 'cat' | 'owl' | 'robot' | 'fox' | 'bear' | 'penguin';

export interface ShadowLine {
  text: string;
  start: number | null;
  end: number | null;
  speaker: number;
  /** Tarjimalar: { uz, ru } (ixtiyoriy). */
  tr?: Partial<Record<'uz' | 'ru', string>> | null;
  /** O'rganishga arziydigan so'zlar (rang bilan ajratiladi). */
  keys?: string[] | null;
}

export interface ShadowLesson {
  id: string;
  kind: ShadowKind;
  title: string;
  level: string;
  videoId: string | null;
  from: number | null;
  to: number | null;
  characters: CharacterName[];
  lines: ShadowLine[];
}

export interface ShadowSummary {
  id: string;
  kind: ShadowKind;
  title: string;
  level: string;
  videoId: string | null;
  characters: CharacterName[];
  lines: number;
  seconds: number;
  /** Seriyadagi raqami (#1, #2...). */
  episode: number;
}

export interface ShadowProgress {
  lessonId: string;
  times: number;
  best: number | null;
}

export interface ShadowCheck {
  score: number;
  heard: string;
  words: { word: string; ok: boolean }[];
  tip: string;
}

export const LEVELS = ['A1', 'A2', 'B1', 'B2', 'C1'] as const;
export const SPEEDS = [0.5, 0.75, 1, 1.25, 1.5] as const;

/** Mashq rejimi: oddiy (o'zingiz boshqarasiz), avto (pauza bilan ketma-ket), qo'l tegizmasdan (o'ynaydi → yozadi → eshittiradi). */
export type ShadowMode = 'normal' | 'auto' | 'free';

/** Takror: o'chiq → 3 marta → cheksiz → o'chiq. */
export type LoopMode = 0 | 3 | 'inf';
export const nextLoop = (m: LoopMode): LoopMode => (m === 0 ? 3 : m === 3 ? 'inf' : 0);
/** Gap necha marta o'ynaladi (takror hisobga olinib). */
export const playsFor = (m: LoopMode): number => (m === 0 ? 1 : m === 3 ? 3 : Infinity);

/** Qahramon ovozi: erkak/ayol ovoz, balandlik va tezlik (multfilmdek farq qilsin). */
export const CHARACTER_VOICE: Record<CharacterName, { voice: 'male' | 'female'; pitch: number; rate: number }> = {
  cat: { voice: 'female', pitch: 1.35, rate: 1 },
  owl: { voice: 'male', pitch: 0.95, rate: 0.92 },
  robot: { voice: 'male', pitch: 0.55, rate: 0.9 },
  fox: { voice: 'female', pitch: 1.1, rate: 1.02 },
  bear: { voice: 'male', pitch: 0.7, rate: 0.9 },
  penguin: { voice: 'female', pitch: 1.5, rate: 1.05 },
};

export const words = (text: string) => text.split(/\s+/).filter(Boolean).length;

/** Gap davomiyligi (soniya): video — vaqtlaridan; qahramon — ~2.6 so'z/s. */
export function lineSeconds(line: ShadowLine): number {
  if (line.start != null && line.end != null) return Math.max(0.5, line.end - line.start);
  return Math.max(1.2, words(line.text) / 2.6);
}

/** Videodan o'ynatiladigan qism: birinchi so'z kesilib qolmasin — biroz oldin va keyin. */
export function segment(lesson: ShadowLesson, i: number): { from: number; to: number } {
  const line = lesson.lines[i];
  const start = line.start ?? 0;
  const end = line.end ?? start + lineSeconds(line);
  const lo = lesson.from ?? 0;
  // Oxirgi gap video (yoki tanlangan qism) oxiridan o'tib ketmasin — aks holda pleyer tugashni kutib qoladi.
  const hi = lesson.to ?? Infinity;
  return { from: Math.max(lo, start - 0.25), to: Math.max(end, Math.min(end + 0.35, hi)) };
}

/**
 * Takrorlash uchun pauza (ms): gap uzunligi × 1.3 + 1 s, sekin tezlikda
 * biroz uzoqroq; 2.5–15 s oralig'ida.
 */
export function repeatPauseMs(line: ShadowLine, rate: number): number {
  const s = lineSeconds(line) * 1.3 / Math.min(1, rate) + 1;
  return Math.round(Math.min(15, Math.max(2.5, s)) * 1000);
}

/** Yozishni avtomatik to'xtatish (ms): gapdan ikki baravar uzun, 4–25 s. */
export function recordLimitMs(line: ShadowLine): number {
  return Math.round(Math.min(25, Math.max(4, lineSeconds(line) * 2.2 + 2)) * 1000);
}

export function clock(seconds: number): string {
  const s = Math.max(0, Math.floor(seconds));
  return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`;
}

export function thumbnail(videoId: string): string {
  return `https://i.ytimg.com/vi/${videoId}/mqdefault.jpg`;
}

export interface ShadowFilter {
  level: string | null;
  kind: ShadowKind | null;
}

export function filterLessons(list: ShadowSummary[], f: ShadowFilter): ShadowSummary[] {
  return list.filter((l) => (!f.level || l.level === f.level) && (!f.kind || l.kind === f.kind));
}

/** Ro'yxatdagi darajalar (mavjudlari, tartib bilan). */
export function levelsIn(list: ShadowSummary[]): string[] {
  return LEVELS.filter((lv) => list.some((l) => l.level === lv));
}

/** Ball → rang darajasi (ui.levelOf bilan bir xil chegaralar). */
export function scoreKey(score: number): 'great' | 'good' | 'mid' | 'low' {
  return score >= 85 ? 'great' : score >= 70 ? 'good' : score >= 50 ? 'mid' : 'low';
}

export function average(scores: number[]): number | null {
  return scores.length === 0 ? null : Math.round(scores.reduce((a, b) => a + b, 0) / scores.length);
}

/** "Tugatdim" — kamida yarmi (va kamida 3 ta) gap takrorlanganda. */
export function canFinish(practiced: number, total: number): boolean {
  return practiced >= Math.min(total, Math.max(3, Math.ceil(total / 2)));
}

/** Yozuv fayli kengaytmasi (Safari — mp4, Firefox — ogg, Chrome — webm). */
export function audioExt(type: string): string {
  return type.includes('mp4') ? 'mp4' : type.includes('ogg') ? 'ogg' : 'webm';
}

/** Keyingi gap (oxirida — null). */
export const nextIndex = (i: number, total: number) => (i + 1 < total ? i + 1 : null);

/** Qo'l tegizmasdan rejim: takrorlash uchun yozish vaqti (ms), 2.5–20 s. */
export function handsFreeRecordMs(line: ShadowLine, rate: number): number {
  const s = (lineSeconds(line) / Math.min(1, rate)) * 1.5 + 1.2;
  return Math.round(Math.min(20, Math.max(2.5, s)) * 1000);
}

// ---------------- Karaoke va so'zlar ----------------

export interface Token {
  text: string;
  /** So'z (harf yoki raqami bor) — bosiladi va karaokeda yonadi. */
  word: boolean;
}

/** Matnni so'z va bo'shliqlarga ajratadi (tinish belgilari so'zga yopishgan holda qoladi). */
export function tokenize(text: string): Token[] {
  return text
    .split(/(\s+)/)
    .filter((t) => t.length > 0)
    .map((t) => ({ text: t, word: /[A-Za-z0-9]/.test(t) }));
}

/** So'z (tinish belgilarisiz, kichik harf) — kalit so'z bilan solishtirish uchun. */
export const bareWord = (t: string) => t.toLowerCase().replace(/’/g, "'").replace(/^[^a-z0-9']+|[^a-z0-9']+$/g, '');

export function isKeyWord(token: string, keys: string[] | null | undefined): boolean {
  if (!keys || keys.length === 0) return false;
  const w = bareWord(token);
  return w.length > 0 && keys.some((k) => bareWord(k) === w);
}

/**
 * Karaoke: gapning qaysi qismi aytildi (0–1) → nechanchi so'z yonadi.
 * So'zlar uzunligiga qarab (uzun so'z ko'proq vaqt oladi). -1 — hali boshlanmagan.
 */
export function activeWord(text: string, fraction: number | null): number {
  if (fraction == null || fraction <= 0) return -1;
  const words = tokenize(text).filter((t) => t.word);
  if (words.length === 0) return -1;
  const total = words.reduce((a, w) => a + w.text.length + 1, 0);
  let acc = 0;
  for (let i = 0; i < words.length; i++) {
    acc += words[i].text.length + 1;
    if (fraction * total < acc) return i;
  }
  return words.length - 1;
}

/** Brauzer "boundary" hodisasi: belgi o'rnidan ulush (0–1). */
export const fractionAt = (text: string, charIndex: number) => (text.length === 0 ? 0 : Math.min(1, Math.max(0, (charIndex + 1) / text.length)));

/** Video gapi: hozirgi vaqtdan ulush (0–1); gap vaqti yo'q bo'lsa — null. */
export function videoFraction(line: ShadowLine, now: number): number | null {
  if (line.start == null || line.end == null || line.end <= line.start) return null;
  return Math.min(1, Math.max(0, (now - line.start) / (line.end - line.start)));
}

/** Qatorning tarjimasi interfeys tilida (ingliz tilida — yo'q). */
export function translationOf(line: ShadowLine, lang: string): string | null {
  if (lang !== 'uz' && lang !== 'ru') return null;
  return line.tr?.[lang]?.trim() || null;
}

export const hasTranslations = (lesson: ShadowLesson, lang: string) => lesson.lines.some((l) => translationOf(l, lang) !== null);

// ---------------- Seriyalar va natijalar ----------------

/** Ro'yxat seriyalar bo'yicha: video, keyin qahramonlar; ichida — raqam tartibida. */
export function bySeries(list: ShadowSummary[]): { kind: ShadowKind; lessons: ShadowSummary[] }[] {
  return (['youtube', 'character'] as const)
    .map((kind) => ({ kind, lessons: list.filter((l) => l.kind === kind).sort((a, b) => a.episode - b.episode) }))
    .filter((g) => g.lessons.length > 0);
}

export function progressMap(p: ShadowProgress[]): Record<string, ShadowProgress> {
  return Object.fromEntries(p.map((x) => [x.lessonId, x]));
}

/**
 * Keyingi dars: shu seriyadagi keyingisi (hali qilinmagani afzal), bo'lmasa —
 * istalgan seriyadagi birinchi qilinmagan dars; hammasi qilingan — seriyadagi keyingisi.
 */
export function nextLesson(list: ShadowSummary[], currentId: string, done: Record<string, unknown>): ShadowSummary | null {
  const cur = list.find((l) => l.id === currentId);
  if (!cur) return null;
  const series = list.filter((l) => l.kind === cur.kind).sort((a, b) => a.episode - b.episode);
  const after = series.filter((l) => l.episode > cur.episode);
  return (
    after.find((l) => !done[l.id]) ??
    bySeries(list).flatMap((g) => g.lessons).find((l) => l.id !== currentId && !done[l.id]) ??
    after[0] ??
    null
  );
}
