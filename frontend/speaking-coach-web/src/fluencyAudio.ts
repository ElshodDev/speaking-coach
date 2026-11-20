import { analyzePauses, frameRms, mixToMono, type PauseStats } from './fluencyLogic';

type AudioCtor = typeof AudioContext;

/**
 * Yozuvdagi uzun pauzalarni brauzerning o'zida o'lchaydi (audio serverga baribir
 * yuboriladi — bu faqat raqamlar). Brauzer audio'ni o'qiy olmasa (eski Safari,
 * noma'lum format) yoki 4 soniyada ulgurmasa — null: baholash buning uchun to'xtamaydi.
 */
export async function measurePauses(blob: Blob, timeoutMs = 4000): Promise<PauseStats | null> {
  const Ctor: AudioCtor | undefined =
    typeof window === 'undefined' ? undefined : window.AudioContext ?? (window as unknown as { webkitAudioContext?: AudioCtor }).webkitAudioContext;
  if (!Ctor) return null;

  const work = (async () => {
    const ctx = new Ctor();
    try {
      const audio = await ctx.decodeAudioData(await blob.arrayBuffer());
      const channels = Array.from({ length: audio.numberOfChannels }, (_, i) => audio.getChannelData(i));
      return analyzePauses(frameRms(mixToMono(channels), audio.sampleRate));
    } finally {
      void ctx.close().catch(() => undefined);
    }
  })().catch(() => null);

  const timeout = new Promise<null>((resolve) => setTimeout(() => resolve(null), timeoutMs));
  return Promise.race([work, timeout]);
}
