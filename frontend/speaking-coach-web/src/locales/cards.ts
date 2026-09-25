import { defineMessages, enPlural, ruPlural } from '../i18n';

/** cards.ts — mashqdan keyingi “yangi kartalar” xabari. */
export const cardsMsg = defineMessages(
  {
    done: 'Tayyor',
    added: (n: number) => `Tayyor — ${n} ta yangi takrorlash kartasi qoʻshildi (🔁 Takrorlash boʻlimida).`,
  },
  {
    ru: {
      done: 'Готово',
      added: (n: number) =>
        `Готово — ${ruPlural(n, 'добавлена', 'добавлены', 'добавлено')} ${n} ${ruPlural(n, 'новая карточка', 'новые карточки', 'новых карточек')} для повторения (раздел «🔁 Повторение»).`,
    },
    en: {
      done: 'Done',
      added: (n: number) => `Done — ${n} new review ${enPlural(n, 'card', 'cards')} added (see 🔁 Review).`,
    },
  },
);
