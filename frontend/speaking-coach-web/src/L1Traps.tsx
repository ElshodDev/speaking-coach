import { useMemo, useState } from 'react';
import { postJson } from './api';
import { TRAP_SETS } from './content/l1traps';
import { useLang, useT } from './i18n';
import { cardsFromWrong, fill, itemsFor, loadBest, saveBest, score, shuffled, type Answer } from './l1Logic';
import { l1Msg } from './locales/l1traps';
import { mistakesMsg } from './locales/mistakes';
import { PageHeader } from './ui';

/** Testlar ro'yxati (Darslar → "Ona tili tuzoqlari"). */
export function L1Hub({ go }: { go: (route: string) => void }) {
  const t = useT(l1Msg);
  const best = useMemo(loadBest, []);
  return (
    <div className="stack" data-testid="l1-hub">
      <p className="muted small" style={{ margin: 0 }}>{t.subtitle}</p>
      {TRAP_SETS.map((s) => (
        <button key={s.id} className="card learn-card" style={{ textAlign: 'left' }} onClick={() => go(`learn/traps/${s.id}`)} data-testid="l1-set">
          <strong>{s.emoji} {t.sets[s.id].title}</strong>
          <span className="muted small">{t.sets[s.id].text}</span>
          <span className="tiny muted">
            {t.questions(s.items.length)}
            {best[s.id] !== undefined && ` · ${t.best(best[s.id])}`}
          </span>
        </button>
      ))}
    </div>
  );
}

/**
 * Test: bitta savol — ikki variant, tanlash bilan darhol javob va ona tilidagi sababi.
 * Oxirida natija va xato qilinganlarni takrorlash kartalariga qo'shish (AI'siz).
 */
export function L1Quiz({ setKey, go, loggedIn, onCardsAdded }: {
  setKey: string;
  go: (route: string) => void;
  loggedIn: boolean;
  onCardsAdded?: () => void;
}) {
  const t = useT(l1Msg);
  const tm = useT(mistakesMsg);
  const { lang } = useLang();
  const [round, setRound] = useState(1);
  const items = useMemo(() => shuffled(itemsFor(setKey), round * 7919 + setKey.length), [setKey, round]);
  const [i, setI] = useState(0);
  const [answers, setAnswers] = useState<Answer[]>([]);
  const [chosen, setChosen] = useState<string | null>(null);
  const [done, setDone] = useState(false);
  const [busy, setBusy] = useState(false);
  const [note, setNote] = useState('');

  const set = TRAP_SETS.find((s) => s.id === setKey);
  const title = set
    ? `${set.emoji} ${t.sets[set.id].title}`
    : `${t.byCategory}: ${tm.cats[setKey.slice(4)]?.name ?? setKey}`;
  const header = <PageHeader title={title} subtitle={t.title} onBack={() => go('learn/traps')} backLabel={t.back} />;

  if (items.length === 0) return <>{header}<p className="muted">—</p></>;

  const item = items[i];
  const options = shuffled(item.options, round * 31 + i * 17 + item.id.length);
  const label = (o: string) => (o === '(nothing)' ? t.nothing : o);

  function pick(o: string) {
    if (chosen !== null) return;
    setChosen(o);
    setAnswers((a) => [...a, { id: item.id, chosen: o, correct: o === item.options[0] }]);
  }

  function next() {
    if (i + 1 < items.length) {
      setI(i + 1);
      setChosen(null);
      return;
    }
    const s = score(answers);
    saveBest(setKey, s.pct);
    setDone(true);
  }

  function restart() {
    setRound((r) => r + 1);
    setI(0);
    setAnswers([]);
    setChosen(null);
    setDone(false);
    setNote('');
  }

  async function addCards() {
    setBusy(true);
    let added = 0;
    for (const c of cardsFromWrong(answers, lang)) {
      try {
        const r = await postJson<{ added: number }>('/api/review/cards', c);
        added += r.added;
      } catch {
        // allaqachon bor (review.phrase_exists) yoki limit — keyingisiga o'tamiz
      }
    }
    setBusy(false);
    setNote(t.added(added));
    if (added > 0) onCardsAdded?.();
  }

  if (done) {
    const s = score(answers);
    const wrong = answers.filter((a) => !a.correct);
    return (
      <>
        {header}
        <div className="card center" data-testid="l1-result">
          <div style={{ fontSize: '2.2rem', fontWeight: 700 }} className={s.pct >= 80 ? 'txt-good' : s.pct >= 50 ? 'txt-mid' : 'txt-low'}>{s.pct}%</div>
          <p>{t.result(s.right, s.total)}</p>
          {wrong.length === 0 && <p className="success">{t.perfect}</p>}
        </div>
        {wrong.length > 0 && (
          <div className="card">
            <h3>{t.review}</h3>
            <ul className="corrections">
              {wrong.map((a) => {
                const it = items.find((x) => x.id === a.id)!;
                return (
                  <li key={a.id}>
                    <div className="break"><s>{fill(it.q, a.chosen)}</s></div>
                    <div className="break"><strong>{fill(it.q, it.options[0])}</strong></div>
                    <div className="muted small">{it.explain[lang]}</div>
                  </li>
                );
              })}
            </ul>
            {loggedIn ? (
              <button className="btn btn-outline" onClick={addCards} disabled={busy || note !== ''} data-testid="l1-cards">{t.toCards(wrong.length)}</button>
            ) : (
              <p className="muted small">{t.loginForCards}</p>
            )}
            {note && <p className="success small" role="status">{note}</p>}
          </div>
        )}
        <div className="row" style={{ gap: 8, flexWrap: 'wrap' }}>
          <button className="btn btn-primary" onClick={restart}>{t.again}</button>
          <button className="btn btn-outline" onClick={() => go('learn/traps')}>{t.back}</button>
        </div>
      </>
    );
  }

  const isRight = chosen !== null && chosen === item.options[0];
  return (
    <>
      {header}
      <div className="card" data-testid="l1-quiz">
        <div className="spread small muted">
          <span>{t.progress(i + 1, items.length)}</span>
          <span>{tm.cats[item.cat]?.name ?? item.cat}</span>
        </div>
        <div className="progress" style={{ margin: '8px 0 14px' }} aria-hidden>
          <div style={{ width: `${(100 * i) / items.length}%` }} />
        </div>
        <p className="small muted" style={{ margin: 0 }}>{t.choose}</p>
        <p style={{ fontSize: '1.15rem', margin: '6px 0 14px' }} data-testid="l1-question">
          {chosen === null ? item.q : fill(item.q, item.options[0])}
        </p>
        <div className="stack">
          {options.map((o) => {
            const state = chosen === null ? '' : o === item.options[0] ? ' btn-primary' : o === chosen ? ' btn-danger' : '';
            return (
              // Javobdan keyin "disabled" emas (u tugmalarni xiralashtiradi va to'g'ri javob ko'rinmay qoladi) —
              // ikkinchi bosish pick() ichida e'tiborsiz qoldiriladi.
              <button key={o} className={`btn block${state || ' btn-outline'}`} onClick={() => pick(o)} aria-disabled={chosen !== null} data-testid="l1-option">
                {label(o)}
              </button>
            );
          })}
        </div>
        {chosen !== null && (
          <div style={{ marginTop: 14 }} role="status" data-testid="l1-feedback">
            <p style={{ margin: '0 0 6px' }}><strong>{isRight ? t.right : t.wrong(label(item.options[0]))}</strong></p>
            <p className="small" style={{ margin: '0 0 4px' }}>{item.explain[lang]}</p>
            <p className="muted tiny" style={{ margin: 0 }}>{t.l1}: {item.l1}</p>
            <button className="btn btn-primary block" style={{ marginTop: 12 }} onClick={next} data-testid="l1-next">
              {i + 1 < items.length ? t.next : t.finish}
            </button>
          </div>
        )}
      </div>
    </>
  );
}
