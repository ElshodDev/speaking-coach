import { describe, expect, it } from 'vitest';
import { chromeIntent, inAppBrowser, isAndroid, isIos } from './inApp';

const CHROME_ANDROID = 'Mozilla/5.0 (Linux; Android 14; SM-A546B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Mobile Safari/537.36';
const SAFARI_IOS = 'Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1';

describe('inAppBrowser', () => {
  it('ordinary browsers are not in-app', () => {
    expect(inAppBrowser(CHROME_ANDROID)).toBeNull();
    expect(inAppBrowser(SAFARI_IOS)).toBeNull();
  });

  it('detects the common in-app browsers', () => {
    expect(inAppBrowser(SAFARI_IOS.replace('Safari/604.1', 'Instagram 330.0.0.0 (iPhone14,5)'))).toBe('Instagram');
    expect(inAppBrowser(CHROME_ANDROID + ' [FB_IAB/FB4A;FBAV/470.0.0.0;]')).toBe('Facebook');
    expect(inAppBrowser(CHROME_ANDROID + ' musical_ly_2023')).toBe('TikTok');
    expect(inAppBrowser(CHROME_ANDROID.replace('Android 14; SM-A546B)', 'Android 14; SM-A546B; wv)'))).toBe('WebView');
  });

  it('Telegram is detected by its webview proxy even with a plain user agent', () => {
    expect(inAppBrowser(SAFARI_IOS, true)).toBe('Telegram');
  });

  it('platform helpers and the Chrome intent link', () => {
    expect(isAndroid(CHROME_ANDROID)).toBe(true);
    expect(isIos(SAFARI_IOS)).toBe(true);
    expect(chromeIntent({ host: 'speaking-coach-theta.vercel.app', pathname: '/' })).toBe(
      'intent://speaking-coach-theta.vercel.app/#Intent;scheme=https;package=com.android.chrome;end',
    );
  });
});
