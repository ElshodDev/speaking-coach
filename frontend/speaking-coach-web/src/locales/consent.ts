import { defineMessages } from '../i18n';

/** consent.tsx — mikrofon oldidan "ovoz AI'ga yuboriladi" roziligi (maxfiylik siyosati bilan bir xil mazmun). */
export const consentMsg = defineMessages(
  {
    title: 'Ovozingiz sunʼiy intellektga yuboriladi',
    body: 'Yozuvingiz baholash uchun Google Gemini sunʼiy intellektiga yuboriladi. Ovoz yozuvi bizda saqlanmaydi.',
    warning:
      'Gemini bepul tarifida Google yuborilgan maʼlumotdan oʻz mahsulotlarini yaxshilash uchun foydalanishi mumkin. Shuning uchun toʻliq ism, telefon, manzil kabi shaxsiy maʼlumotni aytmang.',
    accept: 'Roziman, davom etish',
    cancel: 'Bekor qilish',
    privacy: 'Maxfiylik siyosati',
  },
  {
    ru: {
      title: 'Ваш голос будет отправлен ИИ',
      body: 'Запись отправляется в Google Gemini для оценки. У нас запись голоса не сохраняется.',
      warning:
        'На бесплатном тарифе Gemini Google может использовать отправленные данные для улучшения своих продуктов. Поэтому не называйте личные данные: полное имя, телефон, адрес.',
      accept: 'Согласен, продолжить',
      cancel: 'Отмена',
      privacy: 'Политика конфиденциальности',
    },
    en: {
      title: 'Your voice is sent to AI',
      body: 'Your recording is sent to Google Gemini AI to be assessed. We do not store the recording.',
      warning:
        'On the free Gemini tier Google may use submitted data to improve its products. So please do not say personal details such as your full name, phone number or address.',
      accept: 'I agree, continue',
      cancel: 'Cancel',
      privacy: 'Privacy policy',
    },
  },
);
