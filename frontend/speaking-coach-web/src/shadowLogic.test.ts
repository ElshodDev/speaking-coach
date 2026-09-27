import { describe, expect, it } from 'vitest';
import {
  activeWord,
  audioExt,
  bareWord,
  bySeries,
  fractionAt,
  handsFreeRecordMs,
  hasTranslations,
  isKeyWord,
  nextLesson,
  nextLoop,
  playsFor,
  progressMap,
  tokenize,
  translationOf,
  videoFraction,
  average,
  canFinish,
  CHARACTER_VOICE,
  clock,
  filterLessons,
  levelsIn,
  lineSeconds,
  nextIndex,
  recordLimitMs,
  repeatPauseMs,
  scoreKey,
  segment,
  thumbnail,
  type ShadowLesson,
  type ShadowSummary,
} from './shadowLogic';

const video: ShadowLesson = {
  id: 'v', kind: 'youtube', title: 'Clip', level: 'B1', videoId: 'dQw4w9WgXcQ', from: 60, to: 240, characters: [],
  lines: [
    { text: 'Hello there.', start: 60.1, end: 61.5, speaker: 0 },
    { text: 'How are you doing today, my friend?', start: 62, end: 65, speaker: 0 },
  ],
};

describe('shadowing timing', () => {
  it('pads video segments but never starts before the clip', () => {
    expect(segment(video, 0)).toEqual({ from: 60, to: 61.85 });
    expect(segment(video, 1).from).toBeCloseTo(61.75);
    // Oxirgi gap qism oxiridan o'tmaydi (pleyer tugashni kutib qolmasin)
    expect(segment({ ...video, to: 65.1 }, 1).to).toBe(65.1);
    expect(segment({ ...video, to: 64 }, 1).to).toBe(65);
  });

  it('estimates character lines from word count', () => {
    expect(lineSeconds({ text: 'one two three four five six', start: null, end: null, speaker: 0 })).toBeCloseTo(6 / 2.6);
    expect(lineSeconds({ text: 'Hi', start: null, end: null, speaker: 0 })).toBe(1.2);
    expect(lineSeconds(video.lines[1])).toBe(3);
  });

  it('gives time to repeat, longer when slowed down, within 2.5–15 s', () => {
    expect(repeatPauseMs(video.lines[1], 1)).toBe(4900);
    expect(repeatPauseMs(video.lines[1], 0.75)).toBeGreaterThan(4900);
    expect(repeatPauseMs(video.lines[1], 1.25)).toBe(4900);
    expect(repeatPauseMs({ text: 'Hi', start: 1, end: 1.2, speaker: 0 }, 1)).toBe(2500);
    expect(repeatPauseMs({ text: 'x', start: 0, end: 30, speaker: 0 }, 1)).toBe(15000);
  });

  it('limits recordings sensibly', () => {
    expect(recordLimitMs(video.lines[0])).toBe(5080);
    expect(recordLimitMs({ text: 'Hi', start: 0, end: 0.5, speaker: 0 })).toBe(4000);
    expect(recordLimitMs({ text: 'x', start: 0, end: 20, speaker: 0 })).toBe(25000);
  });
});

describe('shadowing list', () => {
  const list: ShadowSummary[] = [
    { id: 'a', kind: 'youtube', title: 'A', level: 'B2', videoId: 'x', characters: [], lines: 10, seconds: 120, episode: 1 },
    { id: 'b', kind: 'character', title: 'B', level: 'A2', videoId: null, characters: ['cat', 'bear'], lines: 8, seconds: 30, episode: 1 },
    { id: 'c', kind: 'character', title: 'C', level: 'B2', videoId: null, characters: ['owl'], lines: 7, seconds: 30, episode: 2 },
  ];
  it('filters by level and kind', () => {
    expect(filterLessons(list, { level: null, kind: null }).map((l) => l.id)).toEqual(['a', 'b', 'c']);
    expect(filterLessons(list, { level: 'B2', kind: null }).map((l) => l.id)).toEqual(['a', 'c']);
    expect(filterLessons(list, { level: 'B2', kind: 'character' }).map((l) => l.id)).toEqual(['c']);
    expect(filterLessons(list, { level: 'C1', kind: null })).toEqual([]);
  });
  it('lists only the levels present, in CEFR order', () => {
    expect(levelsIn(list)).toEqual(['A2', 'B2']);
  });
});

describe('shadowing helpers', () => {
  it('formats clock and thumbnails', () => {
    expect(clock(83.9)).toBe('1:23');
    expect(clock(5)).toBe('0:05');
    expect(thumbnail('abc')).toBe('https://i.ytimg.com/vi/abc/mqdefault.jpg');
  });
  it('scores, averages and finishing rule', () => {
    expect([scoreKey(90), scoreKey(75), scoreKey(55), scoreKey(10)]).toEqual(['great', 'good', 'mid', 'low']);
    expect(average([])).toBeNull();
    expect(average([70, 81])).toBe(76);
    expect(canFinish(2, 8)).toBe(false);
    expect(canFinish(4, 8)).toBe(true);
    expect(canFinish(3, 3)).toBe(true);
    expect(canFinish(2, 3)).toBe(false);
    expect(canFinish(5, 10)).toBe(true);
  });
  it('picks a file extension from the recording type', () => {
    expect(audioExt('audio/mp4')).toBe('mp4');
    expect(audioExt('audio/ogg;codecs=opus')).toBe('ogg');
    expect(audioExt('audio/webm;codecs=opus')).toBe('webm');
  });
  it('stops at the last line', () => {
    expect(nextIndex(0, 2)).toBe(1);
    expect(nextIndex(1, 2)).toBeNull();
  });
  it('gives every character a distinct voice setting', () => {
    const keys = Object.values(CHARACTER_VOICE).map((v) => `${v.voice}-${v.pitch}`);
    expect(new Set(keys).size).toBe(keys.length);
  });
});

describe('karaoke', () => {
  const text = 'Hello there, my good friend!';
  it('splits words and keeps punctuation attached', () => {
    const t = tokenize(text);
    expect(t.filter((x) => x.word).map((x) => x.text)).toEqual(['Hello', 'there,', 'my', 'good', 'friend!']);
    expect(t.map((x) => x.text).join('')).toBe(text);
    expect(tokenize('— ok').filter((x) => x.word).map((x) => x.text)).toEqual(['ok']);
  });
  it('lights the word that is being said, weighted by length', () => {
    expect(activeWord(text, null)).toBe(-1);
    expect(activeWord(text, 0)).toBe(-1);
    expect(activeWord(text, 0.05)).toBe(0);
    expect(activeWord(text, 0.3)).toBe(1);
    expect(activeWord(text, 0.99)).toBe(4);
    expect(activeWord(text, 1)).toBe(4);
  });
  it('turns browser boundaries and video time into fractions', () => {
    expect(fractionAt('abcd', 1)).toBe(0.5);
    expect(fractionAt('abcd', 10)).toBe(1);
    expect(videoFraction({ text: 'x', start: 10, end: 12, speaker: 0 }, 11)).toBe(0.5);
    expect(videoFraction({ text: 'x', start: 10, end: 12, speaker: 0 }, 9)).toBe(0);
    expect(videoFraction({ text: 'x', start: null, end: null, speaker: 0 }, 9)).toBeNull();
  });
  it('matches key words regardless of case and punctuation', () => {
    expect(bareWord('“Apparently,')).toBe('apparently');
    expect(bareWord('don’t')).toBe("don't");
    expect(isKeyWord('Apparently,', ['apparently'])).toBe(true);
    expect(isKeyWord('habit.', ['habit'])).toBe(true);
    expect(isKeyWord('habits', ['habit'])).toBe(false);
    expect(isKeyWord('x', null)).toBe(false);
  });
});

describe('modes, loop and translations', () => {
  it('cycles loop off → 3× → ∞', () => {
    expect(nextLoop(0)).toBe(3);
    expect(nextLoop(3)).toBe('inf');
    expect(nextLoop('inf')).toBe(0);
    expect([playsFor(0), playsFor(3), playsFor('inf')]).toEqual([1, 3, Infinity]);
  });
  it('gives enough time to repeat in hands-free mode', () => {
    expect(handsFreeRecordMs({ text: 'x', start: 0, end: 2, speaker: 0 }, 1)).toBe(4200);
    expect(handsFreeRecordMs({ text: 'x', start: 0, end: 2, speaker: 0 }, 0.5)).toBe(7200);
    expect(handsFreeRecordMs({ text: 'x', start: 0, end: 0.4, speaker: 0 }, 1)).toBe(2500);
    expect(handsFreeRecordMs({ text: 'x', start: 0, end: 15, speaker: 0 }, 1)).toBe(20000);
  });
  it('shows translations only in uz/ru and when present', () => {
    const l = { text: 'Hi', start: null, end: null, speaker: 0, tr: { uz: 'Salom', ru: ' ' } };
    expect(translationOf(l, 'uz')).toBe('Salom');
    expect(translationOf(l, 'ru')).toBeNull();
    expect(translationOf(l, 'en')).toBeNull();
    expect(hasTranslations({ ...video, lines: [l] }, 'uz')).toBe(true);
    expect(hasTranslations(video, 'uz')).toBe(false);
  });
});

describe('series and next lesson', () => {
  const s = (id: string, kind: 'youtube' | 'character', episode: number): ShadowSummary =>
    ({ id, kind, title: id, level: 'B1', videoId: null, characters: [], lines: 5, seconds: 30, episode });
  const list = [s('c2', 'character', 2), s('v1', 'youtube', 1), s('c1', 'character', 1), s('c3', 'character', 3), s('v2', 'youtube', 2)];
  it('groups videos first, then characters, in episode order', () => {
    expect(bySeries(list).map((g) => [g.kind, g.lessons.map((l) => l.id)])).toEqual([
      ['youtube', ['v1', 'v2']],
      ['character', ['c1', 'c2', 'c3']],
    ]);
    expect(bySeries([s('c1', 'character', 1)]).map((g) => g.kind)).toEqual(['character']);
  });
  it('picks the next lesson in the series, skipping finished ones', () => {
    expect(nextLesson(list, 'c1', {})!.id).toBe('c2');
    expect(nextLesson(list, 'c1', { c2: true })!.id).toBe('c3');
    // Seriya oxiri — boshqa seriyadagi birinchi bajarilmagan dars
    expect(nextLesson(list, 'c3', { c1: true, c2: true, c3: true })!.id).toBe('v1');
    // Hammasi bajarilgan — seriyadagi keyingisi; oxirgisi — yo'q
    const all = { v1: 1, v2: 1, c1: 1, c2: 1, c3: 1 };
    expect(nextLesson(list, 'c2', all)!.id).toBe('c3');
    expect(nextLesson(list, 'c3', all)).toBeNull();
    expect(nextLesson(list, 'missing', {})).toBeNull();
  });
  it('indexes progress by lesson', () => {
    expect(progressMap([{ lessonId: 'a', times: 2, best: 80 }]).a.best).toBe(80);
  });
});
