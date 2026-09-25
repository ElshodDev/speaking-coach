import { defineMessages } from '../i18n';

/** Recorder.tsx — Gapirish mashqi. Mavzularning oʻzi (SPEAKING_TOPICS) inglizcha qoladi. */
export const speakingMsg = defineMessages(
  {
    topic: 'Mavzu',
    otherTopic: '🔀 Boshqa mavzu',
    micDenied: 'Mikrofondan foydalanishga ruxsat berilmadi. Brauzer manzil satridagi 🔒 belgisini bosib, mikrofonga ruxsat bering.',
    evaluating: 'Tinglanmoqda va baholanmoqda... (5–15 soniya)',
    uploadFailed: 'Yozuvni yuborib boʻlmadi',
    stop: 'Toʻxtatish',
    start: '🎙 Yozishni boshlash',
    tip: 'Maslahat: 15–30 soniya gapiring. Xato qilishdan qoʻrqmang — aynan ular takrorlash kartalariga aylanadi.',
    youSaid: 'Siz aytdingiz',
    evaluation: 'Baholash',
    criteria: { fluency: 'Ravonlik', grammar: 'Grammatika', vocabulary: 'Lugʻat boyligi' },
    short: { fluency: 'Ravonlik', grammar: 'Grammatika', vocabulary: 'Lugʻat' },
    stabilityHint: (runs: number) =>
      `Aynan shu yozuv yana ${runs} marta baholanadi — sunʼiy intellekt bahosi qanchalik oʻzgarishini koʻrasiz.`,
  },
  {
    ru: {
      topic: 'Тема',
      otherTopic: '🔀 Другая тема',
      micDenied: 'Нет доступа к микрофону. Нажмите на значок 🔒 в адресной строке браузера и разрешите доступ к микрофону.',
      evaluating: 'Слушаем и оцениваем... (5–15 секунд)',
      uploadFailed: 'Не удалось отправить запись',
      stop: 'Остановить',
      start: '🎙 Начать запись',
      tip: 'Совет: говорите 15–30 секунд. Не бойтесь ошибаться — именно ошибки превращаются в карточки для повторения.',
      youSaid: 'Вы сказали',
      evaluation: 'Оценка',
      criteria: { fluency: 'Беглость', grammar: 'Грамматика', vocabulary: 'Словарный запас' },
      short: { fluency: 'Беглость', grammar: 'Грамматика', vocabulary: 'Словарь' },
      stabilityHint: (runs: number) =>
        `Эта же запись будет оценена ещё ${runs} раз — вы увидите, насколько меняется оценка ИИ.`,
    },
    en: {
      topic: 'Topic',
      otherTopic: '🔀 Another topic',
      micDenied: 'Microphone access was denied. Click the 🔒 icon in the browser address bar and allow the microphone.',
      evaluating: 'Listening and grading... (5–15 seconds)',
      uploadFailed: 'Could not upload the recording',
      stop: 'Stop',
      start: '🎙 Start recording',
      tip: 'Tip: speak for 15–30 seconds. Don’t be afraid of mistakes — they are exactly what becomes your review cards.',
      youSaid: 'You said',
      evaluation: 'Evaluation',
      criteria: { fluency: 'Fluency', grammar: 'Grammar', vocabulary: 'Vocabulary' },
      short: { fluency: 'Fluency', grammar: 'Grammar', vocabulary: 'Vocabulary' },
      stabilityHint: (runs: number) =>
        `The same recording is graded ${runs} more times so you can see how much the AI’s score varies.`,
    },
  },
);
