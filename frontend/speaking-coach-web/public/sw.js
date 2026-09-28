// Service worker — ilovani telefonga "o'rnatish" (PWA) va sekin/uzilgan
// internetda ham tez ochilishi uchun.
//
// Qoidalar:
// - Backend (/api, boshqa domen: onrender.com) so'rovlari HECH QACHON
//   keshlanmaydi — baholash va kartalar har doim yangi bo'lishi kerak.
// - Sahifalar (navigatsiya: index.html, privacy.html, terms.html): avval
//   internetdan — yangi deploy darhol ko'rinsin; internet bo'lmasa — keshdan.
// - /assets/ ichidagi JS/CSS fayllar nomida xesh bor (masalan
//   index-yCz_3Dgm.js) — ular hech qachon o'zgarmaydi, shuning uchun
//   avval keshdan olinadi. Ikonkalar ham shunday.
//
// Yangi versiyada keshlash strategiyasi o'zgarsa, CACHE nomini oshiring
// (v2, v3...) — eski kesh "activate" paytida o'chiriladi.
const CACHE = 'speaking-coach-v4'; // v4: barcha sahifalar network-first, /api hech qachon keshlanmaydi
const SHELL = ['/', '/manifest.webmanifest', '/icons/icon-192.png', '/icons/logo.svg'];

self.addEventListener('install', (event) => {
  event.waitUntil(caches.open(CACHE).then((cache) => cache.addAll(SHELL)));
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    (async () => {
      const keys = await caches.keys();
      await Promise.all(keys.filter((k) => k !== CACHE).map((k) => caches.delete(k)));
      await self.clients.claim();
    })(),
  );
});

/** Ilovaning o'zi (SPA): marshrut hash'da, shuning uchun "/" va "/index.html" bitta sahifa. */
const isAppShell = (pathname) => pathname === '/' || pathname === '/index.html';

self.addEventListener('fetch', (event) => {
  const request = event.request;
  if (request.method !== 'GET') return;

  const url = new URL(request.url);
  // Backend va boshqa domenlar (API, YouTube, Telegram skripti) — tegmaymiz.
  if (url.origin !== self.location.origin) return;
  // Bir domenda proksi qilingan API bo'lsa ham — hech qachon keshlanmaydi.
  if (url.pathname.startsWith('/api/') || url.pathname === '/api') return;

  // Sahifalar: network-first. Har sahifa O'Z manzili bilan keshlanadi —
  // oflaynda /privacy.html o'rniga bosh sahifa ochilib qolmasin.
  if (request.mode === 'navigate') {
    const key = isAppShell(url.pathname) ? '/' : url.pathname;
    event.respondWith(
      fetch(request)
        .then((response) => {
          if (response.ok && response.type === 'basic') {
            const copy = response.clone();
            caches.open(CACHE).then((cache) => cache.put(key, copy));
          }
          return response;
        })
        .catch(async () => {
          const cached = await caches.match(key);
          if (cached) return cached;
          // Noma'lum sahifa oflaynda — hech bo'lmasa ilovaning o'zi.
          return (await caches.match('/')) ?? Response.error();
        }),
    );
    return;
  }

  // Xeshli fayllar va ikonkalar: cache-first.
  if (url.pathname.startsWith('/assets/') || url.pathname.startsWith('/icons/')) {
    event.respondWith(
      caches.match(request).then(
        (cached) =>
          cached ??
          fetch(request).then((response) => {
            if (response.ok) {
              const copy = response.clone();
              caches.open(CACHE).then((cache) => cache.put(request, copy));
            }
            return response;
          }),
      ),
    );
  }
});
