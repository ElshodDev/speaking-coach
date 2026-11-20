import { useCallback, useEffect, useRef, useState } from 'react';
import { apiJson, getLevel } from './api';
import { cardsMessage } from './cards';
import { FluencyCard } from './Fluency';
import { measurePauses } from './fluencyAudio';
import { msg, useT } from './i18n';
import { useLeaveGuard } from './leaveGuard';
import { part2Msg } from './locales/part2';
import { currentMicEnv, micPrecheck, micProblemOf, type MicProblem } from './micErrors';
import { MicProblemNote } from './MicProblem';
import { clock, coveragePercent, nextCard, speakingHint, timeVerdict, type Part2Card, type Part2Result, type Phase } from './part2Logic';
import { audioExt } from './shadowLogic';
import { Corrections, Feedback, GuestNote, PageHeader, ScoreBar } from './ui';
import { speakingMsg } from './locales/speaking';

interface CardsResponse {
  prepSeconds: number;
  speakSeconds: number;
  cards: Part2Card[];
}

/** Kartochka ko'rinishi — imtihondagidek: mavzu, "You should say", yakuniy "and explain…". */
function CueCardView({ card }: { card: Part2Card }) {
  const t = useT(part2Msg);
  return (
    <div className="card" style={{ borderLeft: '4px solid var(--primary)' }} data-testid="cue-card">
      <strong style={{ fontSize: '1.05rem' }}>{card.topic}</strong>
      <p className="small" style={{ margin: '10px 0 4px' }}>{t.youShouldSay}</p>
      <ul className="small" style={{ margin: 0, paddingLeft: 20 }}>
        {card.points.map((p) => (
          <li key={p}>{p}</li>
        ))}
      </ul>
      <p className="small" style={{ margin: '6px 0 0' }}>{card.explain}</p>
    </div>
  );
}

/**
 * IELTS Speaking Part 2 trener. Mikrofon tayyorgarlik boshida so'raladi (tugma bosilishi — foydalanuvchi
 * harakati), tayyorgarlik tugashi bilan yozish o'zi boshlanadi va 2 daqiqada o'zi to'xtaydi.
 */
export function Part2Page({ loggedIn, onLogin, onCardsAdded, go, cardId }: {
  loggedIn: boolean;
  onLogin: () => void;
  onCardsAdded?: () => void;
  go: (route: string) => void;
  cardId?: string;
}) {
  const t = useT(part2Msg);
  const ts = useT(speakingMsg);
  const [data, setData] = useState<CardsResponse | null>(null);
  const [loadError, setLoadError] = useState('');
  const [card, setCard] = useState<Part2Card | null>(null);
  const [phase, setPhase] = useState<Phase>('pick');
  const [left, setLeft] = useState(0);
  const [elapsed, setElapsed] = useState(0);
  const [notes, setNotes] = useState('');
  const [result, setResult] = useState<Part2Result | null>(null);
  const [error, setError] = useState('');
  const [micProblem, setMicProblem] = useState<MicProblem | null>(null);
  const [starting, setStarting] = useState(false);

  const streamRef = useRef<MediaStream | null>(null);
  const recorderRef = useRef<MediaRecorder | null>(null);
  const chunksRef = useRef<Blob[]>([]);
  const startedAtRef = useRef(0);
  const blobRef = useRef<{ blob: Blob; seconds: number } | null>(null);
  const aliveRef = useRef(true);

  const prepSeconds = data?.prepSeconds ?? 60;
  const speakSeconds = data?.speakSeconds ?? 120;

  // Imtihon davomida chiqib ketish — avval so'raymiz (yozuv yo'qolmasin).
  useLeaveGuard(phase === 'prep' || phase === 'speak', msg(part2Msg).leaveWarning);

  const stopStream = useCallback(() => {
    streamRef.current?.getTracks().forEach((tr) => tr.stop());
    streamRef.current = null;
  }, []);

  useEffect(() => {
    aliveRef.current = true;
    apiJson<CardsResponse>('/api/part2/cards')
      .then((d) => {
        if (!aliveRef.current) return;
        setData(d);
        if (cardId) setCard(d.cards.find((c) => c.id === cardId) ?? null);
      })
      .catch((e) => aliveRef.current && setLoadError(e instanceof Error ? e.message : msg(part2Msg).loadFailed));
    return () => {
      aliveRef.current = false;
      if (recorderRef.current?.state === 'recording') {
        recorderRef.current.onstop = null;
        recorderRef.current.stop();
      }
      streamRef.current?.getTracks().forEach((tr) => tr.stop());
    };
  }, [cardId]);

  async function submit(blob: Blob, seconds: number) {
    if (!card) return;
    setPhase('uploading');
    setError('');
    try {
      const pauses = await measurePauses(blob);
      const form = new FormData();
      form.append('audio', blob, `part2.${audioExt(blob.type)}`);
      form.append('cardId', card.id);
      form.append('level', getLevel());
      form.append('seconds', seconds.toFixed(1));
      if (pauses) {
        form.append('longPauses', String(pauses.longPauses));
        form.append('pauseSeconds', String(pauses.pauseSeconds));
      }
      const r = await apiJson<Part2Result>('/api/part2/submit', { method: 'POST', body: form });
      if (!aliveRef.current) return;
      setResult(r);
      setPhase('result');
      if (r.newCards > 0) onCardsAdded?.();
      // Ro'yxatda "bajarilgan" belgisi darhol ko'rinsin.
      setData((d) => (d ? { ...d, cards: d.cards.map((c) => (c.id === card.id ? { ...c, done: r.saved || c.done } : c)) } : d));
    } catch (e) {
      if (!aliveRef.current) return;
      setError(e instanceof Error ? e.message : ts.uploadFailed);
      setPhase('error');
    }
  }

  const startSpeaking = useCallback(() => {
    const stream = streamRef.current;
    if (!stream || recorderRef.current?.state === 'recording') return;
    const rec = new MediaRecorder(stream);
    chunksRef.current = [];
    rec.ondataavailable = (e) => {
      if (e.data.size > 0) chunksRef.current.push(e.data);
    };
    rec.onstop = () => {
      const seconds = (Date.now() - startedAtRef.current) / 1000;
      const blob = new Blob(chunksRef.current, { type: rec.mimeType || 'audio/webm' });
      stopStream();
      if (!aliveRef.current) return;
      blobRef.current = { blob, seconds };
      void submit(blob, seconds);
    };
    recorderRef.current = rec;
    startedAtRef.current = Date.now();
    rec.start();
    setElapsed(0);
    setPhase('speak');
  }, [stopStream, card]);

  const finishSpeaking = useCallback(() => {
    if (recorderRef.current?.state === 'recording') recorderRef.current.stop();
  }, []);

  // Tayyorgarlik taymeri: 0 ga yetsa — yozish o'zi boshlanadi.
  useEffect(() => {
    if (phase !== 'prep') return;
    const end = Date.now() + prepSeconds * 1000;
    setLeft(prepSeconds);
    const id = setInterval(() => {
      const l = Math.ceil((end - Date.now()) / 1000);
      setLeft(Math.max(0, l));
      if (l <= 0) {
        clearInterval(id);
        startSpeaking();
      }
    }, 250);
    return () => clearInterval(id);
  }, [phase, prepSeconds, startSpeaking]);

  // Gapirish taymeri (devor soati bo'yicha — fon rejimida ham to'g'ri): 2 daqiqada o'zi to'xtaydi.
  useEffect(() => {
    if (phase !== 'speak') return;
    const id = setInterval(() => {
      const e = (Date.now() - startedAtRef.current) / 1000;
      setElapsed(e);
      if (e >= speakSeconds) {
        clearInterval(id);
        finishSpeaking();
      }
    }, 250);
    return () => clearInterval(id);
  }, [phase, speakSeconds, finishSpeaking]);

  async function startPrep() {
    if (starting || !card) return;
    setMicProblem(null);
    const env = currentMicEnv();
    const pre = micPrecheck(env);
    if (pre) {
      setMicProblem(pre);
      return;
    }
    setStarting(true);
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      if (!aliveRef.current) {
        stream.getTracks().forEach((tr) => tr.stop());
        return;
      }
      stopStream();
      streamRef.current = stream;
      setNotes('');
      setResult(null);
      setPhase('prep');
    } catch (err) {
      if (aliveRef.current) setMicProblem(micProblemOf(err, env));
    } finally {
      if (aliveRef.current) setStarting(false);
    }
  }

  function choose(c: Part2Card | null) {
    if (!c) return;
    stopStream();
    setCard(c);
    setResult(null);
    setError('');
    setPhase('pick');
    go(`part2/${c.id}`);
  }

  function backToList() {
    stopStream();
    setCard(null);
    setPhase('pick');
    go('part2');
  }

  const header = (
    <PageHeader
      title={t.title}
      subtitle={t.subtitle}
      onBack={phase === 'prep' || phase === 'speak' || phase === 'uploading' ? undefined : () => (card ? backToList() : go('practice'))}
    />
  );

  if (loadError) return <>{header}<p className="error" role="alert">{loadError}</p></>;
  if (!data) return <>{header}<p className="muted" role="status">…</p></>;

  // ---- Kartochka tanlash ----
  if (!card) {
    return (
      <>
        {header}
        <GuestNote loggedIn={loggedIn} onLogin={onLogin} />
        <button className="btn btn-primary block" onClick={() => choose(nextCard(data.cards, null))} style={{ marginBottom: 12 }} data-testid="part2-random">
          {t.random}
        </button>
        <details className="card" open>
          <summary className="small"><strong>{t.allCards(data.cards.length)}</strong></summary>
          <ul className="stack" style={{ listStyle: 'none', padding: 0, margin: '10px 0 0' }}>
            {data.cards.map((c) => (
              <li key={c.id}>
                <button className="btn btn-outline block" style={{ textAlign: 'left', justifyContent: 'space-between' }} onClick={() => choose(c)} data-testid="part2-card">
                  <span>{c.topic}</span>
                  {c.done && <span className="badge">{t.done}</span>}
                </button>
              </li>
            ))}
          </ul>
        </details>
      </>
    );
  }

  const verdict = result ? timeVerdict(blobRef.current?.seconds ?? result.metrics?.seconds ?? 0) : null;
  const spoken = blobRef.current?.seconds ?? result?.metrics?.seconds ?? 0;

  return (
    <>
      {header}
      <div className="practice-split">
        <div>
          <CueCardView card={card} />

          {phase === 'pick' && (
            <div className="card">
              <button className="btn btn-primary block" onClick={startPrep} disabled={starting} style={{ minHeight: 52 }} data-testid="part2-start">
                {t.startPrep}
              </button>
              <p className="muted tiny" style={{ marginBottom: 0 }}>{t.micNote}</p>
              {micProblem && <MicProblemNote problem={micProblem} onRetry={startPrep} />}
            </div>
          )}

          {(phase === 'prep' || phase === 'speak') && (
            <div className="card">
              <label className="small" htmlFor="part2-notes">{t.notes}</label>
              <textarea
                id="part2-notes"
                rows={5}
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                placeholder={t.notesPlaceholder}
                readOnly={phase === 'speak'}
                style={{ width: '100%', marginTop: 6 }}
              />
            </div>
          )}
        </div>

        <div>
          {phase === 'prep' && (
            <div className="card center" data-testid="part2-prep">
              <h3 style={{ marginTop: 0 }}>{t.prepTitle}</h3>
              <div style={{ fontSize: '2.4rem', fontWeight: 700 }} aria-live="polite">{clock(left)}</div>
              <div className="progress" style={{ margin: '12px 0' }}>
                <div style={{ width: `${(100 * (prepSeconds - left)) / prepSeconds}%` }} />
              </div>
              <button className="btn btn-primary block" onClick={startSpeaking} data-testid="part2-speak-now">{t.speakNow}</button>
            </div>
          )}

          {phase === 'speak' && (
            <div className="card center" data-testid="part2-speak">
              <h3 style={{ marginTop: 0 }}>{t.speakTitle}</h3>
              <div style={{ fontSize: '2.4rem', fontWeight: 700 }}>{clock(speakSeconds - elapsed)}</div>
              <p className="small" style={{ margin: '4px 0' }}>
                <span className="recording-dot" /> {t.recording(clock(elapsed), clock(speakSeconds))}
              </p>
              <div className="progress" style={{ margin: '12px 0' }}>
                <div style={{ width: `${Math.min(100, (100 * elapsed) / speakSeconds)}%` }} />
              </div>
              <p className="small" aria-live="polite">{t.hints[speakingHint(elapsed, speakSeconds)]}</p>
              <button className="btn btn-danger block" onClick={finishSpeaking} data-testid="part2-finish">{t.finish}</button>
            </div>
          )}

          {phase === 'uploading' && (
            <div className="card center" role="status">
              <p>{t.evaluating}</p>
            </div>
          )}

          {phase === 'error' && (
            <div className="card">
              <p className="error" role="alert">{error}</p>
              <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
                {blobRef.current && (
                  <button className="btn btn-primary" onClick={() => blobRef.current && submit(blobRef.current.blob, blobRef.current.seconds)}>{t.retry}</button>
                )}
                <button className="btn btn-outline" onClick={() => setPhase('pick')}>{t.again}</button>
              </div>
            </div>
          )}

          {phase === 'result' && result && (
            <div className="card" data-testid="part2-result">
              <div className="stats">
                <div className="stat">
                  <div className="value">{result.evaluation.estimatedBand.toFixed(1)}</div>
                  <div className="label">{t.band}</div>
                </div>
                <div className="stat">
                  <div className={`value txt-${verdict === 'good' ? 'good' : verdict === 'fair' ? 'mid' : 'low'}`}>{clock(spoken)}</div>
                  <div className="label">{t.time}</div>
                </div>
                <div className="stat">
                  <div className="value">{coveragePercent(result.evaluation.coverage)}%</div>
                  <div className="label">{t.coverageLabel}</div>
                </div>
              </div>
              <p className="muted tiny">{t.bandNote}</p>
              {verdict && <p className="small">{t.timeVerdict[verdict]}</p>}

              <h3>{t.coverage(coveragePercent(result.evaluation.coverage))}</h3>
              <ul className="stack" style={{ listStyle: 'none', padding: 0 }} data-testid="part2-coverage">
                {result.evaluation.coverage.map((c) => (
                  <li key={c.point} className="small">
                    <strong>{t.status[c.status]}</strong> — {c.point}
                    {c.evidence && <div className="muted tiny">“{c.evidence}”</div>}
                  </li>
                ))}
              </ul>

              <ScoreBar label={ts.criteria.fluency} data={result.evaluation.fluency} />
              <ScoreBar label={ts.criteria.grammar} data={result.evaluation.grammar} />
              <ScoreBar label={ts.criteria.vocabulary} data={result.evaluation.vocabulary} />
              {result.metrics && <FluencyCard metrics={result.metrics} />}
              <Corrections items={result.evaluation.topCorrections} />
              <Feedback encouragement={result.evaluation.encouragement} nextFocus={result.evaluation.nextFocus} />

              <details style={{ marginTop: 10 }}>
                <summary className="small">{t.transcript}</summary>
                <p className="quote">{result.evaluation.transcript}</p>
              </details>
              {result.newCards > 0 && <p className="success small">{cardsMessage(result.newCards)}</p>}
              <div className="row" style={{ gap: 8, flexWrap: 'wrap', marginTop: 10 }}>
                <button className="btn btn-outline" onClick={() => setPhase('pick')} data-testid="part2-again">{t.again}</button>
                <button className="btn btn-primary" onClick={() => choose(nextCard(data.cards, card.id))} data-testid="part2-next">{t.next}</button>
              </div>
            </div>
          )}
        </div>
      </div>
    </>
  );
}
