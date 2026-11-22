import { describe, expect, it } from 'vitest';
import { logoSrc, MINI_MAX } from './Logo';
import swSource from '../public/sw.js?raw';
import manifestRaw from '../public/manifest.webmanifest?raw';
import indexHtml from '../index.html?raw';

// public/icons dagi PNG'lar (data URL) va SVG'lar (matn) — faqat testda o'qiladi, to'plamga kirmaydi.
const pngs = import.meta.glob('../public/icons/*.png', { query: '?inline', import: 'default', eager: true }) as Record<string, string>;
const svgs = import.meta.glob('../public/icons/*.svg', { query: '?raw', import: 'default', eager: true }) as Record<string, string>;
const iconPath = (src: string) => `..${'/public'}${src}`;

/** PNG sarlavhasidan (IHDR) eni va bo'yini o'qiydi. */
function pngSize(src: string): { w: number; h: number } {
  const url = pngs[iconPath(src)];
  expect(url, src).toBeTruthy();
  const bytes = Uint8Array.from(atob(url.slice(url.indexOf(',') + 1, url.indexOf(',') + 1 + 48)), (c) => c.charCodeAt(0));
  const dv = new DataView(bytes.buffer);
  expect(String.fromCharCode(bytes[1], bytes[2], bytes[3])).toBe('PNG');
  return { w: dv.getUint32(16), h: dv.getUint32(20) };
}

describe('logotip', () => {
  it("kichik o'lchamda soddalashtirilgan versiya, kattada to'liq", () => {
    expect(logoSrc(16)).toBe('/icons/logo-mini.svg');
    expect(logoSrc(MINI_MAX)).toBe('/icons/logo-mini.svg');
    expect(logoSrc(MINI_MAX + 1)).toBe('/icons/logo.svg');
  });

  it("manifestdagi har bir ikonka mavjud va e'lon qilingan o'lchamda", () => {
    const manifest = JSON.parse(manifestRaw) as { icons: { src: string; sizes: string }[] };
    expect(manifest.icons.length).toBeGreaterThanOrEqual(3);
    for (const icon of manifest.icons) {
      const [w, h] = icon.sizes.split('x').map(Number);
      expect(pngSize(icon.src), icon.src).toEqual({ w, h });
    }
  });

  it('index.html havola qiladigan ikonkalar bor; xizmat ishchisi logotiplarni keshlaydi', () => {
    const refs = [...indexHtml.matchAll(/href="(\/icons\/[^"]+)"/g)].map((m) => m[1]);
    expect(refs.length).toBeGreaterThan(0);
    for (const r of refs) expect(pngs[iconPath(r)] ?? svgs[iconPath(r)], r).toBeTruthy();
    for (const f of ['/icons/logo.svg', '/icons/logo-mini.svg']) {
      expect(svgs[iconPath(f)], f).toContain('<svg');
      expect(swSource, f).toContain(f);
    }
    expect(pngSize('/icons/apple-touch-icon.png')).toEqual({ w: 180, h: 180 });
    expect(pngSize('/icons/favicon-32.png')).toEqual({ w: 32, h: 32 });
  });
});
