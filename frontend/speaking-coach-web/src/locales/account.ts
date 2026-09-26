import { defineMessages, enPlural, ruPlural } from '../i18n';

/** Usage.tsx va AccountData.tsx — kunlik AI limiti, maʼlumotlarni yuklab olish va hisobni oʻchirish. */
export const accountMsg = defineMessages(
  {
    usageUser: (left: number, limit: number) => `Bugun yana ${left} ta mashq bajarishingiz mumkin (kuniga ${limit} ta).`,
    usageGuest: (left: number, limit: number, userLimit: number) =>
      `Mehmon: bugun yana ${left} ta mashq (kuniga ${limit} ta). Bepul hisob ochsangiz — kuniga ${userLimit} ta.`,
    usageEmpty: 'Bugungi mashqlar limiti tugadi. Takrorlash va lugʻat cheklanmagan — ertaga yana davom etasiz.',
    dataTitle: 'Maʼlumotlaringiz',
    dataText: 'Mashqlar, baholar, kartalar va lugʻatingizning toʻliq nusxasini yuklab olishingiz yoki hisobni butunlay oʻchirishingiz mumkin.',
    download: '⬇️ Maʼlumotlarni yuklab olish (JSON)',
    downloading: 'Tayyorlanmoqda...',
    deleteOpen: 'Hisobni oʻchirish',
    deleteWarn:
      'Hisob va unga tegishli hamma narsa — mashqlar, baholar, kartalar, lugʻat, natijalar — butunlay oʻchiriladi. Buni qaytarib boʻlmaydi.',
    deleteConfirmLabel: (email: string) => `Tasdiqlash uchun emailingizni kiriting: ${email}`,
    deleteButton: 'Butunlay oʻchirish',
    cancel: 'Bekor qilish',
    deleted: 'Hisobingiz oʻchirildi.',
  },
  {
    ru: {
      usageUser: (left: number, limit: number) =>
        `Сегодня можно выполнить ещё ${left} ${ruPlural(left, 'упражнение', 'упражнения', 'упражнений')} (${limit} в день).`,
      usageGuest: (left: number, limit: number, userLimit: number) =>
        `Гость: сегодня ещё ${left} ${ruPlural(left, 'упражнение', 'упражнения', 'упражнений')} (${limit} в день). С бесплатным аккаунтом — ${userLimit} в день.`,
      usageEmpty: 'Лимит упражнений на сегодня исчерпан. Повторение и словарь без ограничений — продолжите завтра.',
      dataTitle: 'Ваши данные',
      dataText: 'Вы можете скачать полную копию упражнений, оценок, карточек и словаря или полностью удалить аккаунт.',
      download: '⬇️ Скачать данные (JSON)',
      downloading: 'Подготовка...',
      deleteOpen: 'Удалить аккаунт',
      deleteWarn:
        'Аккаунт и всё, что с ним связано, — упражнения, оценки, карточки, словарь, результаты — будут удалены навсегда. Это нельзя отменить.',
      deleteConfirmLabel: (email: string) => `Для подтверждения введите ваш email: ${email}`,
      deleteButton: 'Удалить навсегда',
      cancel: 'Отмена',
      deleted: 'Ваш аккаунт удалён.',
    },
    en: {
      usageUser: (left: number, limit: number) =>
        `You can do ${left} more ${enPlural(left, 'exercise', 'exercises')} today (${limit} a day).`,
      usageGuest: (left: number, limit: number, userLimit: number) =>
        `Guest: ${left} more ${enPlural(left, 'exercise', 'exercises')} today (${limit} a day). A free account gets ${userLimit} a day.`,
      usageEmpty: "You've used today's exercises. Review and vocabulary are unlimited — see you tomorrow.",
      dataTitle: 'Your data',
      dataText: 'Download a full copy of your exercises, scores, cards and vocabulary, or delete your account for good.',
      download: '⬇️ Download my data (JSON)',
      downloading: 'Preparing...',
      deleteOpen: 'Delete account',
      deleteWarn:
        'Your account and everything in it — exercises, scores, cards, vocabulary, progress — will be permanently deleted. This cannot be undone.',
      deleteConfirmLabel: (email: string) => `To confirm, type your email: ${email}`,
      deleteButton: 'Delete forever',
      cancel: 'Cancel',
      deleted: 'Your account has been deleted.',
    },
  },
);
