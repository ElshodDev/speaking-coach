import { msg } from './i18n';
import { cardsMsg } from './locales/cards';

type CardsText = (typeof cardsMsg)['uz'];

/** Mashqdan keyingi xabar: nechta yangi takrorlash kartasi paydo bo'lgani (joriy interfeys tilida). */
export function cardsMessage(newCards: number | undefined, t: CardsText = msg(cardsMsg)): string {
  if (!newCards || newCards <= 0) return t.done;
  return t.added(newCards);
}
