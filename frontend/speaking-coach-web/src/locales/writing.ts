import { defineMessages, enPlural, ruPlural } from '../i18n';

/** WritingCoach.tsx — Yozish mashqi. Mavzularning oʻzi (WRITING_TOPICS) inglizcha qoladi. */
export const writingMsg = defineMessages(
  {
    topic: 'Mavzu',
    otherTopic: '🔀 Boshqa mavzu',
    tooShort: (min: number) => `Matn juda qisqa (kamida ${min} ta belgi kerak).`,
    evaluating: 'Matn oʻqilmoqda va baholanmoqda... (5–15 soniya)',
    sendFailed: 'Matnni yuborib boʻlmadi',
    placeholder: 'Shu mavzuda ingliz tilida yozing... (tavsiya: 80–200 soʻz)',
    essayAria: 'Insho matni',
    words: (n: number) => `${n} ta soʻz`,
    chars: (n: number, max: number) => `${n}/${max} belgi`,
    submit: '✍️ Yuborish',
    evaluation: 'Baholash',
    criteria: {
      taskAchievement: 'Vazifani bajarish',
      coherence: 'Mantiqiy bogʻlanish',
      grammar: 'Grammatika',
      vocabulary: 'Lugʻat boyligi',
    },
    short: { taskAchievement: 'Vazifa', coherence: 'Bogʻlanish', grammar: 'Grammatika', vocabulary: 'Lugʻat' },
    stabilityHint: (runs: number) =>
      `Aynan shu matn yana ${runs} marta baholanadi — sunʼiy intellekt bahosi qanchalik oʻzgarishini koʻrasiz.`,
  },
  {
    ru: {
      topic: 'Тема',
      otherTopic: '🔀 Другая тема',
      tooShort: (min: number) => `Текст слишком короткий (нужно не менее ${min} ${ruPlural(min, 'символа', 'символов', 'символов')}).`,
      evaluating: 'Читаем и оцениваем текст... (5–15 секунд)',
      sendFailed: 'Не удалось отправить текст',
      placeholder: 'Напишите на эту тему по-английски... (рекомендуем 80–200 слов)',
      essayAria: 'Текст эссе',
      words: (n: number) => `${n} ${ruPlural(n, 'слово', 'слова', 'слов')}`,
      chars: (n: number, max: number) => `${n}/${max} ${ruPlural(max, 'символ', 'символа', 'символов')}`,
      submit: '✍️ Отправить',
      evaluation: 'Оценка',
      criteria: {
        taskAchievement: 'Выполнение задания',
        coherence: 'Связность и логичность',
        grammar: 'Грамматика',
        vocabulary: 'Словарный запас',
      },
      short: { taskAchievement: 'Задание', coherence: 'Связность', grammar: 'Грамматика', vocabulary: 'Словарь' },
      stabilityHint: (runs: number) =>
        `Этот же текст будет оценён ещё ${runs} раз — вы увидите, насколько меняется оценка ИИ.`,
    },
    en: {
      topic: 'Topic',
      otherTopic: '🔀 Another topic',
      tooShort: (min: number) => `The text is too short (at least ${min} characters needed).`,
      evaluating: 'Reading and grading... (5–15 seconds)',
      sendFailed: 'Could not send the text',
      placeholder: 'Write in English on this topic... (recommended: 80–200 words)',
      essayAria: 'Essay text',
      words: (n: number) => `${n} ${enPlural(n, 'word', 'words')}`,
      chars: (n: number, max: number) => `${n}/${max} characters`,
      submit: '✍️ Submit',
      evaluation: 'Evaluation',
      criteria: {
        taskAchievement: 'Task achievement',
        coherence: 'Coherence & cohesion',
        grammar: 'Grammar',
        vocabulary: 'Vocabulary',
      },
      short: { taskAchievement: 'Task', coherence: 'Coherence', grammar: 'Grammar', vocabulary: 'Vocabulary' },
      stabilityHint: (runs: number) =>
        `The same text is graded ${runs} more times so you can see how much the AI’s score varies.`,
    },
  },
);
