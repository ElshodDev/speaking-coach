import { describe, expect, it } from 'vitest';
import { ALL_GRAMMAR as GRAMMAR, ALL_VOCAB_TOPICS as VOCAB_TOPICS } from './index';
import { SPEAKING_TOPICS_BANK, WRITING_TOPICS_BANK } from './topics';
import { LEVELS, type L3 } from './types';

const filled = (t: L3) => t.uz.trim().length > 0 && t.ru.trim().length > 0 && t.en.trim().length > 0;
const unique = <T,>(xs: T[]) => new Set(xs).size === xs.length;

describe('grammar lessons', () => {
  it('cover every level with complete, trilingual lessons', () => {
    for (const lv of LEVELS) expect(GRAMMAR.filter((g) => g.level === lv).length, lv).toBeGreaterThanOrEqual(8);
    expect(unique(GRAMMAR.map((g) => g.id))).toBe(true);
    for (const g of GRAMMAR) {
      expect(filled(g.title) && filled(g.summary), g.id).toBe(true);
      expect(g.rules.length, g.id).toBeGreaterThanOrEqual(2);
      for (const r of g.rules) {
        expect(filled(r.text), g.id).toBe(true);
        expect(r.examples.length, g.id).toBeGreaterThanOrEqual(2);
      }
      expect(g.mistakes.length, g.id).toBeGreaterThanOrEqual(2);
      for (const m of g.mistakes) expect(m.wrong !== m.right && filled(m.note), g.id).toBe(true);
      expect(g.quiz.length, g.id).toBe(6);
      for (const q of g.quiz) {
        expect(q.options.length >= 3 && q.options.length <= 4, `${g.id}: ${q.prompt}`).toBe(true);
        expect(unique(q.options), `${g.id}: ${q.prompt}`).toBe(true);
        expect(q.answer >= 0 && q.answer < q.options.length, `${g.id}: ${q.prompt}`).toBe(true);
        expect(filled(q.why), `${g.id}: ${q.prompt}`).toBe(true);
      }
      // To'g'ri javob har doim bir joyda turmasin.
      expect(new Set(g.quiz.map((q) => q.answer)).size, g.id).toBeGreaterThanOrEqual(2);
    }
  });
});

describe('topic vocabulary', () => {
  it('has complete words with translations', () => {
    expect(VOCAB_TOPICS.length).toBeGreaterThanOrEqual(20);
    expect(unique(VOCAB_TOPICS.map((t) => t.id))).toBe(true);
    for (const t of VOCAB_TOPICS) {
      expect(filled(t.title), t.id).toBe(true);
      expect(t.words.length, t.id).toBeGreaterThanOrEqual(12);
      expect(unique(t.words.map((w) => w.word.toLowerCase())), t.id).toBe(true);
      for (const w of t.words) {
        for (const k of ['word', 'pos', 'uz', 'ru', 'definition', 'example'] as const) expect(w[k].trim().length, `${t.id}/${w.word}.${k}`).toBeGreaterThan(0);
        expect(LEVELS.includes(w.level), `${t.id}/${w.word}`).toBe(true);
        expect(w.example.toLowerCase().includes(w.word.toLowerCase().split(' ')[0]), `${t.id}/${w.word}: misolda soʻz yoʻq`).toBe(true);
      }
    }
  });
});

describe('speaking and writing topics', () => {
  it('cover every level and category, within the server limit', () => {
    expect(SPEAKING_TOPICS_BANK.length).toBeGreaterThanOrEqual(40);
    expect(WRITING_TOPICS_BANK.length).toBeGreaterThanOrEqual(24);
    for (const lv of LEVELS) {
      expect(SPEAKING_TOPICS_BANK.filter((t) => t.level === lv).length, `speaking ${lv}`).toBeGreaterThanOrEqual(8);
      expect(WRITING_TOPICS_BANK.filter((t) => t.level === lv).length, `writing ${lv}`).toBeGreaterThanOrEqual(5);
    }
    expect(unique([...SPEAKING_TOPICS_BANK, ...WRITING_TOPICS_BANK].map((t) => t.id))).toBe(true);
    for (const t of [...SPEAKING_TOPICS_BANK, ...WRITING_TOPICS_BANK]) expect(t.text.length > 10 && t.text.length <= 300, t.id).toBe(true);
    for (const t of SPEAKING_TOPICS_BANK.filter((x) => x.category === 'ielts-part2')) expect((t.points ?? []).length, t.id).toBeGreaterThanOrEqual(3);
    for (const t of WRITING_TOPICS_BANK) expect(t.words[0] < t.words[1], t.id).toBe(true);
    for (const c of ['everyday', 'ielts-part1', 'ielts-part2', 'ielts-part3', 'cefr'] as const) expect(SPEAKING_TOPICS_BANK.some((t) => t.category === c), c).toBe(true);
    for (const c of ['paragraph', 'essay-opinion', 'essay-discussion', 'letter-informal', 'letter-formal'] as const) expect(WRITING_TOPICS_BANK.some((t) => t.category === c), c).toBe(true);
  });
});
