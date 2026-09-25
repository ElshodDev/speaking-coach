import { defineMessages } from '../i18n';

/** WordSheet.tsx — soʻz maʼnosi oynasi. */
export const wordSheetMsg = defineMessages(
  {
    dialogLabel: (word: string) => `“${word}” soʻzining maʼnosi`,
    loading: 'Maʼnosi qidirilmoqda...',
    loadError: 'Maʼnosini olib boʻlmadi',
    add: '➕ Lugʻatga qoʻshish',
    added: 'Lugʻatga qoʻshildi ✅ — takrorlashda ham chiqadi.',
    loginToSave: 'Saqlash uchun kiring',
  },
  {
    ru: {
      dialogLabel: (word: string) => `Значение слова «${word}»`,
      loading: 'Ищем значение...',
      loadError: 'Не удалось получить значение',
      add: '➕ Добавить в словарь',
      added: 'Добавлено в словарь ✅ — слово появится и в повторении.',
      loginToSave: 'Войдите, чтобы сохранить',
    },
    en: {
      dialogLabel: (word: string) => `Meaning of “${word}”`,
      loading: 'Looking up the meaning...',
      loadError: 'Could not get the meaning',
      add: '➕ Add to vocabulary',
      added: 'Added to your vocabulary ✅ — it will also come up in Review.',
      loginToSave: 'Log in to save',
    },
  },
);
