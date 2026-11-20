import { describe, expect, it } from 'vitest';
import { clock, coveragePercent, nextCard, speakingHint, timeVerdict, type Part2Card } from './part2Logic';

const cards: Part2Card[] = [
  { id: 'a', topic: 'A', points: [], explain: '', done: true },
  { id: 'b', topic: 'B', points: [], explain: '' },
  { id: 'c', topic: 'C', points: [], explain: '' },
];

describe('part2Logic', () => {
  it('time verdict', () => {
    expect(timeVerdict(45)).toBe('short');
    expect(timeVerdict(75)).toBe('fair');
    expect(timeVerdict(110)).toBe('good');
  });

  it('clock', () => {
    expect(clock(0)).toBe('0:00');
    expect(clock(65.7)).toBe('1:05');
    expect(clock(-3)).toBe('0:00');
  });

  it('coverage percent counts partly as half and matches the server rounding', () => {
    expect(coveragePercent([])).toBe(0);
    expect(coveragePercent([
      { point: 'a', status: 'covered', evidence: '' },
      { point: 'b', status: 'partly', evidence: '' },
      { point: 'c', status: 'missed', evidence: '' },
      { point: 'd', status: 'covered', evidence: '' },
    ])).toBe(63);
  });

  it('next card prefers cards not done yet and never repeats the current one', () => {
    expect(nextCard(cards, 'b', () => 0)!.id).toBe('c');
    expect(nextCard(cards, null, () => 0)!.id).toBe('b');
    const allDone = cards.map((c) => ({ ...c, done: true }));
    expect(nextCard(allDone, 'a', () => 0)!.id).toBe('b');
    expect(nextCard([cards[0]], 'a')!.id).toBe('a');
    expect(nextCard([], null)).toBeNull();
  });

  it('speaking hints move from keep going to wrap up', () => {
    expect(speakingHint(30, 120)).toBe('keepGoing');
    expect(speakingHint(70, 120)).toBe('addIdea');
    expect(speakingHint(105, 120)).toBe('wrapUp');
  });
});
