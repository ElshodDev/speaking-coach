import { defineMessages, enPlural, ruPlural } from '../i18n';

/** Stability.tsx (barqarorlik jadvali) va Gapirish/Yozish ekranlaridagi barqarorlik testi. */
export const stabilityMsg = defineMessages(
  {
    checkButton: (runs: number) => `🔁 Barqarorlikni tekshirish (${runs}x)`,
    progress: (done: number, total: number, failed: number) =>
      `${done}/${total} bajarildi${failed ? ` (${failed} ta xato)` : ''}...`,
    allFailed: 'Birorta ham urinish muvaffaqiyatli boʻlmadi — keyinroq qayta urinib koʻring.',
    title: (n: number) => `Barqarorlik natijasi (${n} ta urinish)`,
    failedNote: (n: number) =>
      `${n} ta urinish xatolik bilan tugadi (masalan, Gemini band edi) — statistika faqat muvaffaqiyatli urinishlar boʻyicha.`,
    colCriterion: 'Mezon',
    colScores: 'Ballar',
    colAverage: 'Oʻrtacha',
    colRange: 'Farq',
    colVerdict: 'Baho',
    verdict: { stable: 'Barqaror', medium: 'Oʻrtacha', unstable: 'Beqaror' },
    notSavedBefore: 'Bu urinishlar tarixga saqlanmadi (',
    notSavedAfter: '). Farq ≤5 — barqaror, 6–15 — oʻrtacha, >15 — beqaror.',
  },
  {
    ru: {
      checkButton: (runs: number) => `🔁 Проверить стабильность (${runs}x)`,
      progress: (done: number, total: number, failed: number) =>
        `Выполнено ${done}/${total}${failed ? ` (${failed} ${ruPlural(failed, 'ошибка', 'ошибки', 'ошибок')})` : ''}...`,
      allFailed: 'Ни одна попытка не удалась — попробуйте ещё раз позже.',
      title: (n: number) => `Результат теста стабильности (${n} ${ruPlural(n, 'попытка', 'попытки', 'попыток')})`,
      failedNote: (n: number) =>
        `${n} ${ruPlural(n, 'попытка завершилась', 'попытки завершились', 'попыток завершились')} ошибкой (например, Gemini был перегружен) — статистика учитывает только успешные.`,
      colCriterion: 'Критерий',
      colScores: 'Баллы',
      colAverage: 'Среднее',
      colRange: 'Разброс',
      colVerdict: 'Оценка',
      verdict: { stable: 'Стабильно', medium: 'Средне', unstable: 'Нестабильно' },
      notSavedBefore: 'Эти попытки не сохранены в истории (',
      notSavedAfter: '). Разброс ≤5 — стабильно, 6–15 — средне, >15 — нестабильно.',
    },
    en: {
      checkButton: (runs: number) => `🔁 Check stability (${runs}x)`,
      progress: (done: number, total: number, failed: number) =>
        `${done}/${total} done${failed ? ` (${failed} ${enPlural(failed, 'error', 'errors')})` : ''}...`,
      allFailed: 'None of the attempts succeeded — please try again later.',
      title: (n: number) => `Stability result (${n} ${enPlural(n, 'attempt', 'attempts')})`,
      failedNote: (n: number) =>
        `${n} ${enPlural(n, 'attempt', 'attempts')} failed (e.g. Gemini was busy) — the statistics only include successful ones.`,
      colCriterion: 'Criterion',
      colScores: 'Scores',
      colAverage: 'Average',
      colRange: 'Range',
      colVerdict: 'Verdict',
      verdict: { stable: 'Stable', medium: 'Moderate', unstable: 'Unstable' },
      notSavedBefore: 'These attempts were not saved to your history (',
      notSavedAfter: '). Range ≤5 — stable, 6–15 — moderate, >15 — unstable.',
    },
  },
);
