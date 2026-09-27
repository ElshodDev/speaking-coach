// Shadowing: sof mantiq (brauzer API'larisiz) — Vitest bilan tekshiriladi.

export type ShadowKind = 'youtube' | 'character';
export type CharacterName = 'cat' | 'owl' | 'robot' | 'fox' | 'bear' | 'penguin';

export interface ShadowLine {
  text: string;
  start: number | null;
  end: number | null;
  speaker: number;
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
}

export interface ShadowCheck {
  score: number;
  heard: string;
  words: { word: string; ok: boolean }[];
  tip: string;
}

export const LEVELS = ['A1', 'A2', 'B1', 'B2', 'C1'] as const;
export const SPEEDS = [0.75, 1, 1.25] as const;

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
  return { from: Math.max(lo, start - 0.25), to: end + 0.35 };
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
