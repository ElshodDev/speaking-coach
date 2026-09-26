import { describe, expect, it } from 'vitest';
import { cefrLevel, cefrSpeakingSteps, speakingSteps, wordRangeStatus, type CefrSpeakingSet } from './mockLogic';
import { cefrMsg } from './locales/cefr';

const set: CefrSpeakingSet = {
  id: 'c1',
  part11: ['q1', 'q2', 'q3'],
  pictures: [{ emoji: '🚌', caption: 'A' }, { emoji: '🚲', caption: 'B' }],
  part12: ['p1', 'p2', 'p3'],
  part2Topic: 'Talk about a journey.',
  part2Questions: ['a', 'b', 'c'],
  part3Statement: 'Public transport should be free.',
  for: ['f1', 'f2', 'f3'],
  against: ['x1', 'x2', 'x3'],
};
const timing = { part11Seconds: 30, part12Seconds: 30, part2PrepSeconds: 60, part2SpeakSeconds: 120, part3PrepSeconds: 60, part3SpeakSeconds: 120 };

describe('cefrSpeakingSteps', () => {
  const steps = cefrSpeakingSteps(set, timing);

  it('follows 1.1 → 1.2 → 2 (prep + talk) → 3 (prep + talk) with server answer indexes 0–7', () => {
    expect(steps.map((s) => `${s.kind === 'prep' ? 'prep' : 'ans'}:${s.partLabel}:${s.index}`)).toEqual([
      'ans:1.1:0', 'ans:1.1:1', 'ans:1.1:2', 'ans:1.2:3', 'ans:1.2:4', 'ans:1.2:5', 'prep:2:6', 'ans:2:6', 'prep:3:7', 'ans:3:7',
    ]);
  });

  it('shows pictures in 1.2 and the for/against table in Part 3', () => {
    const p12 = steps[3];
    expect(p12.kind === 'answer' && p12.card?.pictures?.length).toBe(2);
    const p3 = steps[9];
    expect(p3.kind === 'answer' && p3.longTurn && p3.card?.table?.for).toEqual(['f1', 'f2', 'f3']);
    expect(p3.kind === 'answer' && p3.speak).toBe(false);
  });

  it('keeps IELTS steps working with the generic step type', () => {
    const ielts = speakingSteps(
      { id: 's1', part1Topic: 'x', part1: ['a', 'b', 'c', 'd'], part2: { topic: 'T', points: ['1', '2', '3'], explain: 'e' }, part3: ['p', 'q', 'r', 's'] },
      { part1AnswerSeconds: 45, part2PrepSeconds: 60, part2SpeakSeconds: 120, part3AnswerSeconds: 75 },
    );
    const talk = ielts[5];
    expect(talk.kind === 'answer' && talk.longTurn && talk.card?.bulletsHeading).toBe('youShouldSay');
  });
});

describe('CEFR scale (official cut-offs)', () => {
  it.each([
    [75, 'C1'], [65, 'C1'], [64, 'B2'], [51, 'B2'], [50, 'B1'], [38, 'B1'], [37, 'below B1'], [0, 'below B1'],
  ] as const)('%i → %s', (score, level) => expect(cefrLevel(score)).toBe(level));

  it('names levels in every language', () => {
    expect(cefrMsg.uz.level('below B1')).toBe('B1 dan past');
    expect(cefrMsg.ru.level('B2')).toBe('уровень B2');
  });
});

describe('wordRangeStatus', () => {
  it('flags answers outside the required range', () => {
    expect(wordRangeStatus(49, 50, 70)).toBe('low');
    expect(wordRangeStatus(50, 50, 70)).toBe('ok');
    expect(wordRangeStatus(70, 50, 70)).toBe('ok');
    expect(wordRangeStatus(71, 50, 70)).toBe('high');
  });
});
