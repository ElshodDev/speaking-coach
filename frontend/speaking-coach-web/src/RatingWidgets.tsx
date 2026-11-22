import { useEffect, useRef, useState } from 'react';
import { postJson } from './api';
import { FeedbackDialog } from './Footer';
import { Icon } from './Icon';
import { useT } from './i18n';
import { ratingMsg } from './locales/rating';
import {
  AI_VERDICTS,
  RATE_FIRST_EVENT,
  SOURCES,
  aiKey,
  firstPracticeKey,
  lessonKey,
  markAsked,
  promptsEnabled,
  ratingPage,
  sendRating,
  sourceKey,
  wasAsked,
  type AiSkill,
  type AiVerdict,
  type SourceId,
} from './ratings';

const post = (path: string, body: unknown) => postJson(path, body);

/**
 * Birinchi mashq tugagach (har qurilmada bir marta): "Bu mashq foydali bo'ldimi?" — 2 ta bosish.
 * "Unchalik emas" bosilsa, ixtiyoriy izoh so'raladi (mavjud /api/feedback orqali, adminga Telegram xabari bilan).
 */
export function RatePrompt() {
  const t = useT(ratingMsg);
  const [open, setOpen] = useState(false);
  const [phase, setPhase] = useState<'ask' | 'why' | 'thanks'>('ask');
  const [text, setText] = useState('');
  const [busy, setBusy] = useState(false);
  const timers = useRef<number[]>([]);

  useEffect(() => {
    if (!promptsEnabled()) return;
    const on = () => {
      if (wasAsked('first')) return;
      // Natijani o'qib olishga vaqt beramiz.
      timers.current.push(window.setTimeout(() => setOpen(true), 2500));
    };
    window.addEventListener(RATE_FIRST_EVENT, on);
    const list = timers.current;
    return () => {
      window.removeEventListener(RATE_FIRST_EVENT, on);
      list.forEach((id) => window.clearTimeout(id));
    };
  }, []);

  // Javobsiz qolsa (15 soniya) o'zi yopiladi va qayta so'ralmaydi: natijani o'qishga xalaqit bermasin.
  useEffect(() => {
    if (!open || phase !== 'ask') return;
    const id = window.setTimeout(() => {
      markAsked('first');
      setOpen(false);
    }, 15000);
    return () => window.clearTimeout(id);
  }, [open, phase]);

  if (!open) return null;

  function close() {
    markAsked('first');
    setOpen(false);
  }

  function thank() {
    setPhase('thanks');
    timers.current.push(window.setTimeout(close, 1800));
  }

  async function answer(up: boolean) {
    markAsked('first');
    void sendRating(firstPracticeKey(up), post);
    if (up) thank();
    else setPhase('why');
  }

  async function sendWhy() {
    const message = text.trim();
    if (message.length >= 3) {
      setBusy(true);
      try {
        await postJson('/api/feedback', { kind: 'other', message: `[birinchi mashq] ${message}`.slice(0, 2000), page: ratingPage(window.location.hash) });
      } catch {
        // Yuborib bo'lmasa ham — rahmat deymiz; foydalanuvchini bezovta qilmaymiz.
      }
      setBusy(false);
    }
    thank();
  }

  return (
    <div className="rate-sheet card" role="region" aria-label={t.firstTitle} data-testid="rate-first">
      {phase === 'ask' && (
        <>
          <div className="spread">
            <strong>{t.firstTitle}</strong>
            <button type="button" className="btn-link" onClick={close} aria-label={t.close}>
              <Icon name="x" size={18} />
            </button>
          </div>
          <div className="rate-row">
            <button type="button" className="btn btn-outline" onClick={() => void answer(true)} data-testid="rate-up">
              <Icon name="thumbUp" size={20} /> {t.up}
            </button>
            <button type="button" className="btn btn-outline" onClick={() => void answer(false)} data-testid="rate-down">
              <Icon name="thumbDown" size={20} /> {t.down}
            </button>
          </div>
        </>
      )}
      {phase === 'why' && (
        <>
          <label className="small" htmlFor="rate-why">{t.whyQ}</label>
          <textarea id="rate-why" className="input" rows={3} maxLength={500} value={text} placeholder={t.whyPlaceholder} onChange={(e) => setText(e.target.value)} />
          <div className="rate-row">
            <button type="button" className="btn btn-primary" onClick={() => void sendWhy()} disabled={busy} data-testid="rate-why-send">{t.send}</button>
            <button type="button" className="btn btn-outline" onClick={thank} disabled={busy}>{t.skip}</button>
          </div>
        </>
      )}
      {phase === 'thanks' && <p className="success" role="status" style={{ margin: 0 }}>{t.thanks}</p>}
    </div>
  );
}

/**
 * AI natijasi ostida: "Baho adolatlimi?" (Adolatli / Yuqori / Past). Ota komponent `key={submissionId}` beradi,
 * shuning uchun har natijaga alohida baho beriladi. Maqsad — AI bahosi odamniki bilan qanchalik mosligini o'lchash.
 */
export function AiFairness({ skill }: { skill: AiSkill }) {
  const t = useT(ratingMsg);
  const [picked, setPicked] = useState<AiVerdict | null>(null);
  if (!promptsEnabled()) return null;

  const label: Record<AiVerdict, string> = { fair: t.aiFair, high: t.aiHigh, low: t.aiLow };

  return (
    <div className="card ai-fair" data-testid="ai-fair">
      {picked === null ? (
        <>
          <strong className="small">{t.aiTitle}</strong>
          <div className="rate-row">
            {AI_VERDICTS.map((v) => (
              <button
                key={v}
                type="button"
                className="btn btn-outline"
                onClick={() => {
                  setPicked(v);
                  void sendRating(aiKey(skill, v), post);
                }}
                data-testid={`ai-fair-${v}`}
              >
                {label[v]}
              </button>
            ))}
          </div>
          <p className="muted tiny" style={{ margin: 0 }}>{t.aiHint}</p>
        </>
      ) : (
        <p className="success small" role="status" style={{ margin: 0 }}>{t.aiThanks}</p>
      )}
    </div>
  );
}

/** Bosh sahifada (bir marta): "Bizni qayerdan topdingiz?" — bir bosish, o'tkazib yuborish mumkin. */
export function SourceCard() {
  const t = useT(ratingMsg);
  const [hidden, setHidden] = useState(() => !promptsEnabled() || wasAsked('source'));
  const [thanked, setThanked] = useState(false);
  if (hidden) return null;

  function pick(source: SourceId) {
    markAsked('source');
    void sendRating(sourceKey(source), post);
    setThanked(true);
    window.setTimeout(() => setHidden(true), 1500);
  }

  function skip() {
    markAsked('source');
    setHidden(true);
  }

  return (
    <div className="card stack source-card" data-testid="source-card">
      {thanked ? (
        <p className="success" role="status" style={{ margin: 0 }}>{t.thanks}</p>
      ) : (
        <>
          <div>
            <strong>{t.sourceTitle}</strong>
            <div className="muted small">{t.sourceHint}</div>
          </div>
          <div className="chips-row">
            {SOURCES.map((s) => (
              <button key={s} type="button" className="chip-btn" onClick={() => pick(s)} data-testid={`source-${s}`}>
                {t.sources[s]}
              </button>
            ))}
          </div>
          <button type="button" className="btn-link small" style={{ alignSelf: 'flex-start' }} onClick={skip}>{t.skip}</button>
        </>
      )}
    </div>
  );
}

/** Sahifa oxiridagi "Fikr bildirish" havolasi (footer telefonda ishchi ekranlarda yashirin). */
export function FeedbackLink() {
  const t = useT(ratingMsg);
  const [open, setOpen] = useState(false);
  return (
    <div className="feedback-link">
      <button type="button" className="btn-link small" onClick={() => setOpen(true)} data-testid="feedback-link">
        {t.feedbackLink}
      </button>
      {open && <FeedbackDialog onClose={() => setOpen(false)} />}
    </div>
  );
}

/**
 * AI ustoz darsi natijasi ostida: "Dars foydali bo'ldimi?" (bir bosish). "Unchalik emas" bo'lsa — ixtiyoriy izoh
 * (mavjud /api/feedback orqali). Ota komponent `key` bilan har dars uchun alohida holat beradi.
 */
export function LessonRating({ topic }: { topic: string }) {
  const t = useT(ratingMsg);
  const [phase, setPhase] = useState<'ask' | 'why' | 'thanks'>('ask');
  const [text, setText] = useState('');
  const [busy, setBusy] = useState(false);
  if (!promptsEnabled()) return null;

  async function sendWhy() {
    const message = text.trim();
    if (message.length >= 3) {
      setBusy(true);
      try {
        await postJson('/api/feedback', { kind: 'other', message: `[AI ustoz: ${topic}] ${message}`.slice(0, 2000), page: ratingPage(window.location.hash) });
      } catch {
        // Yuborib bo'lmasa ham rahmat deymiz.
      }
      setBusy(false);
    }
    setPhase('thanks');
  }

  return (
    <div className="stack" data-testid="lesson-rating">
      {phase === 'ask' && (
        <>
          <strong className="small">{t.lessonTitle}</strong>
          <div className="rate-row">
            <button type="button" className="btn btn-outline" data-testid="lesson-up" onClick={() => { void sendRating(lessonKey(true), post); setPhase('thanks'); }}>
              <Icon name="thumbUp" size={20} /> {t.lessonUp}
            </button>
            <button type="button" className="btn btn-outline" data-testid="lesson-down" onClick={() => { void sendRating(lessonKey(false), post); setPhase('why'); }}>
              <Icon name="thumbDown" size={20} /> {t.lessonDown}
            </button>
          </div>
        </>
      )}
      {phase === 'why' && (
        <>
          <label className="small" htmlFor="lesson-why">{t.whyQ}</label>
          <textarea id="lesson-why" className="input" rows={3} maxLength={500} value={text} placeholder={t.whyPlaceholder} onChange={(e) => setText(e.target.value)} />
          <div className="rate-row">
            <button type="button" className="btn btn-primary" onClick={() => void sendWhy()} disabled={busy} data-testid="lesson-why-send">{t.send}</button>
            <button type="button" className="btn btn-outline" onClick={() => setPhase('thanks')} disabled={busy}>{t.skip}</button>
          </div>
        </>
      )}
      {phase === 'thanks' && <p className="success small" role="status" style={{ margin: 0 }}>{t.lessonThanks}</p>}
    </div>
  );
}
