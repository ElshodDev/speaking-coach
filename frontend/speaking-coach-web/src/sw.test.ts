import { describe, expect, it } from 'vitest';
import swSource from '../public/sw.js?raw';

// sw.js — oddiy (modul bo'lmagan) skript: soxta `self` bilan ishga tushirib, toza funksiyalarini olamiz.
interface SwLogic {
  referencedAssets: (text: string) => Set<string>;
  staleAssets: (cached: string[], shells: (string | null)[], read: (path: string) => Promise<string | null>) => Promise<string[]>;
  cacheable: (request: Request, url: URL, origin: string) => boolean;
}
function loadSw(): SwLogic {
  const fakeSelf: Record<string, unknown> = { addEventListener: () => undefined, location: { origin: 'https://site.app' } };
  new Function('self', swSource)(fakeSelf);
  return fakeSelf.swLogic as SwLogic;
}
const sw = loadSw();

describe('service worker asset pruning', () => {
  const html = '<script type="module" src="/assets/index-AAA.js"></script><link rel="stylesheet" href="/assets/index-CSS.css">';
  const files: Record<string, string> = {
    '/assets/index-AAA.js': 'const m=()=>import(`./Mock-BBB.js`);const d=["assets/Mock-BBB.js","assets/mockLogic-CCC.js"]',
    '/assets/Mock-BBB.js': 'import{x}from"./index-AAA.js";import{y}from"./shared-DDD.js"',
    '/assets/index-CSS.css': '@font-face{src:url(/assets/inter-EEE.woff2)}',
  };
  const read = async (p: string) => files[p] ?? null;

  it('finds asset references in html, js and css', () => {
    expect([...sw.referencedAssets(html)]).toEqual(['/assets/index-AAA.js', '/assets/index-CSS.css']);
    expect([...sw.referencedAssets(files['/assets/Mock-BBB.js'])]).toEqual(['/assets/index-AAA.js', '/assets/shared-DDD.js']);
    expect([...sw.referencedAssets(files['/assets/index-CSS.css'])]).toEqual(['/assets/inter-EEE.woff2']);
  });

  it('keeps everything reachable from the current shell and drops the rest', async () => {
    const cached = [
      '/',
      '/icons/logo.svg',
      '/assets/index-AAA.js',
      '/assets/Mock-BBB.js',
      '/assets/mockLogic-CCC.js',
      '/assets/shared-DDD.js',
      '/assets/inter-EEE.woff2',
      '/assets/index-CSS.css',
      '/assets/index-OLD.js',
      '/assets/Mock-OLD.js',
    ];
    expect(await sw.staleAssets(cached, [html], read)).toEqual(['/assets/index-OLD.js', '/assets/Mock-OLD.js']);
  });

  it('keeps the previous build too while an old tab may still use it', async () => {
    const oldHtml = '<script src="/assets/index-OLD.js"></script>';
    const cached = ['/assets/index-AAA.js', '/assets/index-OLD.js', '/assets/index-ANCIENT.js'];
    expect(await sw.staleAssets(cached, [html, oldHtml], read)).toEqual(['/assets/index-ANCIENT.js']);
  });

  it('never caches the API, other origins or requests with a token', () => {
    const origin = 'https://site.app';
    const req = (url: string, init?: RequestInit) => [new Request(url, init), new URL(url), origin] as const;
    expect(sw.cacheable(...req('https://site.app/assets/a.js'))).toBe(true);
    expect(sw.cacheable(...req('https://site.app/api/profile'))).toBe(false);
    expect(sw.cacheable(...req('https://api.onrender.com/api/profile'))).toBe(false);
    expect(sw.cacheable(...req('https://site.app/icons/x.png', { headers: { Authorization: 'Bearer t' } }))).toBe(false);
    expect(sw.cacheable(...req('https://site.app/assets/a.js', { method: 'POST' }))).toBe(false);
  });
});
