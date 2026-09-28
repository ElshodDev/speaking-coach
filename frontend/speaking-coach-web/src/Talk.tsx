// AI suhbatdosh: vaziyat tanlanadi, foydalanuvchi ovoz (yoki matn) bilan javob beradi,
// AI rolda davom etadi (brauzer ovozida o'qiladi) va xatoni qisqa tuzatadi. Oxirida — xulosa.
import { useEffect, useRef, useState, type FormEvent } from 'react';
import { currentMicEnv, micProblemOf, type MicProblem } from './micErrors';
import { MicProblemNote } from './MicProblem';
import { apiJson, getLevel, postJson } from './api';
import { cardsMessage } from './cards';
import { common, useT } from './i18n';
import { TALK_SCENARIOS, talkMsg } from './locales/talk';
import { audioExt } from './shadowLogic';
import { isSpeechSupported, speakAsync, stopSpeaking } from './speech';
import { PageHeader } from './ui';

interface Correction {
  original: string;
  corrected: string;
  explanation: string;
}

export interface TalkMessage {
  who: 'ai' | 'user';
  text: string;
  tip?: Correction | null;
}

interface TurnResponse {
  heard: string;
  reply: string;
  tip: Correction | null;
  turn: number;
  maxTurns: number;
}

interface Summary {
  summary: string;
  strengths: string;
  topCorrections: Correction[];
  usefulPhrases: string[];
  nextFocus: string;
}

/** Daraja belgisi bo'yicha vaziyatlar: foydalanuvchi darajasidagilar birinchi. */
export function orderScenarios(level: string): { id: string; level: string }[] {
  const order = ['A2', 'B1', 'B2', 'C1'];
  const mine = order.indexOf(level);
  return [...TALK_SCENARIOS].sort((a, b) => {
    const da = Math.abs(order.indexOf(a.level) - mine);
    const db = Math.abs(order.indexOf(b.level) - mine);
    return da - db;
  });
}

export function TalkHub({ go }: { go: (route: string) => void }) {
  const t = useT(talkMsg);
  const level = getLevel();
  return (
    <>
      <PageHeader title={t.title} subtitle={t.subtitle} onBack={() => go('practice')} />
      <ul className="talk-grid" data-testid="talk-list">
        {orderScenarios(level).map((s) => {
          const sc = t.scenarios[s.id];
          return (
            <li key={s.id}>
              <button className="pick-card" onClick={() => go(`talk/${s.id}`)} data-testid="talk-scenario">
                <span className="pick-head">
                  <span className="pick-num">{sc?.title ?? s.id}</span>
                  <span className={`badge${s.level === level ? ' badge-done' : ''}`}>{s.level}</span>
                </span>
                <span className="muted small">{sc?.text}</span>
                <span className="pick-go">{t.start} →</span>
              </button>
            </li>
          );
        })}
      </ul>
      <p className="muted tiny" style={{ marginTop: 12 }}>{t.levelNote(8)}</p>
    </>
  );
}

type Phase = 'idle' | 'starting' | 'chat' | 'recording' | 'thinking' | 'finishing' | 'done';

export function TalkSession({
  scenario,
  loggedIn,
  onLogin,
  onCardsAdded,
  go,
}: {
  scenario: string;
  loggedIn: boolean;
  onLogin: () => void;
  onCardsAdded?: () => void;
  go: (route: string) => void;
}) {
  const t = useT(talkMsg);
  const cm = useT(common);
  const sc = t.scenarios[scenario];
  const [phase, setPhase] = useState<Phase>('idle');
  const [talkId, setTalkId] = useState('');
  const [messages, setMessages] = useState<TalkMessage[]>([]);
  const [turn, setTurn] = useState(0);
  const [maxTurns, setMaxTurns] = useState(8);
  const [typing, setTyping] = useState(false);
  const [text, setText] = useState('');
  const [error, setError] = useState('');
  const [micProblem, setMicProblem] = useState<MicProblem | null>(null);
  const [summary, setSummary] = useState<Summary | null>(null);
  const [cardsNote, setCardsNote] = useState('');
  const recorderRef = useRef<MediaRecorder | null>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const chunksRef = useRef<Blob[]>([]);
  const aliveRef = useRef(true);
  const endRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    aliveRef.current = true;
    return () => {
      aliveRef.current = false;
      stopSpeaking();
      if (recorderRef.current?.state === 'recording') recorderRef.current.stop();
      streamRef.current?.getTracks().forEach((tr) => tr.stop());
    };
  }, []);

  useEffect(() => {
    endRef.current?.scrollIntoView?.({ behavior: 'smooth', block: 'end' });
  }, [messages.length, phase]);

  const say = (line: string) => {
    if (isSpeechSupported()) void speakAsync(line, 0.95);
  };

  async function start() {
    setPhase('starting');
    setError('');
    try {
      const d = await postJson<{ talkId: string; opening: string; maxTurns: number }>('/api/talk/start', { scenario, level: getLevel() });
      setTalkId(d.talkId);
      setMaxTurns(d.maxTurns);
      setTurn(0);
      setMessages([{ who: 'ai', text: d.opening }]);
      setSummary(null);
      setPhase('chat');
      say(d.opening);
    } catch (e) {
      setError(e instanceof Error ? e.message : cm.error);
      setPhase('idle');
    }
  }

  async function sendTurn(form: FormData) {
    setPhase('thinking');
    setError('');
    stopSpeaking();
    try {
      const d = await apiJson<TurnResponse>(`/api/talk/${talkId}/turn`, { method: 'POST', body: form });
      if (!aliveRef.current) return;
      setMessages((m) => [...m, { who: 'user', text: d.heard, tip: d.tip }, { who: 'ai', text: d.reply }]);
      setTurn(d.turn);
      setText('');
      setPhase('chat');
      say(d.reply);
    } catch (e) {
      setError(e instanceof Error ? e.message : cm.error);
      setPhase('chat');
    }
  }

  async function startRecording() {
    setError('');
    setMicProblem(null);
    stopSpeaking();
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      if (!aliveRef.current) {
        stream.getTracks().forEach((tr) => tr.stop());
        return;
      }
      streamRef.current = stream;
      const rec = new MediaRecorder(stream);
      chunksRef.current = [];
      rec.ondataavailable = (ev) => {
        if (ev.data.size > 0) chunksRef.current.push(ev.data);
      };
      rec.onstop = () => {
        const blob = new Blob(chunksRef.current, { type: rec.mimeType || 'audio/webm' });
        stream.getTracks().forEach((tr) => tr.stop());
        streamRef.current = null;
        if (!aliveRef.current) return;
        const form = new FormData();
        form.append('audio', blob, `answer.${audioExt(blob.type)}`);
        void sendTurn(form);
      };
      rec.start();
      recorderRef.current = rec;
      setPhase('recording');
    } catch (err) {
      // Mikrofon bo'lmasa ham suhbat to'xtamaydi — yozib javob berish rejimiga o'tadi.
      setMicProblem(micProblemOf(err, currentMicEnv()));
      setTyping(true);
    }
  }

  function stopRecording() {
    recorderRef.current?.stop();
    setPhase('thinking');
  }

  function sendText(e: FormEvent) {
    e.preventDefault();
    const v = text.trim();
    if (!v) return;
    const form = new FormData();
    form.append('text', v);
    void sendTurn(form);
  }

  async function finish() {
    stopSpeaking();
    setPhase('finishing');
    setError('');
    try {
      const d = await postJson<{ result: Summary; newCards: number }>(`/api/talk/${talkId}/finish`, {});
      setSummary(d.result);
      setCardsNote(d.newCards > 0 ? cardsMessage(d.newCards) : '');
      if (d.newCards > 0) onCardsAdded?.();
      setPhase('done');
    } catch (e) {
      setError(e instanceof Error ? e.message : cm.error);
      setPhase('chat');
    }
  }

  const header = <PageHeader title={sc?.title ?? t.title} subtitle={sc?.text} onBack={() => go('talk')} backLabel={t.back} />;

  if (!loggedIn) {
    return (
      <>
        {header}
        <div className="card center">
          <p>{t.loginText}</p>
          <button className="btn btn-primary" onClick={onLogin}>{t.login}</button>
        </div>
      </>
    );
  }

  if (phase === 'idle' || phase === 'starting') {
    return (
      <>
        {header}
        <div className="card stack">
          <p className="muted small" style={{ margin: 0 }}>{t.levelNote(8)}</p>
          {error && <p className="error small" role="alert">{error}</p>}
          <button className="btn btn-primary" onClick={start} disabled={phase === 'starting'} data-testid="talk-start">
            {phase === 'starting' ? t.starting : `🎙 ${t.start}`}
          </button>
        </div>
      </>
    );
  }

  const busy = phase === 'thinking' || phase === 'finishing';
  const outOfTurns = turn >= maxTurns;
  return (
    <>
      {header}
      <div className="talk-chat" data-testid="talk-chat" aria-live="polite">
        {messages.map((m, i) => (
          <div key={i} className={`talk-row ${m.who}`}>
            <div className={`bubble ${m.who}`}>
              <span className="tiny muted who">{m.who === 'ai' ? t.partner : t.you}</span>
              <span>{m.text}</span>
              {m.who === 'ai' && isSpeechSupported() && (
                <button className="btn-link tiny" onClick={() => say(m.text)} aria-label={t.listenAgain}>🔊</button>
              )}
            </div>
            {m.tip && (
              <div className="talk-tip small" data-testid="talk-tip">
                💡 <s>{m.tip.original}</s> → <strong>{m.tip.corrected}</strong>
                {m.tip.explanation && <div className="muted tiny">{m.tip.explanation}</div>}
              </div>
            )}
          </div>
        ))}
        {busy && <div className="talk-row ai"><div className="bubble ai typing" aria-label={phase === 'finishing' ? t.finishing : t.thinking}><span /><span /><span /></div></div>}
        <div ref={endRef} />
      </div>

      {phase !== 'done' && (
        <div className="card talk-controls">
          <div className="spread small">
            <span className="muted">{t.turns(turn, maxTurns)}</span>
            <button className="btn-link small" onClick={() => setTyping((v) => !v)} disabled={busy || phase === 'recording'}>
              {typing ? t.record : t.typeInstead}
            </button>
          </div>
          {error && <p className="error small" role="alert">{error}</p>}
          {micProblem && <MicProblemNote problem={micProblem} />}
          {outOfTurns && <p className="small muted">{t.limitReached}</p>}
          {!outOfTurns && !typing && (
            phase === 'recording' ? (
              <>
                <p className="small" style={{ margin: 0 }}><span className="rec-dot" aria-hidden /> {t.recording}</p>
                <button className="btn btn-danger block" onClick={stopRecording} data-testid="talk-stop">{t.stop}</button>
              </>
            ) : (
              <button className="btn btn-primary block" onClick={startRecording} disabled={busy} data-testid="talk-record">
                {t.record}
              </button>
            )
          )}
          {!outOfTurns && typing && (
            <form className="row" style={{ gap: 8 }} onSubmit={sendText}>
              <input
                className="input"
                value={text}
                onChange={(e) => setText(e.target.value)}
                placeholder={t.typePlaceholder}
                aria-label={t.typePlaceholder}
                maxLength={600}
                enterKeyHint="send"
                autoCapitalize="sentences"
                disabled={busy}
                data-testid="talk-text"
              />
              <button className="btn btn-primary" disabled={busy || !text.trim()}>{t.send}</button>
            </form>
          )}
          {turn > 0 && (
            <button className={outOfTurns ? 'btn btn-primary block' : 'btn btn-outline block'} onClick={finish} disabled={busy || phase === 'recording'} data-testid="talk-finish">
              {phase === 'finishing' ? t.finishing : t.finish}
            </button>
          )}
        </div>
      )}

      {summary && (
        <div className="card stack" data-testid="talk-summary">
          <h3 style={{ margin: 0 }}>{t.summaryTitle}</h3>
          <p style={{ margin: 0 }}>{summary.summary}</p>
          {summary.strengths && (
            <p className="small" style={{ margin: 0 }}><strong>✅ {t.strengths}:</strong> {summary.strengths}</p>
          )}
          {summary.topCorrections.length > 0 && (
            <div>
              <strong className="small">✏️ {t.corrections}</strong>
              <ul className="mistake-examples">
                {summary.topCorrections.map((c, i) => (
                  <li key={i}>
                    <div className="break"><s>{c.original}</s> → <strong>{c.corrected}</strong></div>
                    {c.explanation && <div className="muted tiny">{c.explanation}</div>}
                  </li>
                ))}
              </ul>
            </div>
          )}
          {summary.usefulPhrases.length > 0 && (
            <div>
              <strong className="small">💬 {t.phrases}</strong>
              <ul className="talk-phrases">
                {summary.usefulPhrases.map((p, i) => (
                  <li key={i}>
                    <span>{p}</span>
                    {isSpeechSupported() && <button className="btn-link tiny" onClick={() => say(p)} aria-label={t.listenAgain}>🔊</button>}
                  </li>
                ))}
              </ul>
            </div>
          )}
          {summary.nextFocus && <p className="small" style={{ margin: 0 }}><strong>🎯 {t.nextFocus}:</strong> {summary.nextFocus}</p>}
          {cardsNote && <p className="success small" style={{ margin: 0 }}>{cardsNote}</p>}
          <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
            <button className="btn btn-primary" onClick={start}>{t.again}</button>
            <button className="btn btn-outline" onClick={() => go('talk')}>{t.other}</button>
          </div>
        </div>
      )}
    </>
  );
}
