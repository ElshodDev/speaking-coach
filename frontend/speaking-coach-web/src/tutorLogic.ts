// AI ustoz: toza yordamchilar (komponentdan alohida — testlanadi).

export interface ExerciseView {
  prompt: string;
  options: string[];
  picked: number | null;
  correct: boolean | null;
  correctIndex: number | null;
  explanation: string | null;
}

export interface Correction {
  original: string;
  corrected: string;
  explanation: string;
}

export interface WritingView {
  feedback: string;
  corrections: Correction[];
  improved: string;
}

export interface LessonView {
  topic: string;
  level: string;
  explain: { title: string; body: string }[];
  practice: ExerciseView[];
  test: ExerciseView[] | null;
  writingTask: string | null;
  writing: WritingView | null;
  practiceDone: boolean;
  testDone: boolean;
  asksLeft: number;
}

export type Step = 'explain' | 'practice' | 'write' | 'test';

/** Dars qayta ochilganda qaysi bosqichdan davom etish (server holatidan). */
export function stepOf(v: LessonView): Step {
  if (v.testDone) return 'test';
  if (v.test?.some((q) => q.picked !== null)) return 'test';
  if (v.practiceDone) return v.writing ? 'test' : 'write';
  if (v.practice.some((q) => q.picked !== null)) return 'practice';
  return 'explain';
}

/** Birinchi javobsiz savol (hammasi javobli bo'lsa — oxirgisi). */
export function firstOpen(list: ExerciseView[]): number {
  const i = list.findIndex((q) => q.picked === null);
  return i === -1 ? Math.max(0, list.length - 1) : i;
}

export interface AnswerReply {
  correct: boolean;
  correctIndex: number;
  explanation: string;
  practiceDone: boolean;
  testDone: boolean;
}

/** Server javobini ro'yxatga qo'llaydi (yangi ro'yxat qaytaradi). */
export function applyAnswer(list: ExerciseView[], index: number, option: number, r: AnswerReply): ExerciseView[] {
  return list.map((q, i) => (i === index ? { ...q, picked: option, correct: r.correct, correctIndex: r.correctIndex, explanation: r.explanation } : q));
}

/** Mashq/test savollari soni: javob berilgani. */
export const answered = (list: ExerciseView[] | null): number => (list ?? []).filter((q) => q.picked !== null).length;

const KEY = 'sc.tutor.lesson';

/** Oxirgi dars identifikatori (brauzerda): sahifa yangilansa yoki qaytilsa — davom ettirish. */
export function savedLesson(): string | null {
  try {
    const v = localStorage.getItem(KEY);
    return v && /^[0-9a-f-]{36}$/i.test(v) ? v : null;
  } catch {
    return null;
  }
}

export function saveLesson(id: string | null) {
  try {
    if (id) localStorage.setItem(KEY, id);
    else localStorage.removeItem(KEY);
  } catch {
    // localStorage yo'q — davom ettirish ishlamaydi, muhim emas.
  }
}
