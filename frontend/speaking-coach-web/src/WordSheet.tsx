import { useEffect, useState } from 'react';
import { getLevel, postJson } from './api';
import { splitSentences, isSpeechSupported, speakAsync, stopSpeaking } from './speech';
import { common, msg, useT } from './i18n';
import { wordSheetMsg } from './locales/wordSheet';

interface WordExplanation {
  word: string;
  partOfSpeech: string;
  /** Interfeys tilidagi qisqa tarjima (uz/ru) yoki oddiy inglizcha sinonim (en). */
  translation: string;
  definitionEn: string;
  example: string;
}

const WORD_RE = /[A-Za-z][A-Za-z'’-]*[A-Za-z]|[A-Za-z]/;

/**
 * Matn bo'lagini (masalan `"medicine.`) uch qismga ajratadi: oldingi
 * belgilar, so'zning o'zi va keyingi belgilar. So'z bo'lmasa — null.
 */
export function splitToken(token: string): { before: string; word: string; after: string } | null {
  const m = WORD_RE.exec(token);
  if (!m) return null;
  return { before: token.slice(0, m.index), word: m[0], after: token.slice(m.index + m[0].length) };
}

/** Serverga yuboriladigan shakl: egri apostrof (’) oddiysiga almashtiriladi. */
export const normalizeWord = (w: string) => w.replace(/’/g, "'");

/**
 * Matnni bosiladigan so'zlarga aylantiradi. Har so'z o'zi turgan GAPni
 * biladi — ma'no kontekstda so'raladi ("bank" — daryo qirg'og'imi yoki bankmi).
 */
export function TappableText({ text, onWord }: { text: string; onWord: (word: string, sentence: string) => void }) {
  return (
    <>
      {text.split(/\n\s*\n/).map((para, pi) => (
        <p key={pi} style={{ lineHeight: 1.8 }}>
          {splitSentences(para).map((sentence, si) => (
            <span key={si}>
              {sentence.split(/(\s+)/).map((tok, ti) => {
                const parts = splitToken(tok);
                if (!parts) return tok;
                const pick = () => onWord(normalizeWord(parts.word), sentence);
                return (
                  <span key={ti}>
                    {parts.before}
                    <span
                      className="word"
                      role="button"
                      tabIndex={0}
                      onClick={pick}
                      onKeyDown={(e) => {
                        if (e.key === 'Enter' || e.key === ' ') {
                          e.preventDefault();
                          pick();
                        }
                      }}
                    >
                      {parts.word}
                    </span>
                    {parts.after}
                  </span>
                );
              })}{' '}
            </span>
          ))}
        </p>
      ))}
    </>
  );
}

/**
 * Pastdan chiqadigan oyna: so'z ma'nosi va "Lug'atga qo'shish". Lug'atga
 * qo'shilgan so'z avtomatik ravishda takrorlash navbatiga ham tushadi.
 * sentence bo'sh bo'lsa (Lug'at bo'limida qo'lda yozilgan so'z) — eng
 * ko'p ishlatiladigan ma'nosi so'raladi.
 */
export function WordSheet({
  word,
  sentence,
  source,
  loggedIn,
  onClose,
  onLogin,
  onCardsAdded,
}: {
  word: string;
  sentence: string;
  source?: 'Reading' | 'Listening';
  loggedIn: boolean;
  onClose: () => void;
  onLogin?: () => void;
  onCardsAdded?: () => void;
}) {
  const t = useT(wordSheetMsg);
  const c = useT(common);
  const [data, setData] = useState<WordExplanation | null>(null);
  const [error, setError] = useState('');
  const [saved, setSaved] = useState<{ ok: boolean; text: string } | null>(null);

  useEffect(() => {
    let cancelled = false;
    postJson<WordExplanation>('/api/words/explain', { word, sentence, level: getLevel() })
      .then((d) => !cancelled && setData(d))
      .catch((e) => !cancelled && setError(e instanceof Error ? e.message : msg(wordSheetMsg).loadError));
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    window.addEventListener('keydown', onKey);
    return () => {
      cancelled = true;
      window.removeEventListener('keydown', onKey);
    };
  }, [word, sentence, onClose]);

  async function addToVocab() {
    if (!data) return;
    try {
      await postJson('/api/vocab', {
        word: data.word,
        translation: data.translation,
        partOfSpeech: data.partOfSpeech,
        definitionEn: data.definitionEn,
        example: data.example,
        source,
      });
      setSaved({ ok: true, text: t.added });
      onCardsAdded?.();
    } catch (e) {
      setSaved({ ok: false, text: e instanceof Error ? e.message : c.error });
    }
  }

  return (
    <div className="sheet-backdrop" onClick={onClose}>
      <div className="sheet" role="dialog" aria-modal="true" aria-label={t.dialogLabel(word)} onClick={(e) => e.stopPropagation()}>
        <div className="spread">
          <h2 style={{ margin: 0 }}>{data?.word ?? word}</h2>
          <button className="btn-link" onClick={onClose} aria-label={c.close}>
            ✕
          </button>
        </div>
        {!data && !error && <p className="muted small">{t.loading}</p>}
        {error && <p className="error small">{error}</p>}
        {data && (
          <div className="stack" style={{ marginTop: 8 }}>
            <div>
              <span className="badge">{data.partOfSpeech}</span>
            </div>
            <p style={{ fontSize: '1.25rem', fontWeight: 700, margin: 0 }}>{data.translation}</p>
            <p className="muted small" style={{ margin: 0 }}>
              {data.definitionEn}
            </p>
            <p className="quote small" style={{ margin: 0 }}>
              {data.example}
            </p>
            <div className="row">
              {isSpeechSupported() && (
                <button className="btn btn-outline" onClick={() => { stopSpeaking(); speakAsync(`${data.word}. ${data.example}`); }}>
                  {c.listen}
                </button>
              )}
              {loggedIn ? (
                <button className="btn btn-primary" onClick={addToVocab} disabled={saved?.ok}>
                  {t.add}
                </button>
              ) : (
                onLogin && (
                  <button className="btn btn-primary" onClick={onLogin}>
                    {t.loginToSave}
                  </button>
                )
              )}
            </div>
            {saved && (
              <p className={`small ${saved.ok ? 'success' : 'error'}`} style={{ margin: 0 }}>
                {saved.text}
              </p>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
