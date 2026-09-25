// Brauzerning o'rnatilgan ovoz sintezi (Web Speech API) bilan ishlash —
// Tinglash mashqi va "Yo'lda" rejimi ikkalasi ham shu yordamchilardan
// foydalanadi. Tashqi xizmat yoki API kaliti kerak emas.

export function isSpeechSupported(): boolean {
  return typeof window !== 'undefined' && 'speechSynthesis' in window;
}

/**
 * Matnni gaplarga bo'ladi. Sabab: Chrome'da bitta uzun "utterance"
 * ~15 soniyadan keyin jimgina uzilib qolishi ma'lum xato — qisqa
 * bo'laklarni navbatga qo'yish buni chetlab o'tadi.
 */
export function splitSentences(text: string): string[] {
  return (text.match(/[^.!?]+[.!?]+["')\]]*|[^.!?]+$/g) ?? [text]).map((s) => s.trim()).filter(Boolean);
}

export function pickEnglishVoice(voices: SpeechSynthesisVoice[]): SpeechSynthesisVoice | undefined {
  const english = voices.filter((v) => v.lang.toLowerCase().startsWith('en'));
  return (
    english.find((v) => /google|natural|neural/i.test(v.name) && /en[-_](us|gb)/i.test(v.lang)) ??
    english.find((v) => /en[-_](us|gb)/i.test(v.lang)) ??
    english[0]
  );
}

/**
 * Matnni o'qiydi va O'QIB BO'LGANDA (yoki to'xtatilganda) tugaydigan
 * Promise qaytaradi — shunda "o'qi → kut → keyingisini o'qi" ketma-ketligini
 * oddiy `await` bilan yozish mumkin ("Yo'lda" rejimi shunday ishlaydi).
 * Oldingi o'qishni to'xtatmaydi — buni chaqiruvchi hal qiladi.
 */
export function speakAsync(text: string, rate = 0.9): Promise<void> {
  return new Promise((resolve) => {
    if (!isSpeechSupported()) return resolve();
    const synth = window.speechSynthesis;
    const sentences = splitSentences(text);
    if (sentences.length === 0) return resolve();

    const voice = pickEnglishVoice(synth.getVoices());
    sentences.forEach((sentence, i) => {
      const u = new SpeechSynthesisUtterance(sentence);
      u.lang = voice?.lang ?? 'en-US';
      if (voice) u.voice = voice;
      u.rate = rate;
      if (i === sentences.length - 1) u.onend = () => resolve();
      // cancel() chaqirilganda navbatdagilar "error" bilan tugaydi — bu ham "tugadi".
      u.onerror = () => resolve();
      synth.speak(u);
    });
  });
}

export function stopSpeaking() {
  if (isSpeechSupported()) window.speechSynthesis.cancel();
}

export const sleep = (ms: number) => new Promise<void>((r) => setTimeout(r, ms));
