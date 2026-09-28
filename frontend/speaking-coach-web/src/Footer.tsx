import { useEffect, useId, useRef, useState, type KeyboardEvent } from 'react';
import { postJson } from './api';
import { useLang, useT } from './i18n';
import { legalMsg, type FeedbackKind } from './locales/legal';

/** Aloqa emaili. Bo'sh bo'lsa — "Aloqa" qatori ko'rsatilmaydi. */
export const CONTACT_EMAIL = 'elshodibadullayev28@gmail.com';

export const FEEDBACK_MAX = 2000;
const KINDS: FeedbackKind[] = ['bug', 'idea', 'content', 'ai', 'other'];

/**
 * Qaysi sahifadan yozilgani (hash-marshrut bilan: "/#/practice/writing").
 * "?" dan keyingi qism (masalan, tiklash tokeni) yuborilmaydi.
 */
export function feedbackPage(loc: Pick<Location, 'pathname' | 'hash'>): string {
  const hash = loc.hash.split(/[?&]/)[0];
  return `${loc.pathname}${hash.includes('tgWebAppData') ? '' : hash}`.slice(0, 200);
}

/** Sahifa osti: maxfiylik va shartlar, aloqa, IELTS/CEFR ogohlantirishi, fikr bildirish. */
export function Footer() {
  const t = useT(legalMsg);
  const { lang } = useLang();
  const [open, setOpen] = useState(false);
  const trigger = useRef<HTMLButtonElement>(null);

  function close() {
    setOpen(false);
    // Fokus dialogni ochgan tugmaga qaytadi (klaviatura foydalanuvchisi adashmasin).
    requestAnimationFrame(() => trigger.current?.focus());
  }

  return (
    <footer className="site-footer" data-testid="site-footer">
      <nav className="site-footer-links" aria-label={`${t.privacy} · ${t.terms}`}>
        <a href={`/privacy.html#${lang}`}>{t.privacy}</a>
        <a href={`/terms.html#${lang}`}>{t.terms}</a>
        <button ref={trigger} type="button" className="btn-link" onClick={() => setOpen(true)} data-testid="feedback-open">
          {t.feedback}
        </button>
      </nav>
      {CONTACT_EMAIL && (
        <p className="tiny muted" style={{ margin: '6px 0 0' }}>
          {t.contact}: <a href={`mailto:${CONTACT_EMAIL}`}>{CONTACT_EMAIL}</a>
        </p>
      )}
      <p className="tiny muted" style={{ margin: '6px 0 0' }}>{t.disclaimer}</p>
      {open && <FeedbackDialog onClose={close} />}
    </footer>
  );
}

function FeedbackDialog({ onClose }: { onClose: () => void }) {
  const t = useT(legalMsg);
  const titleId = useId();
  const kindId = useId();
  const msgId = useId();
  const [kind, setKind] = useState<FeedbackKind>('bug');
  const [message, setMessage] = useState('');
  const [state, setState] = useState<'idle' | 'sending' | 'sent'>('idle');
  const [error, setError] = useState('');
  const dialog = useRef<HTMLDivElement>(null);
  const first = useRef<HTMLSelectElement>(null);
  const closeBtn = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    first.current?.focus();
  }, []);

  useEffect(() => {
    if (state === 'sent') closeBtn.current?.focus();
  }, [state]);

  async function send() {
    const text = message.trim();
    if (!text) {
      setError(t.empty);
      return;
    }
    setState('sending');
    setError('');
    try {
      await postJson('/api/feedback', { kind, message: text, page: feedbackPage(window.location) });
      setState('sent');
    } catch (err) {
      setState('idle');
      setError(err instanceof Error && err.message ? err.message : t.failed);
    }
  }

  // Escape — yopish; Tab — fokus dialog ichida aylanadi.
  function onKeyDown(e: KeyboardEvent<HTMLDivElement>) {
    if (e.key === 'Escape') {
      e.stopPropagation();
      onClose();
      return;
    }
    if (e.key !== 'Tab' || !dialog.current) return;
    const items = dialog.current.querySelectorAll<HTMLElement>('button:not(:disabled), select:not(:disabled), textarea:not(:disabled), a[href]');
    if (items.length === 0) return;
    const firstEl = items[0];
    const lastEl = items[items.length - 1];
    if (e.shiftKey && document.activeElement === firstEl) {
      e.preventDefault();
      lastEl.focus();
    } else if (!e.shiftKey && document.activeElement === lastEl) {
      e.preventDefault();
      firstEl.focus();
    }
  }

  const busy = state === 'sending';

  return (
    <div className="modal-backdrop" onClick={busy ? undefined : onClose}>
      <div
        ref={dialog}
        className="modal card"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        onClick={(e) => e.stopPropagation()}
        onKeyDown={onKeyDown}
        data-testid="feedback-dialog"
      >
        <div className="spread">
          <h3 id={titleId} style={{ margin: 0 }}>{t.feedbackTitle}</h3>
          <button type="button" className="btn-link" onClick={onClose} aria-label={t.close}>✕</button>
        </div>

        {state === 'sent' ? (
          <>
            <p className="success" role="status" style={{ margin: 0 }}>{t.thanks}</p>
            <button ref={closeBtn} type="button" className="btn btn-primary" onClick={onClose}>{t.close}</button>
          </>
        ) : (
          <form
            className="stack"
            onSubmit={(e) => {
              e.preventDefault();
              send();
            }}
          >
            <p className="muted small" style={{ margin: 0 }}>{t.feedbackIntro}</p>
            <div className="field">
              <label htmlFor={kindId} className="small">{t.kindLabel}</label>
              <select
                id={kindId}
                ref={first}
                className="input"
                style={{ width: '100%' }}
                value={kind}
                onChange={(e) => setKind(e.target.value as FeedbackKind)}
                disabled={busy}
              >
                {KINDS.map((k) => (
                  <option key={k} value={k}>{t.kinds[k]}</option>
                ))}
              </select>
            </div>
            <div className="field">
              <label htmlFor={msgId} className="small">{t.messageLabel}</label>
              <textarea
                id={msgId}
                className="input"
                rows={5}
                maxLength={FEEDBACK_MAX}
                value={message}
                onChange={(e) => setMessage(e.target.value)}
                placeholder={t.messagePlaceholder}
                disabled={busy}
                aria-describedby={`${msgId}-count`}
              />
              <div id={`${msgId}-count`} className="tiny muted" style={{ textAlign: 'right' }}>
                {t.chars(message.length, FEEDBACK_MAX)}
              </div>
            </div>
            {error && <p className="error small" role="alert" style={{ margin: 0 }}>{error}</p>}
            <div className="row" style={{ gap: 8, justifyContent: 'flex-end' }}>
              <button type="button" className="btn btn-outline" onClick={onClose} disabled={busy}>{t.cancel}</button>
              <button type="submit" className="btn btn-primary" disabled={busy}>{busy ? t.sending : t.send}</button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
