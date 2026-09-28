import { describe, expect, it } from 'vitest';
import { DICTATION } from './content/dictation';
import { LEVELS } from './content/types';
import { checkDictation, normalizeWord, pickSentences } from './dictationLogic';
import { trendOf } from './Mistakes';
import { shareTiles } from './ShareCard';
import { orderScenarios } from './Talk';
import { TALK_SCENARIOS, talkMsg } from './locales/talk';
import { mistakesMsg } from './locales/mistakes';

describe('dictation', () => {
  it('ignores case, punctuation and apostrophe style', () => {
    expect(normalizeWord('Don’t,')).toBe("don't");
    const r = checkDictation('It is seven o’clock.', "it is seven o'clock");
    expect(r.correct).toBe(4);
    expect(r.total).toBe(4);
    expect(r.marks.every((m) => m.status === 'ok')).toBe(true);
  });

  it('marks wrong, missing and extra words', () => {
    const r = checkDictation('My sister works in a small bakery.', 'My sistr works in small bakery today');
    expect(r.total).toBe(7);
    expect(r.correct).toBe(5);
    expect(r.marks.map((m) => m.status)).toEqual(['ok', 'wrong', 'ok', 'ok', 'missing', 'ok', 'ok', 'extra']);
    expect(r.marks[1]).toMatchObject({ expected: 'sister', typed: 'sistr' });
  });

  it('an empty answer misses every word', () => {
    const r = checkDictation('Please sit down.', '   ');
    expect(r.correct).toBe(0);
    expect(r.marks.every((m) => m.status === 'missing')).toBe(true);
  });

  it('picks unseen sentences first', () => {
    const all = ['a', 'b', 'c', 'd'];
    const picked = pickSentences(all, ['a', 'b'], 3, () => 0.5);
    expect(picked.slice(0, 2).sort()).toEqual(['c', 'd']);
    expect(picked).toHaveLength(3);
  });

  it('every level has at least 15 sentences of a suitable length', () => {
    const limits = { A2: [5, 11], B1: [8, 15], B2: [11, 19], C1: [13, 23] } as const;
    for (const lv of LEVELS) {
      expect(DICTATION[lv].length).toBeGreaterThanOrEqual(15);
      for (const s of DICTATION[lv]) {
        const n = s.split(/\s+/).length;
        expect(n, `${lv}: ${s}`).toBeGreaterThanOrEqual(limits[lv][0]);
        expect(n, `${lv}: ${s}`).toBeLessThanOrEqual(limits[lv][1]);
      }
    }
  });
});

describe('talk', () => {
  it('has more than ten scenarios, each described in three languages', () => {
    expect(TALK_SCENARIOS.length).toBeGreaterThan(10);
    for (const s of TALK_SCENARIOS) for (const lang of ['uz', 'ru', 'en'] as const) expect(talkMsg[lang].scenarios[s.id]?.title).toBeTruthy();
  });

  it('puts scenarios of the learner level first', () => {
    expect(orderScenarios('B2')[0].level).toBe('B2');
    expect(orderScenarios('A2')[0].level).toBe('A2');
  });
});

describe('mistakes and share card', () => {
  it('every mistake type has a name and tip in three languages', () => {
    const ids = ['articles', 'prepositions', 'agreement', 'tense', 'plural', 'comparatives', 'word_order', 'spelling', 'word_choice', 'other'];
    for (const lang of ['uz', 'ru', 'en'] as const) for (const id of ids) expect(mistakesMsg[lang].cats[id]?.tip).toBeTruthy();
  });

  it('trend is negative when mistakes decrease', () => {
    expect(trendOf({ last30: 2, prev30: 5 })).toBe(-3);
  });

  it('shows the best streak when the current one is zero', () => {
    const t = { streak: 'streak', bestStreak: 'best', activities: 'a', reviews: 'r', badges: 'b' };
    const base = { name: 'A', level: 3, xp: 420, streak: 0, bestStreak: 9, activities: 31, reviews: 118, badges: 5, badgesTotal: 12 };
    expect(shareTiles(base, t)[0]).toEqual({ emoji: '🔥', value: '9', label: 'best' });
    expect(shareTiles({ ...base, streak: 4 }, t)[0].value).toBe('4');
    expect(shareTiles(base, t)[3].value).toBe('5/12');
  });
});
