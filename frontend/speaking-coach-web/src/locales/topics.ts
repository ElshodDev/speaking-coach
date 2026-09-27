import { defineMessages } from '../i18n';

/** TopicPicker.tsx — Speaking/Writing mavzusini tanlash (tayyor bank yoki AI). */
export const topicsMsg = defineMessages(
  {
    speaking: {
      everyday: 'Kundalik',
      'ielts-part1': 'IELTS 1-qism',
      'ielts-part2': 'IELTS 2-qism',
      'ielts-part3': 'IELTS 3-qism',
      cefr: 'CEFR',
    } as Record<string, string>,
    writing: {
      paragraph: 'Xatboshi',
      'essay-opinion': 'Insho: fikr',
      'essay-discussion': 'Insho: muhokama',
      'letter-informal': 'Norasmiy xat',
      'letter-formal': 'Rasmiy xat',
    } as Record<string, string>,
    shouldSay: 'Aytib oʻting:',
    words: (min: number, max: number) => `Tavsiya: ${min}–${max} soʻz`,
    otherTopic: '🔀 Boshqa mavzu',
    ai: '✨ AI mavzu',
    aiBusy: '✨ …',
    fromAi: '✨ AI taklif qilgan mavzu',
    count: (n: number) => `${n} ta tayyor mavzu`,
  },
  {
    ru: {
      speaking: {
        everyday: 'Повседневное',
        'ielts-part1': 'IELTS, часть 1',
        'ielts-part2': 'IELTS, часть 2',
        'ielts-part3': 'IELTS, часть 3',
        cefr: 'CEFR',
      },
      writing: {
        paragraph: 'Абзац',
        'essay-opinion': 'Эссе: мнение',
        'essay-discussion': 'Эссе: обсуждение',
        'letter-informal': 'Неофиц. письмо',
        'letter-formal': 'Офиц. письмо',
      },
      shouldSay: 'Расскажите:',
      words: (min: number, max: number) => `Рекомендуется: ${min}–${max} слов`,
      otherTopic: '🔀 Другая тема',
      ai: '✨ Тема от ИИ',
      aiBusy: '✨ …',
      fromAi: '✨ Тему предложил ИИ',
      count: (n: number) => `Готовых тем: ${n}`,
    },
    en: {
      speaking: {
        everyday: 'Everyday',
        'ielts-part1': 'IELTS Part 1',
        'ielts-part2': 'IELTS Part 2',
        'ielts-part3': 'IELTS Part 3',
        cefr: 'CEFR',
      },
      writing: {
        paragraph: 'Paragraph',
        'essay-opinion': 'Essay: opinion',
        'essay-discussion': 'Essay: discussion',
        'letter-informal': 'Informal letter',
        'letter-formal': 'Formal letter',
      },
      shouldSay: 'You should say:',
      words: (min: number, max: number) => `Suggested: ${min}–${max} words`,
      otherTopic: '🔀 Another topic',
      ai: '✨ AI topic',
      aiBusy: '✨ …',
      fromAi: '✨ Topic suggested by AI',
      count: (n: number) => `${n} ready-made topics`,
    },
  },
);
