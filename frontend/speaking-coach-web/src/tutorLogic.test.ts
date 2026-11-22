import { beforeEach, describe, expect, it } from 'vitest';
import { tutorMsg } from './locales/tutor';
import {
  answered,
  applyAnswer,
  firstOpen,
  savedLesson,
  saveLesson,
  stepOf,
  type ExerciseView,
  type LessonView,
} from './tutorLogic';

const mem = new Map<string, string>();
(globalThis as { localStorage?: unknown }).localStorage = {
  getItem: (k: string) => mem.get(k) ?? null,
  setItem: (k: string, v: string) => void mem.set(k, v),
  removeItem: (k: string) => void mem.delete(k),
};
beforeEach(() => mem.clear());

const ex = (picked: number | null = null): ExerciseView => ({
  prompt: 'She ___ swim.',
  options: ['can', 'cans', 'canning'],
  picked,
  correct: picked === null ? null : picked === 0,
  correctIndex: picked === null ? null : 0,
  explanation: picked === null ? null : 'why',
});

const view = (over: Partial<LessonView> = {}): LessonView => ({
  topic: 'Modal verbs',
  level: 'B1',
  explain: [{ title: 'a', body: 'b' }],
  practice: [ex(), ex()],
  test: null,
  writingTask: null,
  writing: null,
  practiceDone: false,
  testDone: false,
  asksLeft: 5,
  ...over,
});

describe('stepOf: dars qayta ochilganda qaysi bosqichdan davom etiladi', () => {
  it('yangi dars — tushuntirishdan', () => expect(stepOf(view())).toBe('explain'));
  it('mashq boshlangan — mashqdan', () => expect(stepOf(view({ practice: [ex(0), ex()] }))).toBe('practice'));
  it('mashqlar tugagan, yozma hali yo‘q — yozmadan', () =>
    expect(stepOf(view({ practice: [ex(0), ex(0)], practiceDone: true, test: [ex(), ex()], writingTask: 'x' }))).toBe('write'));
  it('yozma bajarilgan — mini-testdan', () =>
    expect(stepOf(view({ practiceDone: true, test: [ex(), ex()], writing: { feedback: 'ok', corrections: [], improved: '' } }))).toBe('test'));
  it('mini-test boshlangan — mini-testdan (yozmani o‘tkazib yuborgan bo‘lsa ham)', () =>
    expect(stepOf(view({ practiceDone: true, test: [ex(1), ex()] }))).toBe('test'));
  it('mini-test tugagan — mini-test (natija tugmasi)', () =>
    expect(stepOf(view({ practiceDone: true, testDone: true, test: [ex(0)] }))).toBe('test'));
});

describe('savollar ro‘yxati', () => {
  it('firstOpen: birinchi javobsiz savol, hammasi javobli bo‘lsa — oxirgisi', () => {
    expect(firstOpen([ex(0), ex(1), ex()])).toBe(2);
    expect(firstOpen([ex(), ex()])).toBe(0);
    expect(firstOpen([ex(0), ex(1)])).toBe(1);
    expect(firstOpen([])).toBe(0);
  });

  it('applyAnswer faqat bitta savolni o‘zgartiradi va asl ro‘yxatni buzmaydi', () => {
    const list = [ex(), ex()];
    const next = applyAnswer(list, 1, 2, { correct: false, correctIndex: 0, explanation: 'why', practiceDone: false, testDone: false });
    expect(next[1]).toMatchObject({ picked: 2, correct: false, correctIndex: 0, explanation: 'why' });
    expect(next[0].picked).toBeNull();
    expect(list[1].picked).toBeNull();
  });

  it('answered: javob berilganlar soni', () => {
    expect(answered([ex(0), ex(), ex(1)])).toBe(2);
    expect(answered(null)).toBe(0);
  });
});

describe('oxirgi dars (davom ettirish)', () => {
  it('yaroqli identifikator saqlanadi va o‘chiriladi', () => {
    const id = '0b7e6b0e-4c4b-4a53-9a43-1b2f3c4d5e6f';
    expect(savedLesson()).toBeNull();
    saveLesson(id);
    expect(savedLesson()).toBe(id);
    saveLesson(null);
    expect(savedLesson()).toBeNull();
  });

  it('noto‘g‘ri qiymat e‘tiborsiz qoldiriladi', () => {
    mem.set('sc.tutor.lesson', '../../etc/passwd');
    expect(savedLesson()).toBeNull();
  });
});

describe('tarjimalar', () => {
  it('uz/ru/en bir xil kalitlarga ega va bo‘sh emas', () => {
    const keys = (o: object) => Object.keys(o).sort();
    expect(keys(tutorMsg.ru)).toEqual(keys(tutorMsg.uz));
    expect(keys(tutorMsg.en)).toEqual(keys(tutorMsg.uz));
    for (const l of ['uz', 'ru', 'en'] as const) {
      expect(Object.keys(tutorMsg[l].mastery).sort()).toEqual(['ok', 'strong', 'weak']);
      expect(tutorMsg[l].resume('X')).toContain('X');
      expect(tutorMsg[l].practiceStep(1, 6)).toContain('1');
    }
  });
});
