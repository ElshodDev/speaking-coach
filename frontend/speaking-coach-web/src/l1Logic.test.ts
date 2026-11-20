import { describe, expect, it } from 'vitest';
import { ALL_TRAPS, TRAP_SETS } from './content/l1traps';
import { BLANK, cardsFromWrong, fill, hasTrapsFor, itemsFor, score, shuffled } from './l1Logic';

// MistakeAnalyzer.cs dagi xato turlari — "Mening xatolarim" bilan bog'lanish shu ID'lar orqali.
const MISTAKE_TYPES = ['articles', 'prepositions', 'agreement', 'tense', 'plural', 'comparatives', 'word_order', 'spelling', 'word_choice', 'other'];

describe('l1 traps content', () => {
  it('has unique ids and well-formed items', () => {
    const ids = ALL_TRAPS.map((t) => t.id);
    expect(new Set(ids).size).toBe(ids.length);
    expect(ALL_TRAPS.length).toBeGreaterThanOrEqual(35);
    for (const t of ALL_TRAPS) {
      expect(t.q.split(BLANK).length - 1, t.id).toBe(1);
      expect(t.options.length, t.id).toBeGreaterThanOrEqual(2);
      expect(new Set(t.options).size, t.id).toBe(t.options.length);
      expect(MISTAKE_TYPES, t.id).toContain(t.cat);
      expect(t.l1.trim(), t.id).not.toBe('');
      for (const lang of ['uz', 'ru', 'en'] as const) expect(t.explain[lang].trim().length, `${t.id}.${lang}`).toBeGreaterThan(10);
    }
  });

  it('Uzbek explanations follow the app orthography (ʻ for oʻ/gʻ, ʼ for the glottal stop)', () => {
    const bad = ALL_TRAPS.filter((t) => /[‘’]/.test(t.explain.uz) || /(?<![A-Za-z])(?:[a-zA-Z]*[oOgG])'(?=[a-zʻ])/.test(t.explain.uz) || /[oOgG]['‘’]/.test(t.l1));
    expect(bad.map((t) => t.id)).toEqual([]);
  });

  it('every set has items', () => {
    for (const s of TRAP_SETS) expect(s.items.length, s.id).toBeGreaterThanOrEqual(10);
  });
});

describe('l1Logic', () => {
  it('fill replaces the blank, and "(nothing)" removes it cleanly', () => {
    expect(fill('I bought bread at the ___.', 'shop')).toBe('I bought bread at the shop.');
    expect(fill('Let’s discuss ___ the plan.', '(nothing)')).toBe('Let’s discuss the plan.');
  });

  it('shuffle is deterministic and keeps all elements', () => {
    const a = shuffled([1, 2, 3, 4, 5], 42);
    expect(shuffled([1, 2, 3, 4, 5], 42)).toEqual(a);
    expect([...a].sort()).toEqual([1, 2, 3, 4, 5]);
  });

  it('items by set or by mistake category', () => {
    expect(itemsFor('prepositions').length).toBe(TRAP_SETS.find((s) => s.id === 'prepositions')!.items.length);
    expect(itemsFor('cat-articles').every((t) => t.cat === 'articles')).toBe(true);
    expect(itemsFor('nope')).toEqual([]);
    expect(hasTrapsFor('prepositions')).toBe(true);
    expect(hasTrapsFor('spelling')).toBe(false);
  });

  it('score and cards from wrong answers', () => {
    const answers = [
      { id: 'ff-magazine', chosen: 'magazine', correct: false },
      { id: 'pr-depend', chosen: 'on', correct: true },
    ];
    expect(score(answers)).toEqual({ right: 1, total: 2, pct: 50 });
    const [card, ...rest] = cardsFromWrong(answers, 'ru');
    expect(rest).toEqual([]);
    expect(card.front).toBe('I bought bread and milk at the ___ near my house. (shop / magazine)');
    expect(card.back).toBe('I bought bread and milk at the shop near my house.');
    expect(card.note).toContain('журнал');
  });
});
