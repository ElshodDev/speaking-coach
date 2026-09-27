// Ilova bilan keladigan tayyor materiallar (Gemini'siz): grammatika darslari,
// mavzuli lug'at va Speaking/Writing mavzulari. Tushuntirishlar uch tilda.

export type Level = 'A2' | 'B1' | 'B2' | 'C1';
export const LEVELS: Level[] = ['A2', 'B1', 'B2', 'C1'];

/** Uch tildagi matn: interfeys tiliga qarab tanlanadi. */
export interface L3 {
  uz: string;
  ru: string;
  en: string;
}

export interface GrammarQuestion {
  /** Inglizcha gap yoki savol; bo'sh joy — "___". */
  prompt: string;
  /** 3–4 variant (inglizcha). */
  options: string[];
  /** To'g'ri variant indeksi. */
  answer: number;
  /** Nega shu javob to'g'ri — uch tilda, bitta qisqa gap. */
  why: L3;
}

export interface GrammarLesson {
  id: string;
  level: Level;
  title: L3;
  /** Bir-ikki gapda: bu mavzu nima va qachon kerak. */
  summary: L3;
  /** Qoidalar: har birida tushuntirish (uch tilda) va 2–3 ta inglizcha misol. */
  rules: { text: L3; examples: string[] }[];
  /** O'zbek/rus tilida so'zlashuvchilarning tipik xatolari. */
  mistakes: { wrong: string; right: string; note: L3 }[];
  /** 6 ta savol. */
  quiz: GrammarQuestion[];
}

export interface TopicWord {
  word: string;
  /** noun, verb, adjective, adverb, phrase... */
  pos: string;
  uz: string;
  ru: string;
  /** Inglizcha oddiy ta'rif. */
  definition: string;
  /** Inglizcha misol gap. */
  example: string;
  level: Level;
}

export interface VocabTopic {
  id: string;
  emoji: string;
  title: L3;
  words: TopicWord[];
}

export type SpeakingCategory = 'everyday' | 'ielts-part1' | 'ielts-part2' | 'ielts-part3' | 'cefr';
export type WritingCategory = 'paragraph' | 'essay-opinion' | 'essay-discussion' | 'letter-informal' | 'letter-formal';

export interface SpeakingTopic {
  id: string;
  level: Level;
  category: SpeakingCategory;
  /** Inglizcha savol yoki cue card mavzusi (≤ 300 belgi — server cheklovi). */
  text: string;
  /** IELTS Part 2 uchun: "You should say" punktlari. */
  points?: string[];
}

export interface WritingTopic {
  id: string;
  level: Level;
  category: WritingCategory;
  /** Inglizcha topshiriq (≤ 300 belgi). */
  text: string;
  /** Tavsiya etilgan so'zlar soni. */
  words: [number, number];
}
