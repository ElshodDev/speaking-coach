import { describe, expect, it } from 'vitest';
import { normalizeWord, splitToken } from './WordSheet';

describe('splitToken', () => {
  it('tinish belgilarini so\'zdan ajratadi', () => {
    expect(splitToken('"medicine.')).toEqual({ before: '"', word: 'medicine', after: '.' });
    expect(splitToken('well-known,')).toEqual({ before: '', word: 'well-known', after: ',' });
    expect(splitToken("don't")?.word).toBe("don't");
  });

  it("so'z bo'lmasa null", () => {
    expect(splitToken('—')).toBeNull();
    expect(splitToken('2026')).toBeNull();
  });

  it('egri apostrof oddiysiga almashadi', () => {
    expect(normalizeWord('it’s')).toBe("it's");
  });
});
