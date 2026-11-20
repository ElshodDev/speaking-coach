import { describe, expect, it } from 'vitest';
import { defaultTarget, highlight, wordCount, type UpgradeItem } from './upgradeLogic';

const item = (original: string): UpgradeItem => ({ original, upgraded: 'x', kind: 'vocabulary', why: '' });

describe('upgradeLogic', () => {
  it('default target is one step above the learner level', () => {
    expect(defaultTarget('A2')).toBe('B1');
    expect(defaultTarget('B1')).toBe('B2');
    expect(defaultTarget('B2')).toBe('C1');
    expect(defaultTarget('C1')).toBe('C1');
  });

  it('highlights upgraded phrases in order, case-insensitively, keeping the rest', () => {
    const text = 'I think phones is very bad. They play games all time.';
    const segs = highlight(text, [item('They play games all time.'), item('Phones is very bad')]);
    expect(segs.map((s) => s.text).join('')).toBe(text); // matn o'zgarmaydi
    expect(segs.filter((s) => s.item !== undefined).map((s) => [s.text, s.item])).toEqual([
      ['phones is very bad', 1],
      ['They play games all time', 0],
    ]);
  });

  it('skips missing and overlapping phrases', () => {
    const segs = highlight('one two three', [item('one two'), item('two three'), item('four')]);
    expect(segs.filter((s) => s.item !== undefined).map((s) => s.text)).toEqual(['one two']);
    expect(highlight('abc', [])).toEqual([{ text: 'abc' }]);
    expect(highlight('', [item('a')])).toEqual([]);
  });

  it('word count', () => {
    expect(wordCount("I don't like it.")).toBe(4);
    expect(wordCount('')).toBe(0);
  });
});
