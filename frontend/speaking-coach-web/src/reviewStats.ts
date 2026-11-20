// Takrorlash statistikasi: App (menyudagi raqam, streak) va Review sahifasi
// birga ishlatadi. Alohida faylda — App Review sahifasini oldindan yuklamasin.

export interface ReviewStats {
  due: number;
  total: number;
  reviewedToday: number;
  dailyGoal: number;
  streakDays: number;
}

// JavaScript'ning getTimezoneOffset() qiymati (Toshkent uchun -300) —
// server "bugun" va "ketma-ket kunlar"ni mahalliy vaqt bo'yicha hisoblaydi.
export const statsPath = () => `/api/review/stats?tzOffsetMinutes=${new Date().getTimezoneOffset()}`;
