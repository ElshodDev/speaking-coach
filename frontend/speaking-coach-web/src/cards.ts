/** Mashqdan keyingi xabar: nechta yangi takrorlash kartasi paydo bo'lgani. */
export function cardsMessage(newCards: number | undefined): string {
  if (!newCards || newCards <= 0) return 'Tayyor';
  return `Tayyor — ${newCards} ta yangi takrorlash kartasi qo'shildi (🔁 Takrorlash bo'limida).`;
}
