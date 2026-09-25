import { describe, expect, it } from 'vitest';
import { vocabMsg } from './locales/vocab';
import { filterWords, formatDate, isLookupWord, parseNote, type VocabWord } from './Vocab';

const w = (word: string, meaning: string, status: VocabWord['status']): VocabWord => ({
  id: word,
  word,
  meaning,
  note: '',
  kind: 'Word',
  source: null,
  status,
  due: false,
  createdAtUtc: '2026-09-25T10:00:00Z',
  lapses: 0,
});

const words = [w('medicine', 'dori', 'New'), w('reliable', 'ishonchli', 'Learning'), w('take for granted', "qadriga yetmaslik", 'Known')];

describe('parseNote', () => {
  it('splits part of speech from the rest', () => {
    expect(parseNote('(noun) a substance used to treat illness — Take it.')).toEqual({
      pos: 'noun',
      rest: 'a substance used to treat illness — Take it.',
    });
  });

  it('keeps notes without a part of speech as they are', () => {
    expect(parseNote('  Just an example.  ')).toEqual({ pos: null, rest: 'Just an example.' });
    expect(parseNote('')).toEqual({ pos: null, rest: '' });
  });
});

describe('filterWords', () => {
  it('searches both the word and its meaning, case-insensitively', () => {
    expect(filterWords(words, 'MEDI', 'all').map((x) => x.word)).toEqual(['medicine']);
    expect(filterWords(words, 'ishonch', 'all').map((x) => x.word)).toEqual(['reliable']);
  });

  it('filters by status and combines with the query', () => {
    expect(filterWords(words, '', 'Known').map((x) => x.word)).toEqual(['take for granted']);
    expect(filterWords(words, 'dori', 'Known')).toEqual([]);
    expect(filterWords(words, '  ', 'all')).toHaveLength(3);
  });
});

describe('isLookupWord', () => {
  it('accepts single English words only', () => {
    expect(isLookupWord('reliable')).toBe(true);
    expect(isLookupWord(" don't ")).toBe(true);
    expect(isLookupWord('well-known')).toBe(true);
    expect(isLookupWord('two words')).toBe(false);
    expect(isLookupWord('кот')).toBe(false);
    expect(isLookupWord('')).toBe(false);
  });
});

describe('formatDate', () => {
  it('shows day.month.year in local time', () => {
    expect(formatDate(new Date(2026, 8, 5, 12).toISOString())).toBe('05.09.2026');
  });
});

describe('vocabMsg', () => {
  it('counts words with correct plurals in every language', () => {
    expect(vocabMsg.uz.subtitle(4, 1)).toBe('4 ta soʻz · 1 tasi yodlangan');
    expect(vocabMsg.ru.subtitle(1, 0)).toBe('1 слово · выучено: 0');
    expect(vocabMsg.ru.subtitle(3, 1)).toBe('3 слова · выучено: 1');
    expect(vocabMsg.ru.subtitle(11, 2)).toBe('11 слов · выучено: 2');
    expect(vocabMsg.en.subtitle(1, 1)).toBe('1 word · 1 known');
    expect(vocabMsg.en.dueTitle(2)).toBe('2 words due');
    expect(vocabMsg.ru.dueTitle(21)).toBe('21 слово на очереди');
  });
});
