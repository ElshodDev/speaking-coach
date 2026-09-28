import { defineConfig, type Plugin } from 'vite';
import react from '@vitejs/plugin-react';

/** Havola oldindan ko'rinishi (Open Graph) uchun saytning to'liq manzili. */
const DEFAULT_SITE_URL = 'https://speaking-coach-theta.vercel.app';

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

export default defineConfig({
  plugins: [react(), siteUrl()],
  server: {
    port: 5173,
  },
});
