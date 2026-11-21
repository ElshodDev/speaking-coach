import { readFileSync } from 'node:fs';
import { defineConfig, loadEnv, type Plugin } from 'vite';
import react from '@vitejs/plugin-react';
import { connectAllowed } from './src/csp';
import { buildRobots, buildSitemap } from './src/seo';

/** Havola oldindan ko'rinishi (Open Graph) uchun saytning to'liq manzili. */
const DEFAULT_SITE_URL = 'https://fluentuz.app';

/**
 * index.html dagi __SITE_URL__ ni haqiqiy manzilga almashtiradi: og:image va
 * og:url faqat to'liq (absolute) manzil bo'lsa Telegram/Facebook'da ishlaydi.
 * Manzil VITE_SITE_URL (.env yoki Vercel environment) dan olinadi.
 */
function siteUrl(): Plugin {
  let url = DEFAULT_SITE_URL;
  return {
    name: 'site-url',
    configResolved(config) {
      url = (config.env.VITE_SITE_URL || process.env.VITE_SITE_URL || DEFAULT_SITE_URL).replace(/\/+$/, '');
    },
    transformIndexHtml(html) {
      return html.replaceAll('__SITE_URL__', url);
    },
  };
}

/** Build paytida robots.txt va sitemap.xml yozadi (manzil VITE_SITE_URL'dan — domen o'zgarsa qo'lda tahrirlash shart emas). */
function seoFiles(): Plugin {
  let url = DEFAULT_SITE_URL;
  return {
    name: 'seo-files',
    apply: 'build',
    configResolved(config) {
      url = (config.env.VITE_SITE_URL || process.env.VITE_SITE_URL || DEFAULT_SITE_URL).replace(/\/+$/, '');
    },
    generateBundle() {
      this.emitFile({ type: 'asset', fileName: 'robots.txt', source: buildRobots(url) });
      this.emitFile({ type: 'asset', fileName: 'sitemap.xml', source: buildSitemap(url, new Date().toISOString().slice(0, 10)) });
    },
  };
}

/**
 * Build paytida: vercel.json'dagi Content-Security-Policy VITE_API_URL'ga ruxsat
 * beradimi. Bermasa — build to'xtaydi (aks holda deploy bo'lgan sayt serverga
 * ulana olmaydi). Lokal manzil (localhost) tekshirilmaydi — u faqat lokal ishlash uchun.
 */
function cspGuard(): Plugin {
  return {
    name: 'csp-guard',
    apply: 'build',
    configResolved(config) {
      const env = loadEnv(config.mode, config.envDir || process.cwd(), 'VITE_');
      const api = env.VITE_API_URL || process.env.VITE_API_URL;
      if (!api || /^https?:\/\/(localhost|127\.0\.0\.1)(:|\/|$)/.test(api)) return;
      const vercel = JSON.parse(readFileSync(new URL('./vercel.json', import.meta.url), 'utf8')) as {
        headers: { headers: { key: string; value: string }[] }[];
      };
      const policies = vercel.headers.flatMap((h) => h.headers).filter((h) => h.key === 'Content-Security-Policy');
      const bad = policies.find((p) => !connectAllowed(p.value, api));
      if (bad) {
        throw new Error(`vercel.json Content-Security-Policy connect-src ${api} ga ruxsat bermaydi — uni connect-src'ga qo'shing.`);
      }
    },
  };
}

export default defineConfig({
  plugins: [react(), siteUrl(), seoFiles(), cspGuard()],
  server: {
    port: 5173,
  },
});
