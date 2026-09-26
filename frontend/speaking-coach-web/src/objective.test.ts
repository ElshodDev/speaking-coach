import { describe, expect, it } from 'vitest';
import { answeredCount, choiceLabel, questionNumbers, splitGap, type ClientGroup } from './ObjectiveQuestions';
import { pickVoices, playbackPlan } from './MockListening';
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

describe('CEFR: tinglash tartibi (har yozuv ikki marta)', () => {
  it('IELTS — bir marta', () => {
    expect(playbackPlan('ielts', 1, 3)).toEqual([[0, 1, 2]]);
  });
  it('CEFR 1-qism — har gap ketma-ket ikki marta', () => {
    expect(playbackPlan('cefr', 1, 3)).toEqual([[0, 0, 1, 1, 2, 2]]);
  });
  it('CEFR boshqa qismlar — butun yozuv ikki marta', () => {
    expect(playbackPlan('cefr', 4, 2)).toEqual([[0, 1], [0, 1]]);
  });
});

describe('choiceLabel: harf va variant matni', () => {
  const match: ClientGroup = { type: 'match', instructions: '', maxWords: null, options: ['a student', 'a teacher'], questions: [{ number: 15, prompt: 'Speaker 1', options: null }] };
  const mcq: ClientGroup = { type: 'mcq', instructions: '', maxWords: null, questions: [{ number: 1, prompt: '?', options: ['Yes, sure.', 'No.', 'At six.'] }] };
  const map: ClientGroup = { type: 'map', instructions: '', maxWords: null, map: { title: 'Park', landmarks: [], spots: [{ label: 'A', x: 0, y: 0 }] }, questions: [{ number: 19, prompt: 'Café', options: null }] };
  it('moslashtirish va variantli savol', () => {
    expect(choiceLabel(match, match.questions[0], 'b')).toBe('B — a teacher');
    expect(choiceLabel(mcq, mcq.questions[0], 'C')).toBe('C — At six.');
  });
  it('xarita, TRUE/FALSE va notanish qiymat — o‘zgarishsiz', () => {
    expect(choiceLabel(map, map.questions[0], 'A')).toBe('A');
    expect(choiceLabel(mcq, mcq.questions[0], 'NOT GIVEN')).toBe('NOT GIVEN');
    expect(choiceLabel(undefined, undefined, 'B')).toBe('B');
  });
});
