import { describe, expect, it } from 'vitest';
import { cardsMessage } from './cards';
import { cardsMsg } from './locales/cards';
import { reviewMsg } from './locales/review';
import { promptFor, spokenParts, type ReviewCard } from './Review';

const card = (kind: ReviewCard['kind']): ReviewCard => ({
  id: '1',
  kind,
  source: null,
  front: 'I goes to school',
  back: 'I go to school',
  note: "Use 'go' with 'I'.",
});

// Test muhitida brauzer tili (navigator) ingliz boʻlishi mumkin — shuning uchun
// oʻzbekcha matnlar aniq til bilan tekshiriladi.
const uz = cardsMsg.uz;

describe('cardsMessage', () => {
  it("yangi karta bo'lmasa oddiy 'Tayyor'", () => {
    expect(cardsMessage(0, uz)).toBe('Tayyor');
    expect(cardsMessage(undefined, uz)).toBe('Tayyor');
  });

  it('yangi kartalar sonini aytadi', () => {
    expect(cardsMessage(2, uz)).toContain('2 ta yangi takrorlash kartasi');
    expect(cardsMessage(2, uz)).toContain('qoʻshildi');
  });

  it('rus va ingliz tillarida koʻplik shakllari toʻgʻri', () => {
    expect(cardsMsg.ru.added(1)).toContain('добавлена 1 новая карточка');
    expect(cardsMsg.ru.added(3)).toContain('добавлены 3 новые карточки');
    expect(cardsMsg.ru.added(5)).toContain('добавлено 5 новых карточек');
    expect(cardsMsg.en.added(1)).toContain('1 new review card added');
    expect(cardsMsg.en.added(4)).toContain('4 new review cards added');
    expect(cardsMessage(0, cardsMsg.en)).toBe('Done');
    expect(cardsMessage(1, cardsMsg.ru)).toMatch(/^Готово — /);
  });
});

describe('karta matnlari', () => {
  it('karta turiga qarab savol sarlavhasi', () => {
    expect(promptFor('Correction', reviewMsg.uz)).toContain('toʻgʻri');
    expect(promptFor('Question', reviewMsg.uz)).toContain('Savol');
    expect(promptFor('Manual', reviewMsg.uz)).toContain('Maʼnosini');
  });

  it("'Yo'lda' rejimida tuzatish kartasi savol va javob bo'lib o'qiladi", () => {
    const parts = spokenParts(card('Correction'));
    expect(parts.question).toBe('How would you correct this? I goes to school');
    expect(parts.answer).toContain('Better: I go to school.');
    expect(parts.answer).toContain("Use 'go' with 'I'.");
  });
});
