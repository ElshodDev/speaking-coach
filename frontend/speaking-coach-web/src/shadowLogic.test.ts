import { describe, expect, it } from 'vitest';
import {
  audioExt,
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
    { id: 'a', kind: 'youtube', title: 'A', level: 'B2', videoId: 'x', characters: [], lines: 10, seconds: 120 },
    { id: 'b', kind: 'character', title: 'B', level: 'A2', videoId: null, characters: ['cat', 'bear'], lines: 8, seconds: 30 },
    { id: 'c', kind: 'character', title: 'C', level: 'B2', videoId: null, characters: ['owl'], lines: 7, seconds: 30 },
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
