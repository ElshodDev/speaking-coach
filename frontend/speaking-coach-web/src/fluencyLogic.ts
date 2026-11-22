// Ravonlik o'lchovlari: yozuvdagi uzun pauzalar (audio balandligidan) va
// natija uchun maslahatlar. Toza funksiyalar — fluencyLogic.test.ts tekshiradi.

/** Server qaytaradigan o'lchovlar (FluencyMetrics.cs bilan bir xil shakl). */
export interface FluencyStats {
  words: number;
  seconds: number | null;
  wpm: number | null;
  pace: 'slow' | 'natural' | 'fast' | 'unknown';
  fillers: number;
  fillersPerMinute: number | null;
  topFillers: { word: string; count: number }[];
  repetitions: number;
  selfCorrections: number;
  longPauses: number | null;
  pauseSeconds: number | null;
}

export interface FluencyPoint {
  at: string;
  wpm: number | null;
  fillersPerMinute: number | null;
  longPauses: number | null;
  /** Qayerdan: Speaking mashqi, Speaking imtihoni yoki AI suhbat. */
  source?: 'speaking' | 'mock' | 'talk';
}

export interface PauseStats {
  /** Gap ichidagi (boshi va oxiridagi jimlik hisobga kirmaydi) uzun pauzalar soni. */
  longPauses: number;
  /** Shu pauzalarning umumiy davomiyligi, soniya. */
  pauseSeconds: number;
  /** Birinchi ovozdan oxirgi ovozgacha, soniya. */
  speechSeconds: number;
}

const FRAME_MS = 30;
/** Shundan uzun jimlik — "uzun pauza" (IELTS'da sezilarli to'xtalish). */
const LONG_PAUSE_SECONDS = 1;

/** Audio namunalaridan har FRAME_MS uchun o'rtacha kvadratik balandlik (RMS). */
export function frameRms(samples: Float32Array, sampleRate: number, frameMs = FRAME_MS): number[] {
  const size = Math.max(1, Math.round((sampleRate * frameMs) / 1000));
  const out: number[] = [];
  for (let start = 0; start + size <= samples.length; start += size) {
    let sum = 0;
    for (let i = start; i < start + size; i++) sum += samples[i] * samples[i];
    out.push(Math.sqrt(sum / size));
  }
  return out;
}

function percentile(sorted: number[], p: number): number {
  return sorted[Math.min(sorted.length - 1, Math.max(0, Math.floor(p * (sorted.length - 1))))];
}

/**
 * Pauzalarni topadi. Chegara yozuvning o'ziga moslashadi (mikrofon va xona shovqini
 * har xil): shovqin darajasi (10-persentil) va nutq darajasi (95-persentil) orasidagi 20%.
 * Deyarli jim yoki juda qisqa yozuv — null (ishonchli o'lchab bo'lmaydi).
 */
export function analyzePauses(rms: number[], frameMs = FRAME_MS, minPauseSeconds = LONG_PAUSE_SECONDS): PauseStats | null {
  if (rms.length < Math.ceil(1000 / frameMs)) return null;
  const sorted = [...rms].sort((a, b) => a - b);
  const floor = percentile(sorted, 0.1);
  const peak = percentile(sorted, 0.95);
  if (peak < 0.005 || peak - floor < 0.003) return null;
  const threshold = floor + 0.2 * (peak - floor);

  const voiced = rms.map((v) => v > threshold);
  const first = voiced.indexOf(true);
  const last = voiced.lastIndexOf(true);
  if (first < 0) return null;

  const minFrames = Math.ceil((minPauseSeconds * 1000) / frameMs);
  let longPauses = 0;
  let pauseFrames = 0;
  let run = 0;
  for (let i = first; i <= last; i++) {
    if (!voiced[i]) {
      run++;
      continue;
    }
    if (run >= minFrames) {
      longPauses++;
      pauseFrames += run;
    }
    run = 0;
  }
  const round1 = (x: number) => Math.round(x * 10) / 10;
  return {
    longPauses,
    pauseSeconds: round1((pauseFrames * frameMs) / 1000),
    speechSeconds: round1(((last - first + 1) * frameMs) / 1000),
  };
}

/** Bir nechta kanal — bitta (o'rtachasi). */
export function mixToMono(channels: Float32Array[]): Float32Array {
  if (channels.length === 1) return channels[0];
  const n = Math.min(...channels.map((c) => c.length));
  const out = new Float32Array(n);
  for (const c of channels) for (let i = 0; i < n; i++) out[i] += c[i] / channels.length;
  return out;
}

export type FluencyTip = 'slow' | 'fast' | 'fillers' | 'pauses' | 'repeats' | 'great';

/** Daqiqasiga shuncha to'ldiruvchi so'zdan ko'pi — sezilarli. */
const FILLERS_PER_MINUTE_HIGH = 4;
/** Daqiqasiga shuncha uzun pauzadan ko'pi — sezilarli. */
const PAUSES_PER_MINUTE_HIGH = 3;

/** Eng muhim 1–3 maslahat (tartib — ta'sir bo'yicha). Hammasi yaxshi bo'lsa — "great". */
export function fluencyTips(m: FluencyStats): FluencyTip[] {
  const tips: FluencyTip[] = [];
  const minutes = m.seconds && m.seconds > 0 ? m.seconds / 60 : null;
  if (m.fillersPerMinute !== null && m.fillersPerMinute > FILLERS_PER_MINUTE_HIGH) tips.push('fillers');
  if (minutes && m.longPauses !== null && m.longPauses / minutes > PAUSES_PER_MINUTE_HIGH) tips.push('pauses');
  if (m.pace === 'slow') tips.push('slow');
  if (m.pace === 'fast') tips.push('fast');
  if (m.repetitions >= 3) tips.push('repeats');
  return tips.length ? tips.slice(0, 3) : m.pace === 'unknown' ? [] : ['great'];
}

/** Qiymat zonasi ranglar uchun: yaxshi / o'rtacha / e'tibor bering. */
export function fillerLevel(perMinute: number | null): 'good' | 'mid' | 'low' | null {
  if (perMinute === null) return null;
  if (perMinute <= 2) return 'good';
  return perMinute <= FILLERS_PER_MINUTE_HIGH ? 'mid' : 'low';
}

export function paceLevel(pace: FluencyStats['pace']): 'good' | 'mid' | null {
  if (pace === 'unknown') return null;
  return pace === 'natural' ? 'good' : 'mid';
}
