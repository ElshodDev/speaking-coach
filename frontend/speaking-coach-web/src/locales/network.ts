import { defineMessages, enPlural, ruPlural } from '../i18n';

/** api.ts va ServerWake.tsx — tarmoq xatolari va "server uygʻonmoqda" banneri. */
export const networkMsg = defineMessages(
  {
    offline: 'Internet aloqasi yoʻq. Ulanishni tekshirib, qayta urinib koʻring.',
    timeout: 'Server javob bermayapti. Birozdan keyin qayta urinib koʻring.',
    unreachable: 'Server bilan bogʻlanib boʻlmadi. Internetni tekshiring yoki birozdan keyin qayta urinib koʻring.',
    serverDown: 'Server hozir ishlamayapti yoki qayta ishga tushmoqda. Bir daqiqadan keyin qayta urinib koʻring.',
    aiBusy: (seconds: number | undefined) =>
      seconds
        ? `Sunʼiy intellekt hozir band. ${seconds} soniyadan keyin qayta urinib koʻring.`
        : 'Sunʼiy intellekt hozir band. Birozdan keyin qayta urinib koʻring.',
    wakeTitle: 'Server uygʻonmoqda…',
    wakeText: 'Bepul serverda birinchi soʻrov 30–60 soniya olishi mumkin.',
  },
  {
    ru: {
      offline: 'Нет подключения к интернету. Проверьте соединение и попробуйте ещё раз.',
      timeout: 'Сервер не отвечает. Попробуйте ещё раз чуть позже.',
      unreachable: 'Не удалось связаться с сервером. Проверьте интернет или попробуйте чуть позже.',
      serverDown: 'Сервер сейчас недоступен или перезапускается. Попробуйте через минуту.',
      aiBusy: (seconds: number | undefined) =>
        seconds
          ? `ИИ сейчас перегружен. Попробуйте через ${seconds} ${ruPlural(seconds, 'секунду', 'секунды', 'секунд')}.`
          : 'ИИ сейчас перегружен. Попробуйте чуть позже.',
      wakeTitle: 'Сервер просыпается…',
      wakeText: 'На бесплатном сервере первый запрос может занять 30–60 секунд.',
    },
    en: {
      offline: 'No internet connection. Check your connection and try again.',
      timeout: 'The server is not responding. Please try again in a moment.',
      unreachable: 'Could not reach the server. Check your internet or try again in a moment.',
      serverDown: 'The server is unavailable or restarting. Please try again in a minute.',
      aiBusy: (seconds: number | undefined) =>
        seconds
          ? `The AI is busy right now. Try again in ${seconds} ${enPlural(seconds, 'second', 'seconds')}.`
          : 'The AI is busy right now. Please try again in a moment.',
      wakeTitle: 'Waking up the server…',
      wakeText: 'On the free server the first request can take 30–60 seconds.',
    },
  },
);
