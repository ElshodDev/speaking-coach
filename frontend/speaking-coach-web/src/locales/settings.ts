import { defineMessages } from '../i18n';

type LevelId = 'A2' | 'B1' | 'B2' | 'C1';

/** Settings.tsx — interfeys tili, ingliz tili darajasi, taxallus, musobaqa. */
export const settingsMsg = defineMessages(
  {
    title: 'Sozlamalar',
    interfaceLanguage: 'Interfeys tili',
    levelLabel: 'Ingliz tili darajangiz',
    levelGroup: 'Daraja',
    levelHints: {
      A2: 'Boshlangʻich',
      B1: 'Oʻrta',
      B2: 'Oʻrtadan yuqori',
      C1: 'Yuqori',
    } as Record<LevelId, string>,
    levelNote: 'Matnlar uzunligi, savollar va izohlar shunga moslashadi.',
    levelPicked: (id: string) => `Daraja: ${id} ✅`,
    nickname: 'Taxallus (musobaqada koʻrinadi)',
    nicknamePlaceholder: 'masalan: Aziza_B2',
    leagueOptIn: 'Haftalik musobaqada qatnashaman',
    privacy: 'Reytingda faqat taxallus va haftalik XP koʻrinadi — email va mashqlaringiz hech kimga koʻrsatilmaydi.',
    saved: 'Saqlandi ✅',
  },
  {
    ru: {
      title: 'Настройки',
      interfaceLanguage: 'Язык интерфейса',
      levelLabel: 'Ваш уровень английского',
      levelGroup: 'Уровень',
      levelHints: {
        A2: 'Начальный',
        B1: 'Средний',
        B2: 'Выше среднего',
        C1: 'Продвинутый',
      },
      levelNote: 'Под него подстраиваются длина текстов, вопросы и пояснения.',
      levelPicked: (id: string) => `Уровень: ${id} ✅`,
      nickname: 'Никнейм (виден в соревновании)',
      nicknamePlaceholder: 'например: Aziza_B2',
      leagueOptIn: 'Участвовать в недельном соревновании',
      privacy: 'В рейтинге видны только никнейм и XP за неделю — ваш email и упражнения никому не показываются.',
      saved: 'Сохранено ✅',
    },
    en: {
      title: 'Settings',
      interfaceLanguage: 'Interface language',
      levelLabel: 'Your English level',
      levelGroup: 'Level',
      levelHints: {
        A2: 'Elementary',
        B1: 'Intermediate',
        B2: 'Upper-intermediate',
        C1: 'Advanced',
      },
      levelNote: 'Text length, questions and explanations adapt to it.',
      levelPicked: (id: string) => `Level: ${id} ✅`,
      nickname: 'Nickname (shown in the league)',
      nicknamePlaceholder: 'e.g. Aziza_B2',
      leagueOptIn: 'Take part in the weekly league',
      privacy: 'The leaderboard shows only your nickname and weekly XP — your email and exercises are never shown to anyone.',
      saved: 'Saved ✅',
    },
  },
);
