import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import { apiFetch, apiJson } from './api';
import { isSpeechSupported, speakAsync, stopSpeaking } from './speech';
import { PageHeader } from './ui';
import { WordSheet } from './WordSheet';
import { common, msg, useT } from './i18n';
import { vocabMsg } from './locales/vocab';

export type VocabStatus = 'New' | 'Learning' | 'Known';

export interface VocabWord {
  id: string;
  word: string;
  meaning: string;
  note: string;
  kind: string;
  source: string | null;
  status: VocabStatus;
  due: boolean;
  createdAtUtc: string;
  lapses: number;
}

export interface VocabData {
  total: number;
  new: number;
  learning: number;
  known: number;
  due: number;
  words: VocabWord[];
}

export type VocabFilter = 'all' | VocabStatus;

/** Izohni ajratadi: "(noun) a substance... — Take this..." → so'z turkumi va qolgan matn. */
export function parseNote(note: string): { pos: string | null; rest: string } {
  const m = /^\(([^)]{1,30})\)\s*(.*)$/s.exec(note.trim());
  return m ? { pos: m[1], rest: m[2] } : { pos: null, rest: note.trim() };
}

/** Qidiruv (so'z yoki ma'no bo'yicha, katta-kichik harfsiz) va holat filtri. */
export function filterWords(words: VocabWord[], query: string, filter: VocabFilter): VocabWord[] {
  const q = query.trim().toLowerCase();
  return words.filter(
    (w) =>
      (filter === 'all' || w.status === filter) &&
      (q === '' || w.word.toLowerCase().includes(q) || w.meaning.toLowerCase().includes(q)),
  );
}

/** 25.09.2026 — brauzer tilidan qat'i nazar bir xil ko'rinish. */
export function formatDate(iso: string): string {
  const d = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${pad(d.getDate())}.${pad(d.getMonth() + 1)}.${d.getFullYear()}`;
}

/** AI izohlay oladigan bitta inglizcha so'z (backend'dagi tekshiruv bilan bir xil). */
export const isLookupWord = (s: string) => /^[A-Za-z][A-Za-z'-]{0,39}$/.test(s.trim());

/**
 * Lug'at — foydalanuvchining shaxsiy so'zlar ro'yxati. Alohida jadval emas:
 * so'z turidagi takrorlash kartalari, shuning uchun har bir so'z o'z-o'zidan
 * oraliqli takrorlashga tushadi va "Yangi → O'rganilmoqda → Yodlangan"
 * holati takrorlash natijasidan kelib chiqadi.
 */
export function Vocab({
  loggedIn,
  onLogin,
  onChanged,
  go,
}: {
  loggedIn: boolean;
  onLogin: () => void;
  onChanged: () => void;
  go: (route: string) => void;
}) {
  const t = useT(vocabMsg);
  const c = useT(common);
  const [data, setData] = useState<VocabData | null>(null);
  const [error, setError] = useState('');
  const [query, setQuery] = useState('');
  const [filter, setFilter] = useState<VocabFilter>('all');
  const [lookup, setLookup] = useState<string | null>(null);

  const load = useCallback(() => {
    if (!loggedIn) return;
    apiJson<VocabData>('/api/vocab')
      .then((d) => {
        setData(d);
        setError('');
      })
      .catch((e) => setError(e instanceof Error ? e.message : msg(vocabMsg).loadError));
  }, [loggedIn]);

  useEffect(() => {
    load();
  }, [load]);

  const closeLookup = useCallback(() => setLookup(null), []);
  const shown = useMemo(() => (data ? filterWords(data.words, query, filter) : []), [data, query, filter]);

  const q = query.trim();
  const exact = data?.words.some((w) => w.word.toLowerCase() === q.toLowerCase()) ?? false;
  const canLookup = isLookupWord(q) && !exact;

  function submit(e: FormEvent) {
    e.preventDefault();
    if (canLookup) setLookup(q);
  }

  async function remove(w: VocabWord) {
    if (!confirm(t.confirmDelete(w.word))) return;
    await apiFetch(`/api/review/cards/${w.id}`, { method: 'DELETE' });
    load();
    onChanged();
  }

  const sheet = lookup && (
    <WordSheet
      word={lookup}
      sentence=""
      loggedIn={loggedIn}
      onClose={closeLookup}
      onLogin={onLogin}
      onCardsAdded={() => {
        setQuery('');
        load();
        onChanged();
      }}
    />
  );

  const searchForm = (
    <form className="vocab-search" onSubmit={submit} role="search">
      <input
        className="input"
        type="search"
        placeholder={loggedIn ? t.searchPh : t.guestSearchPh}
        aria-label={t.searchLabel}
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        autoCapitalize="off"
        autoCorrect="off"
        spellCheck={false}
      />
      <button type="submit" className="btn btn-primary" disabled={!canLookup}>
        {t.lookup}
      </button>
    </form>
  );

  if (!loggedIn) {
    return (
      <>
        <PageHeader title={t.title} subtitle={t.guestSubtitle} />
        {searchForm}
        <div className="card">
          <div className="steps">
            <div className="step">
              <div>
                {t.guestStep1Before}
                <strong>{t.guestStep1Strong}</strong>
                {t.guestStep1After}
              </div>
            </div>
            <div className="step">
              <div>
                <strong>{t.guestStep2Strong}</strong>
                {t.guestStep2After}
              </div>
            </div>
            <div className="step">
              <div>
                {t.guestStep3Before}
                <strong>{t.guestStep3Strong}</strong>
                {t.guestStep3After}
              </div>
            </div>
          </div>
          <button className="btn btn-primary block" style={{ marginTop: 14 }} onClick={onLogin}>
            {t.loginToSave}
          </button>
        </div>
        {sheet}
      </>
    );
  }

  const counts: { id: VocabFilter; label: string; n: number }[] = data
    ? [
        { id: 'all', label: t.all, n: data.total },
        { id: 'New', label: t.chips.New, n: data.new },
        { id: 'Learning', label: t.chips.Learning, n: data.learning },
        { id: 'Known', label: t.chips.Known, n: data.known },
      ]
    : [];

  return (
    <>
      <PageHeader title={t.title} subtitle={data ? t.subtitle(data.total, data.known) : undefined} />
      {searchForm}
      {error && <p className="error small">{error}</p>}

      {data && data.total > 0 && (
        <>
          <div className="card cta">
            <div>
              <strong>{data.due > 0 ? t.dueTitle(data.due) : t.allReviewed}</strong>
              <div className="muted small">
                {data.due > 0 ? t.dueText : t.noneDueText}
              </div>
            </div>
            <button className="btn btn-primary" onClick={() => go('review/vocab')} disabled={data.due === 0}>
              {t.review}
            </button>
          </div>

          <div className="chips" role="group" aria-label={t.chipsLabel}>
            {counts.map((c) => (
              <button key={c.id} aria-pressed={filter === c.id} onClick={() => setFilter(c.id)}>
                {c.label} <span className="n">{c.n}</span>
              </button>
            ))}
          </div>
        </>
      )}

      {data && data.total === 0 && (
        <div className="card center">
          <div style={{ fontSize: '2.2rem' }}>📚</div>
          <p style={{ fontSize: '1.1rem', fontWeight: 600, margin: '4px 0' }}>{t.emptyTitle}</p>
          <p className="muted small" style={{ margin: '0 0 12px' }}>
            {t.emptyText}
          </p>
          <button className="btn btn-outline" onClick={() => go('practice/reading')}>
            {t.goReading}
          </button>
        </div>
      )}

      {data && data.total > 0 && shown.length === 0 && (
        <p className="muted small center" style={{ marginTop: 16 }}>
          {canLookup ? t.notInVocab(q) : t.nothingFound}
        </p>
      )}

      {shown.length > 0 && (
        <ul className="vocab-list">
          {shown.map((w) => {
            const { pos, rest } = parseNote(w.note);
            return (
              <li key={w.id} className="card">
                <div className="spread">
                  <div className="vocab-word">
                    <strong>{w.word}</strong>
                    {pos && <span className="badge">{pos}</span>}
                    {isSpeechSupported() && (
                      <button
                        className="btn-link"
                        aria-label={t.pronounce(w.word)}
                        onClick={() => {
                          stopSpeaking();
                          speakAsync(w.word);
                        }}
                      >
                        🔊
                      </button>
                    )}
                  </div>
                  <span className={`status status-${w.status.toLowerCase()}`}>{t.status[w.status]}</span>
                </div>
                <div className="vocab-meaning">{w.meaning}</div>
                {rest && <p className="muted small" style={{ margin: '4px 0 0' }}>{rest}</p>}
                <div className="spread tiny muted" style={{ marginTop: 8 }}>
                  <span>
                    {w.source && t.source[w.source] ? `${t.source[w.source]} · ` : ''}
                    {formatDate(w.createdAtUtc)}
                    {w.due ? t.dueMark : ''}
                  </span>
                  <button className="btn-link quiet tiny" onClick={() => remove(w)}>
                    {c.delete}
                  </button>
                </div>
              </li>
            );
          })}
        </ul>
      )}
      {sheet}
    </>
  );
}
