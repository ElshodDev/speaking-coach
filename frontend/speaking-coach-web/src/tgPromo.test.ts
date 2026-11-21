import { describe, expect, it } from 'vitest';
import { PROMO_SNOOZE_DAYS, shouldShowPromo } from './tgPromo';

const DAY = 24 * 60 * 60 * 1000;
const NOW = Date.UTC(2026, 10, 21, 12);

describe('shouldShowPromo', () => {
  it('hech qachon yashirilmagan — ko‘rsatiladi', () => {
    expect(shouldShowPromo(null, NOW)).toBe(true);
  });
  it('yaqinda "Keyinroq" bosilgan — yashirin', () => {
    expect(shouldShowPromo(NOW - 1 * DAY, NOW)).toBe(false);
    expect(shouldShowPromo(NOW - (PROMO_SNOOZE_DAYS * DAY - 1), NOW)).toBe(false);
  });
  it('7 kundan keyin yana ko‘rsatiladi', () => {
    expect(shouldShowPromo(NOW - PROMO_SNOOZE_DAYS * DAY, NOW)).toBe(true);
    expect(shouldShowPromo(NOW - 30 * DAY, NOW)).toBe(true);
  });
  it('kelajakdagi sana (soat siljishi) taklifni abadiy yashirmaydi', () => {
    expect(shouldShowPromo(NOW + 3 * DAY, NOW)).toBe(true);
  });
});
