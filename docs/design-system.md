# Dizayn standarti

Yo'nalish: ELSA Speak, Babbel, Magoosh, British Council "IELTS Prep" kabi rasmiy ta'lim ilovalari.
Bu ilovalarda sokin fon, bitta asosiy rang va chiziqli ikonkalar ishlatiladi, matn o'qilishi birinchi o'rinda turadi.

## Qoidalar

1. **Rang.** Bitta asosiy rang bor: `--primary` (#1d4ed8, tungi rejimda #6e9bff). Bannerlar to'q ko'k `--ink` rangida.
   Binafsha yoki ko'p rangli gradientlar ishlatilmaydi. Ko'nikma ranglari (`--k-*`) faqat ikonka va uning foni uchun.
2. **Ikonkalar.** Interfeysda emoji ishlatilmaydi, `src/Icon.tsx` dagi chiziqli SVG ikonkalar ishlatiladi
   (24×24, `currentColor`). Yangi ikonka kerak bo'lsa, `PATHS` ga qo'shiladi.
   Istisnolar: nishon (badge) rasmlari, reyting medallari, 🔊 / ✓ / ✕ kabi kichik belgi-tugmalar.
3. **Matnlar** (`src/locales/*.ts`). Emoji bilan boshlanmaydi va tugamaydi. Atamalar tushuntiriladi:
   "Sinov imtihoni (mock)", "Takrorlab gapirish (shadowing)".
4. **Kartalar.** Oq fon, 1px chegara, radius 12px, deyarli sezilmaydigan soya.
   Bosiladigan karta butunligicha `button.card.cta.link-card` bo'ladi, chapda ikonka, o'ngda → strelka.
5. **Tugmalar.** `.btn-primary` to'liq rangli, soya va "sakrash"siz, ekranda bitta asosiy harakat uchun ishlatiladi.
   Ikkinchi darajali harakat uchun `.btn-outline` (kulrang chegara), uchinchi darajali uchun `.btn-link`.
6. **Tipografiya.** Inter. Sarlavhalar 650–700 og'irlikda. Bo'lim sarlavhalari (`.section-title`) kichik, katta harflarda, kulrang.
7. **Bo'sh holat.** Doira ichida ikonka (`.empty-ico`), qisqa sarlavha, bitta tushuntiruvchi gap va bitta asosiy tugma.
8. **Harakat (animatsiya).** Faqat qisqa paydo bo'lish (0.18 s). `prefers-reduced-motion` yoqilgan bo'lsa, o'chiriladi.

Barcha o'zgaruvchilar `src/styles.css` oxiridagi "Professional dizayn qatlami" bo'limida.
