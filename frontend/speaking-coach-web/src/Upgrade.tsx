import { useState } from 'react';
import { getLevel, postJson } from './api';
import { useT } from './i18n';
import { upgradeMsg } from './locales/upgrade';
import { defaultTarget, highlight, IELTS_HINT, TARGETS, wordCount, type Target, type UpgradeResult } from './upgradeLogic';

/**
 * "Javobimni yaxshila" — Speaking / Part 2 / Writing natijasi ostida. Saqlangan urinish ID'si bo'yicha
 * ishlaydi (server matnni o'zi bazadan oladi va egasini tekshiradi).
 */
export function UpgradeAnswer({ activityId, original, loggedIn, onCardsAdded }: {
  activityId: string | null | undefined;
  /** O'quvchining matni (transkript yoki insho) — o'zgargan joylarni belgilash uchun. */
  original: string;
  loggedIn: boolean;
  onCardsAdded?: () => void;
}) {
  const t = useT(upgradeMsg);
  const [target, setTarget] = useState<Target>(() => defaultTarget(getLevel()));
  const [result, setResult] = useState<UpgradeResult | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [picked, setPicked] = useState<Set<number>>(new Set());
  const [note, setNote] = useState('');
  const [copied, setCopied] = useState(false);

  if (!loggedIn || !activityId) {
    return (
      <section className="upgrade" data-testid="upgrade">
        <h3>{t.title}</h3>
        <p className="muted small" style={{ margin: 0 }}>{t.login}</p>
      </section>
    );
  }

  async function run(tg: Target) {
    setBusy(true);
    setError('');
    setNote('');
    setCopied(false);
    try {
      const r = await postJson<{ result: UpgradeResult; cached: boolean }>(`/api/upgrade/${activityId}`, { target: tg });
      setResult(r.result);
      setPicked(new Set(r.result.items.map((_, i) => i)));
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  }

  async function addCards() {
    if (!result || picked.size === 0) return;
    setBusy(true);
    setError('');
    try {
      const r = await postJson<{ added: number }>(`/api/upgrade/${activityId}/cards`, { target: result.target, items: [...picked] });
      setNote(t.added(r.added));
      if (r.added > 0) onCardsAdded?.();
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e));
    } finally {
      setBusy(false);
    }
  }

  async function copy() {
    if (!result) return;
    try {
      await navigator.clipboard.writeText(result.upgradedText);
      setCopied(true);
    } catch {
      setCopied(false);
    }
  }

  const toggle = (i: number) =>
    setPicked((prev) => {
      const next = new Set(prev);
      if (next.has(i)) next.delete(i);
      else next.add(i);
      return next;
    });

  const segments = result ? highlight(original, result.items) : [];

  return (
    <section className="upgrade" data-testid="upgrade" aria-labelledby="upgrade-title">
      <h3 id="upgrade-title">{t.title}</h3>
      <p className="muted small" style={{ marginTop: 0 }}>{t.intro}</p>

      <div className="segmented" role="group" aria-label={t.target} style={{ marginBottom: 8 }}>
        {TARGETS.map((tg) => (
          <button key={tg} aria-pressed={target === tg} onClick={() => setTarget(tg)} disabled={busy}>
            {tg} <span className="tiny muted">{t.ielts(IELTS_HINT[tg])}</span>
          </button>
        ))}
      </div>
      <button className="btn btn-primary block" onClick={() => run(target)} disabled={busy} data-testid="upgrade-run">
        {busy && !result ? t.working : t.run(target)}
      </button>
      <p className="muted tiny">{t.aiNote}</p>
      {error && <p className="error" role="alert">{error}</p>}

      {result && (
        <div data-testid="upgrade-result">
          {result.summary && <p className="small">{result.summary}</p>}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', gap: 12 }}>
            <div>
              <div className="small"><strong>{t.yours}</strong> <span className="muted tiny">· {t.words(wordCount(original))}</span></div>
              <p className="quote" style={{ whiteSpace: 'pre-wrap' }}>
                {segments.map((s, i) =>
                  s.item === undefined ? <span key={i}>{s.text}</span> : <mark key={i} title={result.items[s.item].upgraded}>{s.text}</mark>,
                )}
              </p>
            </div>
            <div>
              <div className="small">
                <strong>{t.theirs(result.target)}</strong> <span className="muted tiny">· {t.words(wordCount(result.upgradedText))}</span>
              </div>
              <p className="quote" style={{ whiteSpace: 'pre-wrap' }} data-testid="upgrade-text">{result.upgradedText}</p>
              <button className="btn-link small" onClick={copy}>{copied ? t.copied : t.copy}</button>
            </div>
          </div>

          {result.items.length > 0 && (
            <>
              <h4 style={{ margin: '14px 0 6px' }}>{t.upgrades}</h4>
              <ul className="corrections" style={{ listStyle: 'none', paddingLeft: 0 }}>
                {result.items.map((it, i) => (
                  <li key={i}>
                    <label style={{ display: 'flex', gap: 8, alignItems: 'flex-start', cursor: 'pointer' }}>
                      <input type="checkbox" checked={picked.has(i)} onChange={() => toggle(i)} style={{ marginTop: 4 }} />
                      <span>
                        <span className="badge" style={{ marginRight: 6 }}>{t.kinds[it.kind]}</span>
                        <s>{it.original}</s> → <strong>{it.upgraded}</strong>
                        {it.why && <div className="muted small">{it.why}</div>}
                      </span>
                    </label>
                  </li>
                ))}
              </ul>
              <button className="btn btn-outline" onClick={addCards} disabled={busy || picked.size === 0} data-testid="upgrade-cards">
                {t.toCards(picked.size)}
              </button>
              {note && <p className="success small" role="status">{note}</p>}
            </>
          )}
        </div>
      )}
    </section>
  );
}
