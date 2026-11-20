import { describe, expect, it } from 'vitest';
import { safeJson, scoreText, textOf } from './safeJson';

describe('safeJson', () => {
  it('parses objects and returns null for broken or non-object input', () => {
    expect(safeJson<{ a: number }>('{"a":1}')).toEqual({ a: 1 });
    expect(safeJson('{"a":')).toBeNull();
    expect(safeJson('<html>')).toBeNull();
    expect(safeJson('null')).toBeNull();
    expect(safeJson('42')).toBeNull();
    expect(safeJson('')).toBeNull();
    expect(safeJson(undefined)).toBeNull();
  });
  it('reads scores and text defensively', () => {
    expect(scoreText({ score: 6.5 })).toBe('6.5');
    expect(scoreText({ score: 0 })).toBe('0');
    expect(scoreText(undefined)).toBe('–');
    expect(scoreText({ score: 'x' })).toBe('–');
    expect(scoreText(null)).toBe('–');
    expect(textOf('topic')).toBe('topic');
    expect(textOf(5)).toBe('');
  });
});
