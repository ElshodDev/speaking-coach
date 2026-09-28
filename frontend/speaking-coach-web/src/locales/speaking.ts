import { defineMessages, enPlural, ruPlural } from '../i18n';

/** Recorder.tsx — Gapirish mashqi. Mavzularning oʻzi (SPEAKING_TOPICS) inglizcha qoladi. */
export const speakingMsg = defineMessages(
  {
    topic: 'Mavzu',
    otherTopic: '🔀 Boshqa mavzu',
    maxLength: (min: number) => `Eng koʻpi ${min} daqiqa.`,
    timeLeft: (s: number) => `Yana ${s} soniya — keyin yozuv avtomatik toʻxtaydi.`,
    autoStopped: (max: number) => `Vaqt tugadi (${Math.round(max / 60)} daqiqa) — yozuv toʻxtatildi.`,
    retry: '🔁 Qayta urinish',
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
      maxLength: (min: number) => `Не более ${min} ${ruPlural(min, 'минуты', 'минут', 'минут')}.`,
      timeLeft: (s: number) => `Осталось ${s} ${ruPlural(s, 'секунда', 'секунды', 'секунд')} — потом запись остановится автоматически.`,
      autoStopped: (max: number) => `Время вышло (${Math.round(max / 60)} мин) — запись остановлена.`,
      retry: '🔁 Повторить',
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
      maxLength: (min: number) => `Up to ${min} ${enPlural(min, 'minute', 'minutes')}.`,
      timeLeft: (s: number) => `${s} ${enPlural(s, 'second', 'seconds')} left — then the recording stops automatically.`,
      autoStopped: (max: number) => `Time is up (${Math.round(max / 60)} min) — recording stopped.`,
      retry: '🔁 Try again',
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
