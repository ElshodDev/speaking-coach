// Shadowing: gapma-gap tinglash va darhol takrorlash. Ikki xil manba:
//  • YouTube videosi — o'z pleyerida (youtube-nocookie.com), har gap vaqti bo'yicha
//    alohida o'ynatiladi, yozuvi video ustida karaoke usulida yonadi;
//  • qahramonlar — ilovaning SVG qahramonlari brauzer ovozida navbatma-navbat gapiradi.
// Rejimlar: oddiy, avto (pauza bilan ketma-ket), qo'l tegizmasdan (o'ynaydi → yozadi →
// eshittiradi). So'zni bosib — ma'nosi va lug'atga qo'shish; qatorlar tarjimasi.
import { useCallback, useEffect, useRef, useState, useSyncExternalStore } from 'react';
import { apiJson, postJson } from './api';
import { Character } from './Characters';
import { useLang, useT } from './i18n';
import { shadowingMsg } from './locales/shadowing';
import {
  activeWord,
  audioExt,
  average,
  bareWord,
  bySeries,
  canFinish,
  CHARACTER_VOICE,
  clock,
  fractionAt,
  handsFreeRecordMs,
  hasTranslations,
  isKeyWord,
  lineSeconds,
  levelsIn,
  nextIndex,
  nextLesson,
  nextLoop,
  playsFor,
  progressMap,
  recordLimitMs,
  repeatPauseMs,
  scoreKey,
  segment,
  SPEEDS,
  thumbnail,
  tokenize,
  translationOf,
  videoFraction,
  type LoopMode,
  type ShadowCheck,
  type ShadowLesson,
  type ShadowLine,
  type ShadowMode,
  type ShadowProgress,
  type ShadowSummary,
} from './shadowLogic';
import { isSpeechSupported, pickVoices, sleep } from './speech';
import { PageHeader } from './ui';
import { focusedButton, typingTarget, useWide } from './layout';
import { normalizeWord, WordSheet } from './WordSheet';

const loadProgress = () => apiJson<ShadowProgress[]>('/api/shadowing/progress').catch(() => [] as ShadowProgress[]);

// ---------------- Ro'yxat ----------------

export function ShadowingList({ go }: { go: (route: string) => void }) {
  const t = useT(shadowingMsg);
  const [list, setList] = useState<ShadowSummary[] | null>(null);
  const [progress, setProgress] = useState<Record<string, ShadowProgress>>({});
  const [error, setError] = useState('');
  const [level, setLevel] = useState<string | null>(null);

  useEffect(() => {
    apiJson<ShadowSummary[]>('/api/shadowing')
      .then(setList)
      .catch((e) => setError(e instanceof Error ? e.message : String(e)));
    loadProgress().then((p) => setProgress(progressMap(p)));
  }, []);

  const shown = list ? list.filter((l) => !level || l.level === level) : [];

  return (
    <>
      <PageHeader title={t.title} subtitle={t.subtitle} onBack={() => go('practice')} />
      <p className="muted small">{t.how}</p>
      {error && <p className="error" role="alert">{error}</p>}
      {list && (
        <>
          <div className="chips" role="group" aria-label={t.level}>
            <button aria-pressed={level === null} onClick={() => setLevel(null)}>{t.all}</button>
            {levelsIn(list).map((lv) => (
              <button key={lv} aria-pressed={level === lv} onClick={() => setLevel(lv)}>{lv}</button>
            ))}
          </div>
          {shown.length === 0 && <p className="muted">{t.empty}</p>}
          {bySeries(shown).map((g) => (
            <section key={g.kind} className="shadow-series" data-testid={`series-${g.kind}`}>
              <h2 className="shadow-series-title">
                {t.series[g.kind]} <span className="muted small">· {t.seriesHint[g.kind]}</span>
              </h2>
              <div className="shadow-grid">
                {g.lessons.map((l) => {
                  const p = progress[l.id];
                  return (
                    <button key={l.id} className={`shadow-card${p ? ' is-done' : ''}`} data-testid="shadow-lesson" onClick={() => go(`shadowing/${l.id}`)}>
                      <div className="shadow-thumb">
                        {l.kind === 'youtube' && l.videoId ? (
                          <img src={thumbnail(l.videoId)} alt="" loading="lazy" />
                        ) : (
                          <div className="shadow-thumb-chars">
                            {l.characters.map((c) => <Character key={c} name={c} size={52} label={t.names[c]} />)}
                          </div>
                        )}
                        <span className="shadow-ep">{t.episode(l.episode)}</span>
                        <span className="shadow-level">{l.level}</span>
                        {p && <span className="shadow-done" data-testid="done-badge">{p.best != null ? t.best(p.best) : '✓'}</span>}
                      </div>
                      <div className="shadow-card-body">
                        <strong>{l.title}</strong>
                        <span className="muted small">{t.lines(l.lines)} · {t.minutes(l.seconds)}</span>
                      </div>
                    </button>
                  );
                })}
              </div>
            </section>
          ))}
          <p className="muted small" style={{ marginTop: 16 }}>{t.teacherHint}</p>
        </>
      )}
    </>
  );
}

// ---------------- Karaoke holati (faqat kerakli qism qayta chiziladi) ----------------

function createStore() {
  let value: number | null = null;
  const listeners = new Set<() => void>();
  return {
    get: () => value,
    set(v: number | null) {
      if (v === value) return;
      value = v;
      listeners.forEach((l) => l());
    },
    subscribe(l: () => void) {
      listeners.add(l);
      return () => listeners.delete(l);
    },
  };
}
type Store = ReturnType<typeof createStore>;

/** Video ustidagi yozuv: aytilgan so'zlar yonadi, kalit so'zlar rangli. */
function Karaoke({ line, store }: { line: ShadowLine; store: Store }) {
  const fraction = useSyncExternalStore(store.subscribe, store.get, store.get);
  const now = activeWord(line.text, fraction);
  let w = -1;
  return (
    <span className="karaoke" data-testid="karaoke">
      {tokenize(line.text).map((tok, k) => {
        if (!tok.word) return tok.text;
        w++;
        const cls = [w < now ? 'said' : '', w === now ? 'now' : '', isKeyWord(tok.text, line.keys) ? 'kw' : ''].filter(Boolean).join(' ');
        return <span key={k} className={cls || undefined}>{tok.text}</span>;
      })}
    </span>
  );
}

/** Joriy qator matni: so'zlar bosiladi (ma'nosi), kalit so'zlar rangli, AI natijasi — yashil/qizil. */
function TapText({ line, check, onWord }: { line: ShadowLine; check?: ShadowCheck; onWord: (w: string) => void }) {
  const status = new Map<string, boolean>();
  check?.words.forEach((x) => {
    const k = bareWord(x.word);
    status.set(k, (status.get(k) ?? true) && x.ok);
  });
  return (
    <span className="tap-text">
      {tokenize(line.text).map((tok, k) => {
        if (!tok.word) return tok.text;
        const ok = check ? status.get(bareWord(tok.text)) : undefined;
        const cls = ['word', isKeyWord(tok.text, line.keys) ? 'kw' : '', ok === true ? 'w-ok' : ok === false ? 'w-bad' : ''].filter(Boolean).join(' ');
        const pick = () => onWord(normalizeWord(bareWord(tok.text) || tok.text));
        return (
          <span
            key={k}
            className={cls}
            role="button"
            tabIndex={0}
            onClick={(e) => {
              e.stopPropagation();
              pick();
            }}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                pick();
              }
            }}
          >
            {tok.text}
          </span>
        );
      })}
    </span>
  );
}

function KeyText({ line }: { line: ShadowLine }) {
  return (
    <>
      {tokenize(line.text).map((tok, k) => (tok.word && isKeyWord(tok.text, line.keys) ? <span key={k} className="kw">{tok.text}</span> : tok.text))}
    </>
  );
}

// ---------------- O'ynatish ----------------

/** Bitta gapni o'ynatadi; `onFrac` — aytilgan ulush (0–1) karaoke uchun; tugaganda Promise yakunlanadi. */
interface LinePlayer {
  play(i: number, rate: number, onFrac: (f: number) => void): Promise<void>;
  stop(): void;
}

interface YTPlayer {
  seekTo(s: number, allowSeekAhead: boolean): void;
  playVideo(): void;
  pauseVideo(): void;
  getCurrentTime(): number;
  setPlaybackRate(r: number): void;
  destroy(): void;
}

declare global {
  interface Window {
    YT?: { Player: new (el: HTMLElement, opts: unknown) => YTPlayer };
    onYouTubeIframeAPIReady?: () => void;
  }
}

let ytLoading: Promise<NonNullable<Window['YT']>> | null = null;

/** YouTube IFrame API — faqat video dars ochilganda yuklanadi. */
function loadYouTube(): Promise<NonNullable<Window['YT']>> {
  if (window.YT?.Player) return Promise.resolve(window.YT);
  if (!ytLoading) {
    ytLoading = new Promise((resolve, reject) => {
      const prev = window.onYouTubeIframeAPIReady;
      window.onYouTubeIframeAPIReady = () => {
        prev?.();
        if (window.YT) resolve(window.YT);
      };
      const s = document.createElement('script');
      s.src = 'https://www.youtube.com/iframe_api';
      s.async = true;
      s.onerror = () => {
        ytLoading = null;
        reject(new Error('youtube'));
      };
      document.head.appendChild(s);
      window.setTimeout(() => {
        if (window.YT?.Player) return;
        ytLoading = null; // keyingi darsda qayta urinib ko'riladi
        reject(new Error('youtube timeout'));
      }, 20000);
    });
  }
  return ytLoading;
}

function youtubeLinePlayer(yt: YTPlayer, lesson: ShadowLesson): LinePlayer {
  let run = 0;
  let timer: number | undefined;
  let finish: (() => void) | null = null;
  const end = () => {
    window.clearInterval(timer);
    const f = finish;
    finish = null;
    f?.();
  };
  return {
    play(i, rate, onFrac) {
      end();
      const my = ++run;
      const seg = segment(lesson, i);
      const line = lesson.lines[i];
      return new Promise<void>((resolve) => {
        finish = resolve;
        try {
          yt.setPlaybackRate(rate);
          yt.seekTo(seg.from, true);
          yt.playVideo();
        } catch {
          end();
          return;
        }
        // Seek va yuklanish darhol bo'lmaydi: avval qism ichiga kirganini ko'ramiz (15 s gacha kutamiz),
        // keyin oxirini — vaqt chegarasi qism boshlangan paytdan hisoblanadi.
        let started = Date.now();
        let limitMs = 15000;
        let seen = false;
        timer = window.setInterval(() => {
          if (my !== run) return;
          const now = yt.getCurrentTime?.() ?? 0;
          if (!seen && now >= seg.from - 0.6 && now < seg.to) {
            seen = true;
            started = Date.now();
            limitMs = ((seg.to - seg.from) / rate + 3) * 1000;
          }
          if (seen) onFrac(videoFraction(line, now) ?? 0);
          if ((seen && now >= seg.to) || Date.now() - started > limitMs) {
            try {
              yt.pauseVideo();
            } catch {
              /* pleyer yopilgan */
            }
            onFrac(1);
            end();
          }
        }, 80);
      });
    },
    stop() {
      run++;
      try {
        yt.pauseVideo();
      } catch {
        /* pleyer hali tayyor emas */
      }
      end();
    },
  };
}

function characterLinePlayer(lesson: ShadowLesson, onSpeaker: (s: number | null) => void): LinePlayer {
  let run = 0;
  let finish: (() => void) | null = null;
  let tick: number | undefined;
  const end = () => {
    window.clearInterval(tick);
    const f = finish;
    finish = null;
    onSpeaker(null);
    f?.();
  };
  return {
    play(i, rate, onFrac) {
      const my = ++run;
      end();
      window.speechSynthesis?.cancel();
      const line = lesson.lines[i];
      const who = lesson.characters[line.speaker] ?? lesson.characters[0];
      const cfg = CHARACTER_VOICE[who] ?? CHARACTER_VOICE.owl;
      return new Promise<void>((resolve) => {
        finish = resolve;
        if (!isSpeechSupported()) {
          end();
          return;
        }
        const voices = pickVoices(window.speechSynthesis.getVoices());
        const voice = cfg.voice === 'male' ? voices.male : voices.female;
        const u = new SpeechSynthesisUtterance(line.text);
        u.lang = voice?.lang ?? 'en-US';
        if (voice) u.voice = voice;
        u.pitch = cfg.pitch;
        u.rate = cfg.rate * rate;
        // Karaoke: brauzer so'z chegaralarini bersa — ularga, bo'lmasa — taxminiy vaqtga qarab.
        let boundaries = false;
        const started = Date.now();
        const estMs = (lineSeconds(line) * 1000) / (cfg.rate * rate) + 250;
        u.onboundary = (e) => {
          if (my !== run || (e.name && e.name !== 'word')) return;
          boundaries = true;
          onFrac(fractionAt(line.text, e.charIndex));
        };
        u.onstart = () => my === run && onSpeaker(line.speaker);
        const done = () => {
          if (my !== run) return;
          onFrac(1);
          end();
        };
        u.onend = done;
        u.onerror = done;
        onSpeaker(line.speaker);
        tick = window.setInterval(() => {
          if (my === run && !boundaries) onFrac(Math.min(0.97, (Date.now() - started) / estMs));
        }, 90);
        window.speechSynthesis.speak(u);
        // Ba'zi brauzerlar "end" bermaydi — qotib qolmaslik uchun (ovozni ham to'xtatamiz, keyingi bosqichga aralashmasin).
        window.setTimeout(() => {
          if (my !== run) return;
          window.speechSynthesis.cancel();
          done();
        }, (line.text.split(/\s+/).length * 600) / (cfg.rate * rate) + 4000);
      });
    },
    stop() {
      run++;
      window.speechSynthesis?.cancel();
      end();
    },
  };
}

// ---------------- Ovoz yozish ----------------

type MicError = 'denied' | 'unsupported' | null;

function useRecorder() {
  const [recording, setRecording] = useState(false);
  const [micError, setMicError] = useState<MicError>(null);
  const recRef = useRef<MediaRecorder | null>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const timerRef = useRef<number | undefined>(undefined);
  // Har to'xtatish/bo'shatishda oshadi: ruxsat oynasi ochiq turganda bekor qilingan yozuv keyin boshlanib ketmasin.
  const genRef = useRef(0);

  const release = () => {
    genRef.current++;
    streamRef.current?.getTracks().forEach((tr) => tr.stop());
    streamRef.current = null;
  };
  const stop = () => {
    genRef.current++;
    window.clearTimeout(timerRef.current);
    if (recRef.current && recRef.current.state !== 'inactive') recRef.current.stop();
  };

  /** Yozadi va to'xtatilganda (qo'lda yoki vaqt tugab) Blob qaytaradi. `keep` — mikrofon ochiq qoladi (qo'l tegizmasdan rejim). */
  async function start(limitMs: number, keep = false): Promise<Blob | null> {
    if (typeof MediaRecorder === 'undefined' || !navigator.mediaDevices?.getUserMedia) {
      setMicError('unsupported');
      return null;
    }
    const gen = ++genRef.current;
    let stream = streamRef.current;
    if (!stream || stream.getTracks().every((tr) => tr.readyState === 'ended')) {
      try {
        stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      } catch {
        setMicError('denied');
        return null;
      }
      if (gen !== genRef.current) {
        // Kutish paytida bekor qilindi (sahifadan chiqildi, rejim almashdi yoki to'xtatildi).
        stream.getTracks().forEach((tr) => tr.stop());
        return null;
      }
      streamRef.current = stream;
    }
    if (gen !== genRef.current) return null;
    setMicError(null);
    return new Promise<Blob | null>((resolve) => {
      const rec = new MediaRecorder(stream!);
      const chunks: Blob[] = [];
      rec.ondataavailable = (e) => e.data.size > 0 && chunks.push(e.data);
      rec.onstop = () => {
        if (!keep) release();
        recRef.current = null;
        setRecording(false);
        resolve(chunks.length ? new Blob(chunks, { type: rec.mimeType || 'audio/webm' }) : null);
      };
      recRef.current = rec;
      rec.start();
      setRecording(true);
      timerRef.current = window.setTimeout(stop, limitMs);
    });
  }

  useEffect(
    () => () => {
      genRef.current++;
      window.clearTimeout(timerRef.current);
      if (recRef.current && recRef.current.state !== 'inactive') recRef.current.stop();
      streamRef.current?.getTracks().forEach((tr) => tr.stop());
      streamRef.current = null;
    },
    [],
  );
  return { recording, micError, start, stop, release };
}

// ---------------- Dars ----------------

interface Take {
  blob: Blob;
  url: string;
}

type Phase = 'idle' | 'playing' | 'pause' | 'rec' | 'mine';

const TR_KEY = 'speakingCoach.shadowTr';
function readTr(): boolean {
  try {
    return localStorage.getItem(TR_KEY) === '1';
  } catch {
    return false;
  }
}
function writeTr(on: boolean) {
  try {
    localStorage.setItem(TR_KEY, on ? '1' : '0');
  } catch {
    /* saqlab bo'lmadi — muhim emas */
  }
}

export function ShadowingLessonPage({ id, go, loggedIn, onLogin }: { id: string; go: (route: string) => void; loggedIn: boolean; onLogin?: () => void }) {
  const t = useT(shadowingMsg);
  const { lang } = useLang();
  const [lesson, setLesson] = useState<ShadowLesson | null>(null);
  const [list, setList] = useState<ShadowSummary[]>([]);
  const [progress, setProgress] = useState<Record<string, ShadowProgress>>({});
  const [error, setError] = useState('');
  const [index, setIndex] = useState(0);
  const [phase, setPhase] = useState<Phase>('idle');
  const [waitMs, setWaitMs] = useState(0);
  const [waitKey, setWaitKey] = useState(0);
  const [rate, setRate] = useState(1);
  const [mode, setMode] = useState<ShadowMode>('normal');
  const [loop, setLoop] = useState<LoopMode>(0);
  const [showTr, setShowTr] = useState(readTr);
  const [speaker, setSpeaker] = useState<number | null>(null);
  const [practiced, setPracticed] = useState<Set<number>>(new Set());
  const [takes, setTakes] = useState<Record<number, Take>>({});
  const [checks, setChecks] = useState<Record<number, ShadowCheck>>({});
  const [checking, setChecking] = useState<number | null>(null);
  const [batch, setBatch] = useState<{ i: number; n: number } | null>(null);
  const [checkError, setCheckError] = useState('');
  const [videoError, setVideoError] = useState(false);
  const [finished, setFinished] = useState('');
  const [picked, setPicked] = useState<{ word: string; sentence: string } | null>(null);
  const [player, setPlayer] = useState<LinePlayer | null>(null);

  const store = useRef(createStore()).current;
  const wide = useWide();
  // Klaviatura (kompyuter): Probel — tinglash/pauza, ←/→ — oldingi/keyingi gap, R — yozish.
  const keyHandler = useRef<(e: KeyboardEvent) => void>(() => undefined);
  useEffect(() => {
    const on = (e: KeyboardEvent) => keyHandler.current(e);
    window.addEventListener('keydown', on);
    return () => window.removeEventListener('keydown', on);
  }, []);
  // WordSheet effekti onClose'ga bog'liq — barqaror bo'lmasa, har renderda qayta so'raydi.
  const closePicked = useCallback(() => setPicked(null), []);
  const runRef = useRef(0);
  const settings = useRef({ rate, mode, loop });
  settings.current = { rate, mode, loop };
  const videoBox = useRef<HTMLDivElement | null>(null);
  const listRef = useRef<HTMLOListElement | null>(null);
  const audioRef = useRef<HTMLAudioElement | null>(null);
  const startedRef = useRef(false);
  const aliveRef = useRef(true);
  const recorder = useRecorder();

  useEffect(() => {
    apiJson<ShadowLesson>(`/api/shadowing/${id}`)
      .then(setLesson)
      .catch((e) => setError(e instanceof Error ? e.message : String(e)));
    apiJson<ShadowSummary[]>('/api/shadowing').then(setList).catch(() => setList([]));
    loadProgress().then((p) => setProgress(progressMap(p)));
  }, [id]);

  // Pleyer: qahramonlar — darhol; YouTube — API yuklangach.
  useEffect(() => {
    if (!lesson) return;
    if (lesson.kind === 'character') {
      window.speechSynthesis?.getVoices(); // Chrome ovozlar ro'yxatini oldindan yuklasin
      const p = characterLinePlayer(lesson, setSpeaker);
      setPlayer(p);
      return () => p.stop();
    }
    let yt: YTPlayer | null = null;
    let lp: LinePlayer | null = null;
    let cancelled = false;
    loadYouTube()
      .then((YT) => {
        if (cancelled || !videoBox.current) return;
        const el = document.createElement('div');
        videoBox.current.replaceChildren(el);
        yt = new YT.Player(el, {
          host: 'https://www.youtube-nocookie.com',
          videoId: lesson.videoId,
          width: '100%',
          height: '100%',
          playerVars: { start: Math.floor(lesson.from ?? 0), playsinline: 1, rel: 0, controls: 0, disablekb: 1, fs: 0, iv_load_policy: 3, cc_load_policy: 0 },
          events: {
            onReady: () => {
              if (cancelled || !yt) return;
              lp = youtubeLinePlayer(yt, lesson);
              setPlayer(lp);
            },
            onError: () => setVideoError(true),
          },
        });
      })
      .catch(() => setVideoError(true));
    return () => {
      cancelled = true;
      lp?.stop();
      try {
        yt?.destroy();
      } catch {
        /* allaqachon yopilgan */
      }
    };
  }, [lesson]);

  // Sahifadan chiqilganda: ovoz, yozuv va URL'lar tozalanadi.
  const takesRef = useRef(takes);
  takesRef.current = takes;
  useEffect(
    () => () => {
      aliveRef.current = false;
      runRef.current++;
      audioRef.current?.pause();
      Object.values(takesRef.current).forEach((tk) => URL.revokeObjectURL(tk.url));
    },
    [],
  );

  // Joriy gap ko'rinib tursin (faqat mashq boshlangandan keyin — ochilganda sahifa sakramasin).
  useEffect(() => {
    if (!startedRef.current) return;
    const item = listRef.current?.children[index] as HTMLElement | undefined;
    item?.scrollIntoView?.({ block: 'center', behavior: 'smooth' });
  }, [index]);

  if (error) {
    return (
      <>
        <PageHeader title={t.title} onBack={() => go('shadowing')} backLabel={t.back} />
        <p className="error" role="alert">{error}</p>
      </>
    );
  }
  if (!lesson) return <PageHeader title={t.title} onBack={() => go('shadowing')} backLabel={t.back} />;

  const total = lesson.lines.length;
  const line = lesson.lines[index];
  const check = checks[index];
  const take = takes[index];
  const avg = average(Object.values(checks).map((c) => c.score));
  const need = Math.min(total, Math.max(3, Math.ceil(total / 2)));
  const busy = phase !== 'idle';
  const summary = list.find((l) => l.id === lesson.id);
  const before = progress[lesson.id];
  const trAvailable = hasTranslations(lesson, lang);
  const trOn = showTr && trAvailable;
  const unchecked = Object.keys(takes).map(Number).filter((i) => !checks[i]).sort((a, b) => a - b);
  const next = finished ? nextLesson(list, lesson.id, { ...progress, [lesson.id]: true }) : null;
  const loopKey = loop === 0 ? 'off' : loop === 3 ? '3' : 'inf';

  const markPracticed = (i: number) => setPracticed((s) => (s.has(i) ? s : new Set(s).add(i)));

  function saveTake(i: number, blob: Blob): string | null {
    if (!aliveRef.current) return null; // sahifadan chiqilgan — URL yaratmaymiz
    const url = URL.createObjectURL(blob);
    setTakes((m) => {
      if (m[i]) URL.revokeObjectURL(m[i].url);
      return { ...m, [i]: { blob, url } };
    });
    setChecks((m) => {
      const { [i]: _old, ...rest } = m;
      return rest;
    });
    return url;
  }

  function playUrl(url: string): Promise<void> {
    return new Promise((resolve) => {
      audioRef.current?.pause();
      const a = new Audio(url);
      audioRef.current = a;
      const done = () => resolve();
      a.onended = done;
      a.onerror = done;
      a.onpause = done;
      a.play().catch(done);
      window.setTimeout(done, 30000);
    });
  }

  function stopAll() {
    runRef.current++;
    player?.stop();
    recorder.stop();
    audioRef.current?.pause();
    setPhase('idle');
  }

  function wait(ms: number, next: Phase) {
    setWaitMs(ms);
    setWaitKey((k) => k + 1);
    setPhase(next);
  }

  /** Gapni o'ynatadi; rejimga qarab: pauza/takror, yoki yozish va eshittirish, keyin keyingi gap. `single` — faqat bir marta. */
  async function playLine(i: number, single = false): Promise<void> {
    if (!player || !lesson) return;
    startedRef.current = true;
    const my = ++runRef.current;
    recorder.stop();
    audioRef.current?.pause();
    setIndex(i);
    store.set(null);
    setPhase('playing');
    const current = lesson.lines[i];
    for (let k = 0; ; k++) {
      await player.play(i, settings.current.rate, (f) => my === runRef.current && store.set(f));
      if (my !== runRef.current) return;
      const { mode: m, loop: lp, rate: r } = settings.current;
      if (single) {
        setPhase('idle');
        return;
      }
      if (m === 'free') {
        // Qo'l tegizmasdan: o'quvchi takrorlaydi (o'zi yoziladi) → yozuv eshittiriladi.
        const limit = handsFreeRecordMs(current, r);
        wait(limit, 'rec');
        const blob = await recorder.start(limit, true);
        if (blob && aliveRef.current) {
          const url = saveTake(i, blob);
          markPracticed(i);
          if (my !== runRef.current || !url) return;
          setPhase('mine');
          await playUrl(url);
        }
        if (my !== runRef.current) return;
        if (!blob) {
          setPhase('idle');
          return;
        }
        await sleep(350);
        break;
      }
      const more = k + 1 < playsFor(lp);
      if (m === 'normal' && !more) {
        setPhase('idle');
        return;
      }
      const ms = repeatPauseMs(current, r);
      wait(ms, 'pause');
      await sleep(ms);
      if (my !== runRef.current) return;
      if (m === 'auto') markPracticed(i);
      if (!more) break;
      store.set(null);
      setPhase('playing');
    }
    if (my !== runRef.current) return;
    const n = nextIndex(i, total);
    if (n === null) {
      setPhase('idle');
      return;
    }
    return playLine(n);
  }

  async function record() {
    stopAll();
    setCheckError('');
    const i = index;
    const blob = await recorder.start(recordLimitMs(lesson!.lines[i]));
    if (!blob || !aliveRef.current) return;
    saveTake(i, blob);
    markPracticed(i);
  }

  async function checkLine(i: number): Promise<boolean> {
    const tk = takesRef.current[i];
    if (!tk) return true;
    setChecking(i);
    setCheckError('');
    try {
      const form = new FormData();
      form.append('audio', tk.blob, `line.${audioExt(tk.blob.type)}`);
      form.append('lessonId', lesson!.id);
      form.append('line', String(i));
      const r = await apiJson<ShadowCheck>('/api/shadowing/check', { method: 'POST', body: form });
      if (!aliveRef.current) return false;
      // Kutish paytida gap qayta yozilgan bo'lsa — eski natija yangi yozuvga yopishmasin.
      if (takesRef.current[i] === tk) setChecks((m) => ({ ...m, [i]: r }));
      return true;
    } catch (e) {
      setCheckError(e instanceof Error ? e.message : String(e));
      return false;
    } finally {
      setChecking(null);
    }
  }

  async function checkAll() {
    stopAll();
    const todo = unchecked;
    for (let k = 0; k < todo.length; k++) {
      if (!aliveRef.current) return;
      setBatch({ i: k + 1, n: todo.length });
      if (!(await checkLine(todo[k]))) break;
    }
    if (aliveRef.current) setBatch(null);
  }

  async function finish() {
    stopAll();
    try {
      const r = await postJson<{ saved: boolean }>(`/api/shadowing/${lesson!.id}/done`, { lines: practiced.size, average: avg });
      setFinished(!loggedIn ? t.finishedGuest : r.saved ? t.finishedSaved : t.finishedAgain);
    } catch (e) {
      setFinished(e instanceof Error ? e.message : String(e));
    }
  }

  function changeMode(m: ShadowMode) {
    stopAll();
    if (m !== 'free') recorder.release();
    setMode(m);
  }

  const jump = (i: number | null) => i !== null && playLine(i);
  keyHandler.current = (e: KeyboardEvent) => {
    if (!player || picked || e.ctrlKey || e.metaKey || e.altKey || typingTarget(e)) return;
    const onButton = focusedButton(e);
    if (e.key === ' ' && !onButton) {
      e.preventDefault();
      if (busy) stopAll();
      else playLine(index);
    } else if (e.key === 'ArrowRight') {
      e.preventDefault();
      jump(nextIndex(index, total));
    } else if (e.key === 'ArrowLeft') {
      e.preventDefault();
      jump(index > 0 ? index - 1 : null);
    } else if ((e.key === 'r' || e.key === 'R') && mode !== 'free') {
      e.preventDefault();
      if (recorder.recording) recorder.stop();
      else void record();
    }
  };
  const status =
    phase === 'pause' ? t.yourTurn : phase === 'rec' ? t.yourTurnRec : phase === 'mine' ? t.listeningBack : null;

  return (
    <div className="shadow-page">
      <PageHeader title={lesson.title} onBack={() => go('shadowing')} backLabel={t.back} />
      <div className="shadow-meta muted small">
        <span className="badge">{lesson.level}</span>
        {summary && <span>{t.series[lesson.kind]} {t.episode(summary.episode)}</span>}
        <span>· {t.lines(total)}</span>
        {before && <span className="shadow-before" data-testid="done-before">{t.doneBefore(before.times, before.best)}</span>}
      </div>

      {(() => {
        const mediaEl = (
          <>
      <div className="shadow-media" data-testid="media">
        {lesson.kind === 'youtube' ? (
          <div className="shadow-video">
            <div ref={videoBox} className="shadow-video-frame" data-testid="yt-box" />
            <div className="shadow-overlay">
              <Karaoke line={line} store={store} />
              {trOn && translationOf(line, lang) && <span className="shadow-overlay-tr">{translationOf(line, lang)}</span>}
            </div>
            {videoError && <p className="error shadow-video-error" role="alert">{t.videoError}</p>}
          </div>
        ) : (
          <div className="shadow-stage" data-testid="stage">
            <div className="shadow-actors">
              {lesson.characters.map((c, s) => (
                <div key={c} className={`shadow-actor${speaker === s ? ' speaking' : ''}`}>
                  <Character name={c} talking={speaker === s} active={line.speaker === s} size={lesson.characters.length === 1 ? 96 : 84} label={t.names[c]} />
                </div>
              ))}
            </div>
            <div className={`shadow-bubble${lesson.characters.length > 1 ? (line.speaker === 0 ? ' left' : ' right') : ''}`}>
              <Karaoke line={line} store={store} />
              {trOn && translationOf(line, lang) && <span className="shadow-overlay-tr">{translationOf(line, lang)}</span>}
            </div>
          </div>
        )}
        {status && (
          <div className={`shadow-status ${phase}`} data-testid="your-turn" role="status">
            <div className="spread">
              <strong>{status}</strong>
              {phase === 'rec' && (
                <button className="btn-link" onClick={recorder.stop}>{t.doneRec}</button>
              )}
            </div>
            {(phase === 'pause' || phase === 'rec') && (
              <div className="progress">
                <div key={waitKey} className="shadow-countdown" style={{ animationDuration: `${waitMs}ms` }} />
              </div>
            )}
          </div>
        )}
      </div>

          </>
        );
        const controlsEl = (
          <>
      <div className="shadow-modebar">
        <div className="segmented" role="group" aria-label={t.modes.normal}>
          {(['normal', 'auto', 'free'] as const).map((m) => (
            <button key={m} aria-pressed={mode === m} onClick={() => changeMode(m)}>{t.modes[m]}</button>
          ))}
        </div>
        {trAvailable && (
          <div className="chips" style={{ margin: 0 }}>
            <button
              aria-pressed={showTr}
              onClick={() => {
                setShowTr((v) => !v);
                writeTr(!showTr);
              }}
            >
              {t.translation}
            </button>
          </div>
        )}
      </div>
      <p className="muted small shadow-modehint">{t.modeHint[mode]}</p>
      {lesson.kind === 'youtube' && <p className="muted small" style={{ margin: '0 0 8px' }}>{t.videoNote}</p>}
      {wide && <p className="muted tiny" data-testid="shadow-keys">⌨️ {t.keysHint}</p>}

          </>
        );
        const linesEl = (
          <>
      <ol className="shadow-lines" ref={listRef} data-testid="lines">
        {lesson.lines.map((l, i) => {
          const current = i === index;
          const tr = trOn ? translationOf(l, lang) : null;
          const who = lesson.characters.length > 1 ? lesson.characters[l.speaker] ?? lesson.characters[0] : null;
          const num = lesson.kind === 'youtube' && l.start != null ? clock(l.start) : String(i + 1);
          if (!current) {
            return (
              <li key={i}>
                <button className="shadow-line" onClick={() => playLine(i, true)} disabled={!player}>
                  <span className="n">{num}</span>
                  {who && <span className="who" aria-hidden="true"><Character name={who} size={24} /></span>}
                  <span className="txt">
                    <span>
                      <KeyText line={l} />
                    </span>
                    {tr && <span className="tr">{tr}</span>}
                  </span>
                  {checks[i] ? (
                    <span className={`mini txt-${scoreKey(checks[i].score)}`}>{checks[i].score}</span>
                  ) : practiced.has(i) ? (
                    <span className="mini" aria-label="✓">✓</span>
                  ) : null}
                </button>
              </li>
            );
          }
          return (
            <li key={i}>
              <div className="shadow-line current" aria-current="true" data-testid="current-line">
                <div className="shadow-line-head">
                  <span className="n">{num}</span>
                  {who && <span className="who" aria-hidden="true"><Character name={who} size={24} /></span>}
                  <span className="muted small">{t.lineN(i + 1)}</span>
                  {check && <span className={`shadow-score txt-${scoreKey(check.score)}`} data-testid="score">{t.score}: {check.score}</span>}
                </div>
                <p className="shadow-line-text">
                  <TapText line={l} check={check} onWord={(w) => { stopAll(); setPicked({ word: w, sentence: l.text }); }} />
                </p>
                {tr && <p className="tr" style={{ margin: 0 }}>{tr}</p>}
                <p className="muted small" style={{ margin: 0 }}>{t.tapWords}</p>
                <div className="shadow-rec-buttons">
                  {mode !== 'free' && (recorder.recording && phase === 'idle' ? (
                    <button className="btn btn-danger" onClick={recorder.stop}>{t.stopRec}</button>
                  ) : (
                    <button className="btn btn-primary" onClick={record} disabled={recorder.recording}>{t.record}</button>
                  ))}
                  {take && !recorder.recording && (
                    <button className="btn btn-outline" onClick={() => { stopAll(); void playUrl(take.url); }}>{t.mine}</button>
                  )}
                  {take && !recorder.recording && (
                    <button className="btn btn-teal" onClick={() => checkLine(index)} disabled={checking !== null || batch !== null}>
                      {checking === index ? t.checking : t.check}
                    </button>
                  )}
                </div>
                {recorder.recording && phase === 'idle' && <p className="muted small shadow-rec-live" role="status">● {t.recording}</p>}
                {check && (check.heard || check.tip) && (
                  <div className="shadow-result" data-testid="check-result">
                    {check.heard && <p className="small" style={{ margin: 0 }}><span className="muted">{t.heard}:</span> <i>{check.heard}</i></p>}
                    {check.tip && <p className="small" style={{ margin: 0 }}><span className="muted">{t.tip}:</span> {check.tip}</p>}
                  </div>
                )}
              </div>
            </li>
          );
        })}
      </ol>

      {recorder.micError && <p className="error small" role="alert">{recorder.micError === 'denied' ? t.micDenied : t.noMic}</p>}
      {checkError && <p className="error small" role="alert">{checkError}</p>}

      {unchecked.length > 1 && (
        <div className="card stack" data-testid="check-all">
          <span>{t.recordedN(unchecked.length)}</span>
          <button className="btn btn-teal" onClick={checkAll} disabled={batch !== null || checking !== null}>
            {batch ? t.checkingAll(batch.i, batch.n) : t.checkAll(unchecked.length)}
          </button>
        </div>
      )}

      <div className="card stack" data-testid="finish">
        <div className="spread">
          <span>{t.progress(practiced.size, total)}</span>
          {avg !== null && <span className="muted small">{t.avg(avg)}</span>}
        </div>
        <div className="progress"><div style={{ width: `${(practiced.size / total) * 100}%` }} /></div>
        {finished ? (
          <>
            <p className="shadow-finished" role="status">{finished}</p>
            {next && (
              <button className="btn btn-primary" onClick={() => go(`shadowing/${next.id}`)} data-testid="next-lesson">
                {t.nextLesson} {next.title}
              </button>
            )}
          </>
        ) : (
          <>
            <button className="btn btn-primary" onClick={finish} disabled={!canFinish(practiced.size, total)}>{t.finish}</button>
            {!canFinish(practiced.size, total) && <p className="muted small" style={{ margin: 0 }}>{t.finishHint(need)}</p>}
          </>
        )}
      </div>

          </>
        );
        const dockEl = (
      <div className="shadow-dock" data-testid="dock">
        <div className="shadow-controls" role="group" aria-label={t.play}>
          <button className="shadow-ctl" onClick={() => playLine(index, true)} disabled={!player} aria-label={t.replay}>
            <span aria-hidden="true">↺</span>
            <small>{t.replay}</small>
          </button>
          <button className="shadow-ctl" onClick={() => jump(index > 0 ? index - 1 : null)} disabled={!player || index === 0} aria-label={t.prev}>
            <span aria-hidden="true">⏮</span>
            <small>{t.prev}</small>
          </button>
          <button className="shadow-ctl main" onClick={() => (busy ? stopAll() : playLine(index))} disabled={!player} aria-label={busy ? t.pause : t.play}>
            <span aria-hidden="true">{busy ? '⏸' : '▶'}</span>
            <small>{busy ? t.pause : t.play}</small>
          </button>
          <button className="shadow-ctl" onClick={() => jump(nextIndex(index, total))} disabled={!player || index === total - 1} aria-label={t.next}>
            <span aria-hidden="true">⏭</span>
            <small>{t.next}</small>
          </button>
          <button className="shadow-ctl" aria-pressed={loop !== 0} onClick={() => setLoop((v) => nextLoop(v))} aria-label={t.loop(loopKey)} disabled={mode === 'free'}>
            <span aria-hidden="true">🔁</span>
            <small>{t.loop(loopKey)}</small>
          </button>
        </div>
        <div className="shadow-speeds" role="group" aria-label={t.speed}>
          {SPEEDS.map((s) => (
            <button key={s} aria-pressed={rate === s} onClick={() => setRate(s)}>{s}×</button>
          ))}
        </div>
      </div>
        );
        // Kompyuterda: video/qahramonlar va boshqaruv chapda (yopishib turadi), gaplar ro'yxati o'ngda.
        return wide ? (
          <div className="shadow-wide">
            <div className="shadow-left">
              {mediaEl}
              {controlsEl}
              {dockEl}
            </div>
            <div className="shadow-right">{linesEl}</div>
          </div>
        ) : (
          <>
            {mediaEl}
            {controlsEl}
            {linesEl}
            {dockEl}
          </>
        );
      })()}

      {picked && (
        <WordSheet word={picked.word} sentence={picked.sentence} loggedIn={loggedIn} onClose={closePicked} onLogin={onLogin} />
      )}
    </div>
  );
}
