import { describe, expect, it, vi } from 'vitest';
import { applyTelegramUi, genuineTelegram, parseLaunch, syncTgBack } from './tgApp';

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

function fakeWebApp(version = '7.10') {
  const handlers = new Set<() => void>();
  const calls: string[] = [];
  const wa = {
    ready: () => calls.push('ready'),
    expand: () => calls.push('expand'),
    disableVerticalSwipes: () => calls.push('noSwipe'),
    isVersionAtLeast: (v: string) => {
      const [a, b] = version.split('.').map(Number);
      const [x, y] = v.split('.').map(Number);
      return a > x || (a === x && b >= y);
    },
    BackButton: {
      visible: false,
      show() {
        this.visible = true;
      },
      hide() {
        this.visible = false;
      },
      onClick: (cb: () => void) => handlers.add(cb),
      offClick: (cb: () => void) => handlers.delete(cb),
    },
  };
  const click = () => handlers.forEach((h) => h());
  return { wa, calls, handlers, click };
}

describe('Telegram UI setup', () => {
  it('disables vertical swipes on Bot API 7.7+', () => {
    const { wa, calls } = fakeWebApp('7.7');
    applyTelegramUi(wa);
    expect(calls).toEqual(['ready', 'expand', 'noSwipe']);
  });
  it('skips it on older clients', () => {
    const { wa, calls } = fakeWebApp('7.6');
    applyTelegramUi(wa);
    expect(calls).toEqual(['ready', 'expand']);
  });
});

describe('syncTgBack', () => {
  it('shows the button and calls the latest handler exactly once', () => {
    const { wa, handlers, click } = fakeWebApp();
    const first = vi.fn();
    const second = vi.fn();
    syncTgBack(true, first, wa);
    syncTgBack(true, second, wa);
    expect(wa.BackButton.visible).toBe(true);
    expect(handlers.size).toBe(1);
    click();
    expect(first).not.toHaveBeenCalled();
    expect(second).toHaveBeenCalledTimes(1);
  });
  it('hides the button and unbinds the handler', () => {
    const { wa, handlers, click } = fakeWebApp();
    const onBack = vi.fn();
    syncTgBack(true, onBack, wa);
    syncTgBack(false, onBack, wa);
    expect(wa.BackButton.visible).toBe(false);
    expect(handlers.size).toBe(0);
    click();
    expect(onBack).not.toHaveBeenCalled();
  });
  it('does nothing outside Telegram or on clients without BackButton', () => {
    expect(() => syncTgBack(true, () => undefined, undefined)).not.toThrow();
    const { wa } = fakeWebApp('6.0');
    syncTgBack(true, () => undefined, wa);
    expect(wa.BackButton.visible).toBe(false);
  });
});
