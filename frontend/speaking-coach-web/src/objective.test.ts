import { describe, expect, it } from 'vitest';
import { answeredCount, questionNumbers, splitGap, type ClientGroup } from './ObjectiveQuestions';
import { pickVoices } from './MockListening';
import { FULL_ORDER } from './MockFull';
import { mockObjMsg } from './locales/mockObjective';

const groups: ClientGroup[] = [
  { type: 'gap', instructions: 'ONE WORD', maxWords: 1, questions: [{ number: 1, prompt: 'Meet at the ___ gate', options: null }, { number: 2, prompt: 'Cost: £___', options: null }] },
  { type: 'mcq', instructions: 'Choose', maxWords: null, questions: [{ number: 3, prompt: 'Why?', options: ['a', 'b', 'c', 'd'] }] },
];

describe('objective questions', () => {
  it('splits a gap prompt around the blank', () => {
    expect(splitGap('Meet at the ___ gate')).toEqual(['Meet at the ', ' gate']);
    expect(splitGap('Cost: £_____')).toEqual(['Cost: £', '']);
    expect(splitGap('No blank')).toEqual(['No blank', '']);
  });

  it('counts only non-blank answers', () => {
    expect(questionNumbers(groups)).toEqual([1, 2, 3]);
    expect(answeredCount({ '1': 'main', '2': '  ', '3': 'B' }, [1, 2, 3])).toBe(2);
  });
});

const voice = (name: string, lang = 'en-GB') => ({ name, lang }) as SpeechSynthesisVoice;

describe('pickVoices', () => {
  it('uses a male and a female English voice when available', () => {
    const v = pickVoices([voice('Google русский', 'ru-RU'), voice('Google UK English Female'), voice('Google UK English Male'), voice('Daniel')]);
    expect(v.female?.name).toBe('Google UK English Female');
    expect(v.male?.name).toBe('Google UK English Male');
  });

  it('falls back to one voice (the caller then changes the pitch)', () => {
    const v = pickVoices([voice('Samantha', 'en-US')]);
    expect(v.male).toBe(v.female);
  });
});

describe('full exam', () => {
  it('runs in the official order: Listening, Reading, Writing, Speaking', () => {
    expect([...FULL_ORDER]).toEqual(['listening', 'reading', 'writing', 'speaking']);
    for (const lang of ['uz', 'ru', 'en'] as const) expect(Object.keys(mockObjMsg[lang].moduleNames).sort()).toEqual([...FULL_ORDER].sort());
  });
});
