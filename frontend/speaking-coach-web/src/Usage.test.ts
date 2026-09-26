import { describe, expect, it } from 'vitest';
import { accountMsg } from './locales/account';
import { usageText, type Usage } from './Usage';

const u = (left: number, limit: number, guest = false, unlimited = false): Usage => ({
  exercises: { used: limit - left, limit, left },
  words: { used: 0, limit: 100, left: 100 },
  guest,
  unlimited,
});

describe('usageText', () => {
  it('shows what is left for a user', () => {
    expect(usageText(u(27, 30), 30, accountMsg.uz)).toBe('Bugun yana 27 ta mashq bajarishingiz mumkin (kuniga 30 ta).');
  });

  it('nudges guests to create an account', () => {
    expect(usageText(u(3, 5, true), 30, accountMsg.en)).toBe('Guest: 3 more exercises today (5 a day). A free account gets 30 a day.');
  });

  it('uses Russian plurals', () => {
    expect(usageText(u(1, 30), 30, accountMsg.ru)).toContain('ещё 1 упражнение');
    expect(usageText(u(22, 30), 30, accountMsg.ru)).toContain('ещё 22 упражнения');
    expect(usageText(u(25, 30), 30, accountMsg.ru)).toContain('ещё 25 упражнений');
  });

  it('explains what still works when the limit is reached', () => {
    expect(usageText(u(0, 30), 30, accountMsg.uz)).toContain('Takrorlash va lugʻat cheklanmagan');
  });

  it('shows nothing for unlimited (admin) accounts', () => {
    expect(usageText(u(30, 30, false, true), 30, accountMsg.uz)).toBeNull();
  });
});
