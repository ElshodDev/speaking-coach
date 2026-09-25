import { describe, expect, it } from 'vitest';
import { splitSentences } from './speech';

describe('splitSentences', () => {
  it("matnni gaplarga bo'ladi, abzatslarni ham", () => {
    expect(splitSentences('Tea was medicine. Later it became a drink.\n\nToday people love it!')).toEqual([
      'Tea was medicine.',
      'Later it became a drink.',
      'Today people love it!',
    ]);
  });

  it("qo'shtirnoq va savol belgilarini to'g'ri ushlaydi", () => {
    expect(splitSentences('Are you free? "Great," she said. Call me')).toEqual([
      'Are you free?',
      '"Great," she said.',
      'Call me',
    ]);
  });

  it("tinish belgisiz matn — bitta bo'lak; bo'sh matn — hech narsa", () => {
    expect(splitSentences('No punctuation at all')).toEqual(['No punctuation at all']);
    expect(splitSentences('   ')).toEqual([]);
  });
});
