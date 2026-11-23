import { createContext } from 'react';

/**
 * Yangi menyuda (VITE_NEW_NAV) sahifa sarlavhasi tepa panelda ko'rsatiladi. Sahifaning o'z <PageHeader> i sarlavhasini shu
 * kontekst orqali tepa panelga uzatadi va o'zining <h1> ini ko'rinmas qiladi (ekran o'qiydigan dasturlar uchun qoladi) —
 * shunda sarlavha ikki marta chiqmaydi. Kontekst yo'q bo'lsa (eski menyu) — hech narsa o'zgarmaydi.
 */
export const PageTitleContext = createContext<{ set: (title: string | null) => void } | null>(null);
