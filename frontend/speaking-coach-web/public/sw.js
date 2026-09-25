// Service worker — ilovani telefonga "o'rnatish" (PWA) va sekin/uzilgan
// internetda ham tez ochilishi uchun.
//
// Qoidalar:
// - Backend (boshqa domen: onrender.com) so'rovlari HECH QACHON keshlanmaydi —
//   baholash va kartalar har doim yangi bo'lishi kerak.
// - Sahifaning o'zi (index.html): avval internetdan (yangi versiya chiqsa
//   darhol ko'rinsin), internet bo'lmasa — keshdan.
// - /assets/ ichidagi JS/CSS fayllar nomida xesh bor (masalan
//   index-yCz_3Dgm.js) — ular hech qachon o'zgarmaydi, shuning uchun
//   avval keshdan olinadi.
//
// Yangi versiyada keshlash strategiyasi o'zgarsa, CACHE nomini oshiring
// (v2, v3...) — eski kesh "activate" paytida o'chiriladi.
const CACHE = 'speaking-coach-v1';
const SHELL = ['/', '/manifest.webmanifest', '/icons/icon-192.png'];

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

self.addEventListener('fetch', (event) => {
  const request = event.request;
  if (request.method !== 'GET') return;

  const url = new URL(request.url);
  if (url.origin !== self.location.origin) return; // backend va boshqa domenlar — tegmaymiz

  if (request.mode === 'navigate') {
    event.respondWith(
      fetch(request)
        .then((response) => {
          const copy = response.clone();
          caches.open(CACHE).then((cache) => cache.put('/', copy));
          return response;
        })
        .catch(() => caches.match('/')),
    );
    return;
  }

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
