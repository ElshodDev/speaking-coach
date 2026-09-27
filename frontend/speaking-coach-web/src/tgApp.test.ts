import { describe, expect, it } from 'vitest';
import { genuineTelegram, parseLaunch } from './tgApp';

describe('Telegram Mini App launch', () => {
  const data = encodeURIComponent('query_id=AAH&user=%7B%22id%22%3A7%7D&auth_date=1&hash=abc');
  it('reads signed init data and the requested section', () => {
    const l = parseLaunch(`#tgWebAppData=${data}&tgWebAppVersion=7.0&tgWebAppPlatform=ios`, '?tgroute=mock%2Fcefr-reading');
    expect(l).toEqual({ initData: 'query_id=AAH&user=%7B%22id%22%3A7%7D&auth_date=1&hash=abc', route: 'mock/cefr-reading' });
  });
  it('is null outside Telegram', () => {
    expect(parseLaunch('#/shadowing', '')).toBeNull();
    expect(parseLaunch('', '?tgroute=shadowing')).toBeNull();
  });
  it('opens the home page for missing or unsafe routes', () => {
    expect(parseLaunch(`#tgWebAppData=${data}`, '')!.route).toBe('');
    expect(parseLaunch(`#tgWebAppData=${data}`, '?tgroute=https%3A%2F%2Fevil.example')!.route).toBe('');
    expect(parseLaunch(`#tgWebAppData=${data}`, '?tgroute=..%2F..%2Fadmin')!.route).toBe('');
  });
  it('handles empty init data', () => {
    expect(parseLaunch('#tgWebAppData=&tgWebAppVersion=7.0', '')!.initData).toBeNull();
  });
});

describe('genuine Telegram check (login CSRF)', () => {
  it('trusts the native client bridge or an iframe, not a plain tab', () => {
    const top = {} as Window;
    const plain = { parent: top } as unknown as Window;
    Object.assign(plain, { parent: plain });
    expect(genuineTelegram(plain)).toBe(false);
    expect(genuineTelegram({ parent: plain, TelegramWebviewProxy: { postEvent() {} } } as unknown as Window)).toBe(true);
    const framed = { parent: top } as unknown as Window; // web.telegram.org iframe ichida
    expect(genuineTelegram(framed)).toBe(true);
  });
});
