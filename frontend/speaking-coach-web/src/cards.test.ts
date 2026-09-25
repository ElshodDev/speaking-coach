import { describe, expect, it } from 'vitest';
import { cardsMessage } from './cards';
import { promptFor, spokenParts, type ReviewCard } from './Review';

const card = (kind: ReviewCard['kind']): ReviewCard => ({
  id: '1',
  kind,
  source: null,
  front: 'I goes to school',
  back: 'I go to school',
  note: "Use 'go' with 'I'.",
});

describe('cardsMessage', () => {
  it("yangi karta bo'lmasa oddiy 'Tayyor'", () => {
    expect(cardsMessage(0)).toBe('Tayyor');
    expect(cardsMessage(undefined)).toBe('Tayyor');
  });

  it('yangi kartalar sonini aytadi', () => {
    expect(cardsMessage(2)).toContain('2 ta yangi takrorlash kartasi');
  });
});

describe('karta matnlari', () => {
  it('karta turiga qarab savol sarlavhasi', () => {
    expect(promptFor('Correction')).toContain("to'g'ri");
    expect(promptFor('Question')).toContain('Savol');
    expect(promptFor('Manual')).toContain("Ma'nosini");
  });

  it("'Yo'lda' rejimida tuzatish kartasi savol va javob bo'lib o'qiladi", () => {
    const parts = spokenParts(card('Correction'));
    expect(parts.question).toBe('How would you correct this? I goes to school');
    expect(parts.answer).toContain('Better: I go to school.');
    expect(parts.answer).toContain("Use 'go' with 'I'.");
  });
});
