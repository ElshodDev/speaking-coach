import { defineMessages, enPlural, ruPlural } from '../i18n';

/** ReferralCard.tsx — do'stni taklif qilish: havola, ulashish, mukofot. */
export const referralMsg = defineMessages(
  {
    title: 'Doʻstingizni taklif qiling',
    text: (days: number, extra: number) =>
      `Doʻstingiz sizning havolangiz orqali roʻyxatdan oʻtsa, ikkingizga ham ${days} kun davomida kuniga +${extra} ta qoʻshimcha AI mashq beriladi.`,
    linkLabel: 'Taklif havolangiz',
    copy: 'Nusxa olish',
    copied: 'Nusxa olindi',
    share: 'Ulashish',
    shareText: 'FluentUZ — AI bilan ingliz tilini oʻrganamiz. Mana mening taklif havolam:',
    invited: (n: number) => `Qoʻshilgan doʻstlar: ${n}`,
    bonusOn: (extra: number) => `Mukofot faol: kuniga +${extra} ta qoʻshimcha AI mashq`,
    note: 'Doʻst emaili tasdiqlangandan keyin hisoblanadi. Mukofotlar qoʻshilmaydi.',
  },
  {
    ru: {
      title: 'Пригласите друга',
      text: (days: number, extra: number) =>
        `Если друг зарегистрируется по вашей ссылке, вы оба получите +${extra} дополнительных ИИ-упражнений в день на ${days} ${ruPlural(days, 'день', 'дня', 'дней')}.`,
      linkLabel: 'Ваша пригласительная ссылка',
      copy: 'Скопировать',
      copied: 'Скопировано',
      share: 'Поделиться',
      shareText: 'FluentUZ — учим английский с ИИ. Вот моя пригласительная ссылка:',
      invited: (n: number) => `Присоединилось друзей: ${n}`,
      bonusOn: (extra: number) => `Бонус активен: +${extra} дополнительных ИИ-упражнений в день`,
      note: 'Засчитывается после подтверждения email друга. Бонусы не суммируются.',
    },
    en: {
      title: 'Invite a friend',
      text: (days: number, extra: number) =>
        `When a friend signs up with your link, you both get +${extra} extra AI exercises a day for ${days} ${enPlural(days, 'day', 'days')}.`,
      linkLabel: 'Your invite link',
      copy: 'Copy',
      copied: 'Copied',
      share: 'Share',
      shareText: 'FluentUZ — learn English with AI. Here is my invite link:',
      invited: (n: number) => `Friends joined: ${n}`,
      bonusOn: (extra: number) => `Bonus active: +${extra} extra AI exercises a day`,
      note: "Counted once your friend's email is verified. Bonuses don't stack.",
    },
  },
);
