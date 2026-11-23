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

## "Kundalik" mavzusi (`VITE_NEW_NAV=true`)

Yangi menyu bayrog'i yoqilganda `.app.nn` sinfi qo'shiladi va `src/newNav.css` asosiy qoidalarni o'zgartiradi
(bayroq o'chiq bo'lsa, yuqoridagi qoidalar o'zgarishsiz amal qiladi). Yo'nalish: qog'oz daftar va kundalik reja.

1. **Rang.** Qog'oz fon (`--bg` #fbf8f1), to'q jigarrang matn va asosiy tugma (`--primary` #2a2218), sariq urg'u
   (`--nn-gold`, `--nn-note`). Faol bo'lim — to'q sariq (`--nn-accent`). Tungi rejimda to'q jigarrang fon va och sariq urg'u.
   Ko'k va binafsha ishlatilmaydi; ko'nikma ranglari (`--k-*`) va holat ranglari (yashil/sariq/qizil) o'zgarmaydi.
2. **Menyu.** Pastda 5 ta tugma: Asosiy · Takrorlash · **AI ustoz** (o'rtada, katta doira) · Mashqlar · Natijalar.
   Lug'at alohida tugma emas, Takrorlash ichida "Kartalar | Lug'at" segmenti. Kompyuterda chap menyu (nom yozuvisiz, faqat logo).
3. **Tepa panel.** Chapda sarlavha (bosh sahifada "Salom, ism / Bugun"), o'ngda seriya, **Fikr**, **Hammasi** va avatar.
   Sahifa sarlavhasi tepa panelda: sahifaning `<PageHeader>`i uni `PageTitleContext` orqali uzatadi va o'z `<h1>`ini ko'rinmas (`.nn-sr`) qiladi, shuning uchun sarlavha ikki marta chiqmaydi (orqaga tugmasi va izoh sahifada qoladi).
   "Hammasi" — qidiruvli oyna; ilovadagi barcha bo'limlar (`HUB` ro'yxati, `src/newNav.ts`). Yangi joy qo'shilsa, shu ro'yxatga ham qo'shiladi.
4. **Banner = eslatma.** Katta bannerlar (`.hero`, demo-banner) to'q ko'k o'rniga sariq "yopishqoq eslatma": qora matn, qora tugma, Georgia sarlavha.
5. **Tugmalar.** `.btn-primary` — qora (tungi rejimda sariq), soyasiz; `.btn-outline` — qora chegara. Tanlangan chip va tab — qog'oz tusida.
6. **Bosh sahifa.** Maqsad eslatmasi + bugungi reja chizig'i: birinchi bajarilmagan qadam "Hozir" (to'q ramka, asosiy tugma), keyingilari — kontur tugma, bajarilganlari — ✓ va xira.
7. **Profil.** Tepada ism, daraja/maqsad/seriya va uchta tez tugma; keyin guruhlar: O'rganish, Hamjamiyat, Hisob, Ilova, eng pastda qizil "xavfli zona".
8. **O'qituvchi paneli.** Taklif kodi — sariq stiker (monospace); vazifa ulushi — rangli pilyula; natijalar jadvali — pastel kataklar.

Yangi komponent yozilganda: ranglar faqat o'zgaruvchilardan (`var(--…)`), qattiq yozilgan `#fff` matn rangi fon moslashganda ko'rinmay qolishi mumkin (tungi rejimda tanlangan chiplarda shunday bo'lgan edi).
