import { defineMessages } from '../i18n';

/** Mikrofon xatolari (micErrors.ts) — Recorder va boshqa yozish oynalari uchun umumiy. */
export const micMsg = defineMessages(
  {
    denied:
      'Mikrofonga ruxsat berilmagan. Manzil satridagi 🔒 (yoki ⓘ) belgisini bosing → Mikrofon → Ruxsat berish, soʻng “Qayta urinish”ni bosing.',
    deniedInApp: (app: string) =>
      `${app} ichidagi brauzer mikrofonni koʻpincha bloklaydi. Saytni Chrome yoki Safariʼda oching: ⋮ / ⋯ menyu → “Brauzerda ochish”.`,
    openChrome: '🌐 Chromeʼda ochish',
    notFound: 'Mikrofon topilmadi. Naushnik yoki mikrofonni ulang va qayta urining.',
    busy: 'Mikrofon boshqa ilovada band (masalan, qoʻngʻiroq yoki ovoz yozish). Uni yoping va qayta urining.',
    insecure: 'Mikrofon faqat xavfsiz (https://) sahifada ishlaydi. Saytni https:// manzili bilan oching.',
    unsupported: 'Bu brauzer ovoz yozishni qoʻllab-quvvatlamaydi. Chrome, Safari yoki Edgeʼning yangi versiyasida oching.',
    other: 'Mikrofonni yoqib boʻlmadi. Qayta urinib koʻring yoki boshqa brauzerda oching.',
    retry: 'Qayta urinish',
  },
  {
    ru: {
      denied:
        'Нет доступа к микрофону. Нажмите на значок 🔒 (или ⓘ) в адресной строке → Микрофон → Разрешить, затем нажмите «Повторить».',
      deniedInApp: (app: string) =>
        `Встроенный браузер ${app} часто блокирует микрофон. Откройте сайт в Chrome или Safari: меню ⋮ / ⋯ → «Открыть в браузере».`,
      openChrome: '🌐 Открыть в Chrome',
      notFound: 'Микрофон не найден. Подключите наушники или микрофон и попробуйте снова.',
      busy: 'Микрофон занят другим приложением (например, звонком или диктофоном). Закройте его и попробуйте снова.',
      insecure: 'Микрофон работает только на защищённой странице (https://). Откройте сайт по адресу https://.',
      unsupported: 'Этот браузер не умеет записывать звук. Откройте сайт в свежей версии Chrome, Safari или Edge.',
      other: 'Не удалось включить микрофон. Попробуйте снова или откройте сайт в другом браузере.',
      retry: 'Повторить',
    },
    en: {
      denied:
        'Microphone access is blocked. Tap the 🔒 (or ⓘ) icon in the address bar → Microphone → Allow, then tap “Try again”.',
      deniedInApp: (app: string) =>
        `The ${app} in-app browser often blocks the microphone. Open the site in Chrome or Safari: ⋮ / ⋯ menu → “Open in browser”.`,
      openChrome: '🌐 Open in Chrome',
      notFound: 'No microphone found. Connect headphones or a microphone and try again.',
      busy: 'The microphone is busy in another app (e.g. a call or voice recorder). Close it and try again.',
      insecure: 'The microphone only works on a secure (https://) page. Open the site with an https:// address.',
      unsupported: 'This browser can’t record audio. Open the site in an up-to-date Chrome, Safari or Edge.',
      other: 'Could not turn on the microphone. Try again or open the site in another browser.',
      retry: 'Try again',
    },
  },
);
