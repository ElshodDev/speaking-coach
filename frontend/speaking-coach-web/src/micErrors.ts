// Mikrofon xatolari: getUserMedia / MediaRecorder turli sabablarga ko'ra
// ishlamasligi mumkin. Umumiy "ruxsat berilmadi" o'rniga aniq sabab va
// yechim ko'rsatiladi. Toza funksiyalar — testlanadi.

export type MicProblem = 'denied' | 'notFound' | 'busy' | 'insecure' | 'unsupported' | 'other';

export interface MicEnv {
  secure: boolean;
  hasGetUserMedia: boolean;
  hasMediaRecorder: boolean;
}

export function currentMicEnv(): MicEnv {
  const w = typeof window === 'undefined' ? undefined : window;
  return {
    secure: w?.isSecureContext !== false,
    hasGetUserMedia: typeof navigator !== 'undefined' && !!navigator.mediaDevices?.getUserMedia,
    hasMediaRecorder: typeof MediaRecorder !== 'undefined',
  };
}

/** Yozishni boshlashdan OLDIN aniqlanadigan muammo (yoki null — urinib ko'rish mumkin). */
export function micPrecheck(env: MicEnv): MicProblem | null {
  if (!env.secure) return 'insecure';
  if (!env.hasGetUserMedia || !env.hasMediaRecorder) return 'unsupported';
  return null;
}

/** getUserMedia / MediaRecorder xatosini turkumlash (DOMException nomi bo'yicha). */
export function micProblemOf(err: unknown, env: MicEnv): MicProblem {
  const pre = micPrecheck(env);
  if (pre) return pre;
  const name = err && typeof err === 'object' && 'name' in err ? String((err as { name: unknown }).name) : '';
  switch (name) {
    case 'NotAllowedError':
    case 'PermissionDeniedError': // eski Chrome
    case 'SecurityError': // sahifa sozlamasi (Permissions-Policy) bloklagan
      return 'denied';
    case 'NotFoundError':
    case 'DevicesNotFoundError':
    case 'OverconstrainedError':
      return 'notFound';
    case 'NotReadableError':
    case 'TrackStartError':
    case 'AbortError':
      return 'busy';
    case 'NotSupportedError':
      return 'unsupported';
    default:
      return 'other';
  }
}
