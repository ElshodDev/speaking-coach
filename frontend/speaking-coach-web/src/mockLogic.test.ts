import { describe, expect, it } from 'vitest';
import { bandKey, countWords, draftUsable, formatBand, formatClock, speakingSteps, type SpeakingSet } from './mockLogic';
import { wrapLabel } from './MockChart';
import { mockMsg } from './locales/mock';

const set: SpeakingSet = {
  id: 's1',
  part1Topic: 'Home',
  part1: ['q1', 'q2', 'q3', 'q4'],
  part2: { topic: 'Describe a place.', points: ['a', 'b', 'c'], explain: 'and explain why.' },
  part3: ['d1', 'd2', 'd3', 'd4'],
};
const timing = { part1AnswerSeconds: 45, part2PrepSeconds: 60, part2SpeakSeconds: 120, part3AnswerSeconds: 75 };

describe('speakingSteps', () => {
  const steps = speakingSteps(set, timing);

  it('follows the exam order with a preparation minute before Part 2', () => {
    expect(steps.map((s) => (s.kind === 'prep' ? 'prep' : `${s.part}:${s.index}`))).toEqual([
      '1:0', '1:1', '1:2', '1:3', 'prep', '2:4', '3:5', '3:6', '3:7', '3:8',
    ]);
  });

  it('uses the official Part 2 timing and per-part limits', () => {
    const prep = steps[4];
    const talk = steps[5];
    expect(prep.kind === 'prep' && prep.seconds).toBe(60);
    expect(talk.kind === 'answer' && talk.maxSeconds).toBe(120);
    const total = steps.reduce((sum, s) => sum + (s.kind === 'prep' ? s.seconds : s.maxSeconds), 0);
    expect(total / 60).toBeCloseTo(11, 0); // ielts.org: 11–14 minutes
  });

  it('numbers questions within each part', () => {
    const p3 = steps.filter((s) => s.kind === 'answer' && s.part === 3);
    expect(p3.map((s) => s.kind === 'answer' && `${s.numberInPart}/${s.ofInPart}`)).toEqual(['1/4', '2/4', '3/4', '4/4']);
  });
});

describe('formatting', () => {
  it('shows bands with one decimal like IELTS results', () => {
    expect(formatBand(6.5)).toBe('6.5');
    expect(formatBand(7)).toBe('7.0');
    expect(formatBand(null)).toBe('—');
  });
  it('maps half bands down to the band name', () => {
    expect(bandKey(6.5)).toBe(6);
    expect(bandKey(9)).toBe(9);
    expect(bandKey(-1)).toBe(0);
    expect(mockMsg.en.bandNames[bandKey(7.5)]).toBe('Good');
  });
  it('formats a countdown clock', () => {
    expect(formatClock(0)).toBe('0:00');
    expect(formatClock(59.2)).toBe('1:00');
    expect(formatClock(3600)).toBe('60:00');
    expect(formatClock(-5)).toBe('0:00');
  });
});

describe('countWords (same rule as the server)', () => {
  it.each([
    ['', 0],
    ['Hello world', 2],
    ["It's a well-known fact — 42 people agree.", 7],
    ['• first line\n- second, third', 4],
  ])('%j → %i', (text, n) => expect(countWords(text)).toBe(n));
});

describe('draftUsable', () => {
  const d = { setId: 'a1', variant: 'academic' as const, startedAt: 0, task1: '', task2: '' };
  it('allows resuming only the same variant within the time (+5 min grace)', () => {
    expect(draftUsable(d, 'academic', 60, 30 * 60_000)).toBe(true);
    expect(draftUsable(d, 'academic', 60, 66 * 60_000)).toBe(false);
    expect(draftUsable(d, 'general', 60, 1000)).toBe(false);
    expect(draftUsable(null, 'academic', 60, 1000)).toBe(false);
  });
});

describe('wrapLabel', () => {
  it('keeps short labels and wraps long ones into at most two lines', () => {
    expect(wrapLabel('2005', 60)).toEqual(['2005']);
    const lines = wrapLabel('Using the internet', 60);
    expect(lines.length).toBe(2);
    expect(lines.join(' ')).toBe('Using the internet');
  });
});
