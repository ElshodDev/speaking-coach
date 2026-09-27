// Shadowing: gapma-gap tinglash va darhol takrorlash. Ikki xil manba:
//  • YouTube videosi — o'z pleyerida (youtube-nocookie.com), har gap vaqti bo'yicha
//    alohida o'ynatiladi, tagida yozuvi chiqadi;
//  • qahramonlar — ilovaning SVG qahramonlari brauzer ovozida navbatma-navbat gapiradi.
// O'quvchi o'z ovozini yozib, asl bilan solishtiradi; xohlasa AI bilan tekshiradi.
import { useCallback, useEffect, useRef, useState } from 'react';
import { apiJson, postJson } from './api';
import { Character } from './Characters';
import { useT } from './i18n';
import { shadowingMsg } from './locales/shadowing';
import {
  audioExt,
  average,
  canFinish,
  CHARACTER_VOICE,
  clock,
  filterLessons,
  levelsIn,
  nextIndex,
  recordLimitMs,
  repeatPauseMs,
  scoreKey,
  segment,
  SPEEDS,
  thumbnail,
  type ShadowCheck,
  type ShadowFilter,
  type ShadowKind,
  type ShadowLesson,
  type ShadowSummary,
} from './shadowLogic';
import { isSpeechSupported, pickVoices, sleep } from './speech';
import { PageHeader } from './ui';

// ---------------- Ro'yxat ----------------

export function ShadowingList({ go }: { go: (route: string) => void }) {
  const t = useT(shadowingMsg);
  const [list, setList] = useState<ShadowSummary[] | null>(null);
  const [error, setError] = useState('');
  const [filter, setFilter] = useState<ShadowFilter>({ level: null, kind: null });

  useEffect(() => {
    apiJson<ShadowSummary[]>('/api/shadowing')
      .then(setList)
      .catch((e) => setError(e instanceof Error ? e.message : String(e)));
  }, []);

  const shown = list ? filterLessons(list, filter) : [];
  const kinds: { key: ShadowKind | null; label: string }[] = [
    { key: null, label: t.all },
    { key: 'youtube', label: t.video },
    { key: 'character', label: t.characters },
  ];

  return (
    <>
      <PageHeader title={t.title} subtitle={t.subtitle} onBack={() => go('practice')} />
      <p className="muted small">{t.how}</p>
      {error && <p className="error" role="alert">{error}</p>}
      {list && (
        <>
          <div className="chips" role="group" aria-label={t.video}>
            {kinds.map((k) => (
              <button key={k.label} aria-pressed={filter.kind === k.key} onClick={() => setFilter((f) => ({ ...f, kind: k.key }))}>
                {k.label}
              </button>
            ))}
          </div>
          <div className="chips" role="group" aria-label="CEFR">
            <button aria-pressed={filter.level === null} onClick={() => setFilter((f) => ({ ...f, level: null }))}>{t.all}</button>
            {levelsIn(list).map((lv) => (
              <button key={lv} aria-pressed={filter.level === lv} onClick={() => setFilter((f) => ({ ...f, level: lv }))}>{lv}</button>
            ))}
          </div>
          {shown.length === 0 && <p className="muted">{t.empty}</p>}
          <div className="shadow-grid">
            {shown.map((l) => (
              <button key={l.id} className="shadow-card" data-testid="shadow-lesson" onClick={() => go(`shadowing/${l.id}`)}>
                <div className="shadow-thumb">
                  {l.kind === 'youtube' && l.videoId ? (
                    <img src={thumbnail(l.videoId)} alt="" loading="lazy" />
                  ) : (
                    <div className="shadow-thumb-chars">
                      {l.characters.map((c) => <Character key={c} name={c} size={64} label={t.names[c]} />)}
                    </div>
                  )}
                  <span className="shadow-level">{l.level}</span>
                </div>
                <div className="shadow-card-body">
                  <strong>{l.title}</strong>
                  <span className="muted small">
                    {l.kind === 'youtube' ? '🎬' : '🐾'} {t.lines(l.lines)} · {t.minutes(l.seconds)}
                  </span>
                </div>
              </button>
            ))}
          </div>
          <p className="muted small" style={{ marginTop: 16 }}>{t.teacherHint}</p>
        </>
      )}
    </>
  );
}

// ---------------- O'ynatish ----------------

/** Bitta gapni o'ynatadi; tugaganda (yoki to'xtatilganda) Promise yakunlanadi. */
interface LinePlayer {
  play(i: number, rate: number): Promise<void>;
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
      window.setTimeout(() => reject(new Error('youtube timeout')), 20000);
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
    play(i, rate) {
      end();
      const my = ++run;
      const seg = segment(lesson, i);
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
        const started = Date.now();
        const maxMs = ((seg.to - seg.from) / rate + 5) * 1000;
        // Seek darhol bo'lmaydi: avval qism ichiga kirganini ko'ramiz, keyin oxirini kutamiz.
        let seen = false;
        timer = window.setInterval(() => {
          if (my !== run) return;
          const now = yt.getCurrentTime?.() ?? 0;
          if (now >= seg.from - 0.6 && now < seg.to) seen = true;
          if ((seen && now >= seg.to) || Date.now() - started > maxMs) {
            try {
              yt.pauseVideo();
            } catch {
              /* pleyer yopilgan */
            }
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
  const end = () => {
    const f = finish;
    finish = null;
    onSpeaker(null);
    f?.();
  };
  return {
    play(i, rate) {
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
        u.onstart = () => my === run && onSpeaker(line.speaker);
        u.onend = () => my === run && end();
        u.onerror = () => my === run && end();
        onSpeaker(line.speaker);
        window.speechSynthesis.speak(u);
        // Ba'zi brauzerlar "end" bermaydi — qotib qolmaslik uchun.
        window.setTimeout(() => my === run && end(), (line.text.split(/\s+/).length * 520) / rate + 3000);
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
  const timerRef = useRef<number | undefined>(undefined);

  const stop = useCallback(() => {
    window.clearTimeout(timerRef.current);
    if (recRef.current && recRef.current.state !== 'inactive') recRef.current.stop();
  }, []);

  /** Yozadi va to'xtatilganda (qo'lda yoki vaqt tugab) Blob qaytaradi. */
  const start = useCallback(async (limitMs: number): Promise<Blob | null> => {
    if (typeof MediaRecorder === 'undefined' || !navigator.mediaDevices?.getUserMedia) {
      setMicError('unsupported');
      return null;
    }
    let stream: MediaStream;
    try {
      stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    } catch {
      setMicError('denied');
      return null;
    }
    setMicError(null);
    return new Promise<Blob | null>((resolve) => {
      const rec = new MediaRecorder(stream);
      const chunks: Blob[] = [];
      rec.ondataavailable = (e) => e.data.size > 0 && chunks.push(e.data);
      rec.onstop = () => {
        stream.getTracks().forEach((tr) => tr.stop());
        recRef.current = null;
        setRecording(false);
        resolve(chunks.length ? new Blob(chunks, { type: rec.mimeType || 'audio/webm' }) : null);
      };
      recRef.current = rec;
      rec.start();
      setRecording(true);
      timerRef.current = window.setTimeout(stop, limitMs);
    });
  }, [stop]);

  useEffect(() => () => stop(), [stop]);
  return { recording, micError, start, stop };
}

// ---------------- Dars ----------------

interface Take {
  blob: Blob;
  url: string;
}

type Phase = 'idle' | 'playing' | 'pause';

export function ShadowingLessonPage({ id, go, loggedIn }: { id: string; go: (route: string) => void; loggedIn: boolean }) {
  const t = useT(shadowingMsg);
  const [lesson, setLesson] = useState<ShadowLesson | null>(null);
  const [error, setError] = useState('');
  const [index, setIndex] = useState(0);
  const [phase, setPhase] = useState<Phase>('idle');
  const [pauseMs, setPauseMs] = useState(0);
  const [pauseKey, setPauseKey] = useState(0);
  const [rate, setRate] = useState(1);
  const [auto, setAuto] = useState(false);
  const [loop, setLoop] = useState(false);
  const [speaker, setSpeaker] = useState<number | null>(null);
  const [practiced, setPracticed] = useState<Set<number>>(new Set());
  const [takes, setTakes] = useState<Record<number, Take>>({});
  const [checks, setChecks] = useState<Record<number, ShadowCheck>>({});
  const [checking, setChecking] = useState(false);
  const [checkError, setCheckError] = useState('');
  const [videoError, setVideoError] = useState(false);
  const [finished, setFinished] = useState('');
  const [player, setPlayer] = useState<LinePlayer | null>(null);

  const runRef = useRef(0);
  const settings = useRef({ rate, auto, loop });
  settings.current = { rate, auto, loop };
  const videoBox = useRef<HTMLDivElement | null>(null);
  const listRef = useRef<HTMLOListElement | null>(null);
  const recorder = useRecorder();

  useEffect(() => {
    apiJson<ShadowLesson>(`/api/shadowing/${id}`)
      .then(setLesson)
      .catch((e) => setError(e instanceof Error ? e.message : String(e)));
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
  useEffect(() => () => {
    runRef.current++;
    Object.values(takesRef.current).forEach((tk) => URL.revokeObjectURL(tk.url));
  }, []);

  // Joriy gap ro'yxatda ko'rinib tursin (sahifani emas, faqat ro'yxatni aylantiramiz).
  useEffect(() => {
    const list = listRef.current;
    const item = list?.children[index] as HTMLElement | undefined;
    if (!list || !item) return;
    if (item.offsetTop < list.scrollTop || item.offsetTop + item.offsetHeight > list.scrollTop + list.clientHeight) {
      list.scrollTop = item.offsetTop - list.clientHeight / 3;
    }
  }, [index]);

  const markPracticed = (i: number) => setPracticed((s) => (s.has(i) ? s : new Set(s).add(i)));

  const stopAll = useCallback(() => {
    runRef.current++;
    player?.stop();
    setPhase('idle');
  }, [player]);

  /** Gapni o'ynatadi; avto yoki takror rejimida — pauza va davom. `single` — faqat bir marta. */
  const playLine = useCallback(
    async (i: number, single = false) => {
      if (!player || !lesson) return;
      const my = ++runRef.current;
      recorder.stop();
      setIndex(i);
      setPhase('playing');
      await player.play(i, settings.current.rate);
      if (my !== runRef.current) return;
      const { auto: isAuto, loop: isLoop, rate: r } = settings.current;
      if (single || !(isAuto || isLoop)) {
        setPhase('idle');
        return;
      }
      const wait = repeatPauseMs(lesson.lines[i], r);
      setPauseMs(wait);
      setPauseKey((k) => k + 1);
      setPhase('pause');
      await sleep(wait);
      if (my !== runRef.current) return;
      if (isAuto) markPracticed(i);
      if (settings.current.loop) return playLine(i);
      const n = nextIndex(i, lesson.lines.length);
      if (n === null || !settings.current.auto) {
        setPhase('idle');
        return;
      }
      return playLine(n);
    },
    [player, lesson, recorder],
  );

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
  const scores = Object.values(checks).map((c) => c.score);
  const avg = average(scores);
  const need = Math.min(total, Math.max(3, Math.ceil(total / 2)));
  const busy = phase !== 'idle';

  async function record() {
    stopAll();
    setCheckError('');
    const i = index;
    const blob = await recorder.start(recordLimitMs(lesson!.lines[i]));
    if (!blob) return;
    setTakes((m) => {
      if (m[i]) URL.revokeObjectURL(m[i].url);
      return { ...m, [i]: { blob, url: URL.createObjectURL(blob) } };
    });
    setChecks((m) => {
      const { [i]: _old, ...rest } = m;
      return rest;
    });
    markPracticed(i);
  }

  async function aiCheck() {
    if (!take) return;
    setChecking(true);
    setCheckError('');
    const i = index;
    try {
      const form = new FormData();
      form.append('audio', take.blob, `line.${audioExt(take.blob.type)}`);
      form.append('lessonId', lesson!.id);
      form.append('line', String(i));
      const r = await apiJson<ShadowCheck>('/api/shadowing/check', { method: 'POST', body: form });
      setChecks((m) => ({ ...m, [i]: r }));
    } catch (e) {
      setCheckError(e instanceof Error ? e.message : String(e));
    } finally {
      setChecking(false);
    }
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

  const go1 = (i: number | null) => i !== null && playLine(i);

  return (
    <>
      <PageHeader title={lesson.title} onBack={() => go('shadowing')} backLabel={t.back} />
      <div className="shadow-meta muted small">
        <span className="badge">{lesson.level}</span> {t.lines(total)} · {t.tapLine}
      </div>

      <div className="card shadow-stage-card">
        {lesson.kind === 'youtube' ? (
          <div className="shadow-video">
            <div ref={videoBox} className="shadow-video-frame" data-testid="yt-box" />
            {videoError && <p className="error shadow-video-error" role="alert">{t.videoError}</p>}
          </div>
        ) : (
          <div className="shadow-stage" data-testid="stage">
            {lesson.characters.map((c, s) => (
              <div key={c} className={`shadow-actor${speaker === s ? ' speaking' : ''}`}>
                <Character name={c} talking={speaker === s} active={line.speaker === s} size={lesson.characters.length === 1 ? 150 : 118} label={t.names[c]} />
                <span className="small muted">{t.names[c]}</span>
              </div>
            ))}
          </div>
        )}
        <p className={`shadow-caption${phase === 'playing' ? ' live' : ''}`} data-testid="caption" aria-live="polite">
          {check
            ? check.words.map((w, k) => (
                <span key={k} className={w.ok ? 'w-ok' : 'w-bad'}>
                  {w.word}{' '}
                </span>
              ))
            : line.text}
        </p>
        {phase === 'pause' && (
          <div className="shadow-turn" data-testid="your-turn">
            <strong>{t.yourTurn}</strong>
            <div className="progress">
              <div key={pauseKey} className="shadow-countdown" style={{ animationDuration: `${pauseMs}ms` }} />
            </div>
          </div>
        )}

        <div className="shadow-controls" role="group" aria-label={t.play}>
          <button className="shadow-ctl" onClick={() => playLine(index, true)} disabled={!player} aria-label={t.replay}>
            <span aria-hidden="true">↺</span>
            <small>{t.replay}</small>
          </button>
          <button className="shadow-ctl" onClick={() => go1(index > 0 ? index - 1 : null)} disabled={!player || index === 0} aria-label={t.prev}>
            <span aria-hidden="true">⏮</span>
            <small>{t.prev}</small>
          </button>
          <button className="shadow-ctl main" onClick={() => (busy ? stopAll() : playLine(index))} disabled={!player} aria-label={busy ? t.pause : t.play}>
            <span aria-hidden="true">{busy ? '⏸' : '▶'}</span>
            <small>{busy ? t.pause : t.play}</small>
          </button>
          <button className="shadow-ctl" onClick={() => go1(nextIndex(index, total))} disabled={!player || index === total - 1} aria-label={t.next}>
            <span aria-hidden="true">⏭</span>
            <small>{t.next}</small>
          </button>
          <button className="shadow-ctl" aria-pressed={loop} onClick={() => setLoop((v) => !v)} aria-label={t.loop}>
            <span aria-hidden="true">🔁</span>
            <small>{t.loop}</small>
          </button>
        </div>

        <div className="shadow-options">
          <div className="chips" role="group" aria-label={t.speed}>
            {SPEEDS.map((s) => (
              <button key={s} aria-pressed={rate === s} onClick={() => setRate(s)}>{s}×</button>
            ))}
          </div>
          <label className="shadow-auto">
            <input type="checkbox" checked={auto} onChange={(e) => setAuto(e.target.checked)} />
            <span>
              <strong>{t.auto}</strong>
              <span className="muted small"> — {t.autoHint}</span>
            </span>
          </label>
        </div>
        {lesson.kind === 'youtube' && <p className="muted small" style={{ margin: 0 }}>{t.videoNote}</p>}
      </div>

      <div className="card stack shadow-record" data-testid="record-panel">
        <div className="spread">
          <strong>{t.lineN(index + 1)}</strong>
          {check && <span className={`shadow-score txt-${scoreKey(check.score)}`} data-testid="score">{t.score}: {check.score}</span>}
        </div>
        <div className="shadow-rec-buttons">
          {recorder.recording ? (
            <button className="btn btn-danger" onClick={recorder.stop}>{t.stopRec}</button>
          ) : (
            <button className="btn btn-primary" onClick={record}>{t.record}</button>
          )}
          <button className="btn btn-outline" onClick={() => playLine(index, true)} disabled={!player || recorder.recording}>{t.original}</button>
          {take && !recorder.recording && (
            <button className="btn btn-outline" onClick={() => void new Audio(take.url).play()}>{t.mine}</button>
          )}
          {take && !recorder.recording && (
            <button className="btn btn-teal" onClick={aiCheck} disabled={checking}>{checking ? t.checking : t.check}</button>
          )}
        </div>
        {recorder.recording && <p className="muted small shadow-rec-live" role="status">● {t.recording}</p>}
        {recorder.micError && <p className="error small" role="alert">{recorder.micError === 'denied' ? t.micDenied : t.noMic}</p>}
        {checkError && <p className="error small" role="alert">{checkError}</p>}
        {check && (
          <div className="shadow-result" data-testid="check-result">
            {check.heard && <p className="small" style={{ margin: 0 }}><span className="muted">{t.heard}:</span> <i>{check.heard}</i></p>}
            {check.tip && <p className="small" style={{ margin: 0 }}><span className="muted">{t.tip}:</span> {check.tip}</p>}
          </div>
        )}
      </div>

      <ol className="shadow-lines" ref={listRef} data-testid="lines">
        {lesson.lines.map((l, i) => (
          <li key={i}>
            <button className={`shadow-line${i === index ? ' current' : ''}`} aria-current={i === index ? 'true' : undefined} onClick={() => playLine(i, true)} disabled={!player}>
              <span className="n">{lesson.kind === 'youtube' && l.start != null ? clock(l.start) : i + 1}</span>
              {lesson.kind === 'character' && lesson.characters.length > 1 && (
                <span className="who" aria-hidden="true"><Character name={lesson.characters[l.speaker] ?? lesson.characters[0]} size={26} /></span>
              )}
              <span className="txt">{l.text}</span>
              {checks[i] ? (
                <span className={`mini txt-${scoreKey(checks[i].score)}`}>{checks[i].score}</span>
              ) : practiced.has(i) ? (
                <span className="mini" aria-label="✓">✓</span>
              ) : null}
            </button>
          </li>
        ))}
      </ol>

      <div className="card stack" data-testid="finish">
        <div className="spread">
          <span>{t.progress(practiced.size, total)}</span>
          {avg !== null && <span className="muted small">{t.avg(avg)}</span>}
        </div>
        <div className="progress"><div style={{ width: `${(practiced.size / total) * 100}%` }} /></div>
        {finished ? (
          <p className="shadow-finished" role="status">{finished}</p>
        ) : (
          <>
            <button className="btn btn-primary" onClick={finish} disabled={!canFinish(practiced.size, total)}>{t.finish}</button>
            {!canFinish(practiced.size, total) && <p className="muted small" style={{ margin: 0 }}>{t.finishHint(need)}</p>}
          </>
        )}
      </div>
    </>
  );
}
