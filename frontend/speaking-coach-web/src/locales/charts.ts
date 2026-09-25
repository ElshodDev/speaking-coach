import { defineMessages, enPlural, ruPlural } from '../i18n';

/** charts.tsx — chiziqli grafik, faollik kalendari, ustunli grafik. */
export const chartsMsg = defineMessages(
  {
    /** Hafta kunlari dushanbadan: kalendarda faqat 1-, 3-, 5-, 7-si yoziladi. */
    weekdays: ['Du', 'Se', 'Ch', 'Pa', 'Ju', 'Sh', 'Ya'],
    showTable: 'Jadval koʻrinishi',
    hideTable: 'Jadvalni yopish',
    date: 'Sana',
    less: 'Kam',
    more: 'Koʻp',
    /** Tooltipda son yonidagi soʻz: “3 harakat”. */
    actions: (_n: number) => 'harakat',
    lineAria: (title: string, n: number) =>
      `${title}: ${n} ta urinish. Qiymatlarni chap va oʻng tugmalar bilan koʻring.`,
    calendarAria: (days: number, active: number) => `Oxirgi ${days} kunda ${active} kun faol boʻldingiz`,
    calendarSummary: (days: number, active: number) => `${days} kunda ${active} kun faol`,
    barAria: (label: string, days: number) => `${label}, oxirgi ${days} kun`,
  },
  {
    ru: {
      weekdays: ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Вс'],
      showTable: 'Показать таблицей',
      hideTable: 'Скрыть таблицу',
      date: 'Дата',
      less: 'Меньше',
      more: 'Больше',
      actions: (n: number) => ruPlural(n, 'действие', 'действия', 'действий'),
      lineAria: (title: string, n: number) =>
        `${title}: ${n} ${ruPlural(n, 'попытка', 'попытки', 'попыток')}. Используйте стрелки влево и вправо, чтобы посмотреть значения.`,
      calendarAria: (days: number, active: number) =>
        `За последние ${days} ${ruPlural(days, 'день', 'дня', 'дней')} вы были активны ${active} ${ruPlural(active, 'день', 'дня', 'дней')}`,
      calendarSummary: (days: number, active: number) =>
        `Активны ${active} ${ruPlural(active, 'день', 'дня', 'дней')} из ${days}`,
      barAria: (label: string, days: number) => `${label}, последние ${days} ${ruPlural(days, 'день', 'дня', 'дней')}`,
    },
    en: {
      weekdays: ['Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa', 'Su'],
      showTable: 'Show as table',
      hideTable: 'Hide table',
      date: 'Date',
      less: 'Less',
      more: 'More',
      actions: (n: number) => enPlural(n, 'action', 'actions'),
      lineAria: (title: string, n: number) =>
        `${title}: ${n} ${enPlural(n, 'attempt', 'attempts')}. Use the left and right arrow keys to see the values.`,
      calendarAria: (days: number, active: number) =>
        `Active on ${active} ${enPlural(active, 'day', 'days')} in the last ${days} days`,
      calendarSummary: (days: number, active: number) => `${active} active ${enPlural(active, 'day', 'days')} out of ${days}`,
      barAria: (label: string, days: number) => `${label}, last ${days} days`,
    },
  },
);
