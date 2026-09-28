import { describe, expect, it } from 'vitest';
import { accountLabel, isSyntheticEmail } from './api';
import { nextState } from './TelegramLogin';

describe('Telegram hisobi', () => {
  it('ichki emailni taniydi', () => {
    expect(isSyntheticEmail('tg-12345@telegram.invalid')).toBe(true);
    expect(isSyntheticEmail('TG-1@Telegram.Invalid')).toBe(true);
    expect(isSyntheticEmail('ali@gmail.com')).toBe(false);
    expect(isSyntheticEmail(null)).toBe(false);
  });

  it('ichki email oʻrniga taxallus yoki "Telegram" koʻrsatiladi', () => {
    expect(accountLabel('tg-1@telegram.invalid', 'Ali')).toBe('Ali');
    expect(accountLabel('tg-1@telegram.invalid', null)).toBe('Telegram');
    expect(accountLabel('ali@gmail.com', 'Ali')).toBe('ali@gmail.com');
    expect(accountLabel(null)).toBe('');
  });
});

describe('Telegram orqali kirish holati', () => {
  const until = 1_000_000;
  it('tasdiqlansa — tugaydi', () => expect(nextState('confirmed', 0, until)).toBe('done'));
  it('rad etilsa — bekor', () => expect(nextState('rejected', 0, until)).toBe('rejected'));
  it('server eskirgan desa yoki vaqt oʻtsa — expired', () => {
    expect(nextState('expired', 0, until)).toBe('expired');
    expect(nextState('pending', until, until)).toBe('expired');
  });
  it('kutilmoqda — davom etadi', () => expect(nextState('pending', 5, until)).toBe('wait'));
});
