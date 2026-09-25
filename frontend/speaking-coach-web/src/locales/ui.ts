import { defineMessages } from '../i18n';

/** ui.tsx — ball shkalasi, tuzatishlar, tarix ro'yxati, mehmon eslatmasi. */
export const uiMsg = defineMessages(
  {
    level: { great: 'Aʼlo', good: 'Yaxshi', mid: 'Oʻrtacha', low: 'Ishlash kerak' },
    practiceBack: 'Mashqlar',
    corrections: 'Tuzatishlar',
    nextFocus: 'Keyingi eʼtibor:',
    history: (n: number) => `Oldingi urinishlar (${n})`,
    guest:
      'Siz mehmon sifatida ishlamoqdasiz: natijalar baholanadi, biroq saqlanmaydi va takrorlash kartalariga aylanmaydi.',
  },
  {
    ru: {
      level: { great: 'Отлично', good: 'Хорошо', mid: 'Средне', low: 'Нужно поработать' },
      practiceBack: 'Упражнения',
      corrections: 'Исправления',
      nextFocus: 'Над чем работать дальше:',
      history: (n: number) => `Предыдущие попытки (${n})`,
      guest:
        'Вы работаете как гость: результаты оцениваются, но не сохраняются и не превращаются в карточки для повторения.',
    },
    en: {
      level: { great: 'Excellent', good: 'Good', mid: 'Fair', low: 'Needs work' },
      practiceBack: 'Practice',
      corrections: 'Corrections',
      nextFocus: 'Next focus:',
      history: (n: number) => `Previous attempts (${n})`,
      guest: "You're using the app as a guest: your work is graded but not saved, and no review cards are created.",
    },
  },
);
