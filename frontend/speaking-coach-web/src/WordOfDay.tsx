import { useEffect, useState } from 'react';
import { apiJson, postJson } from './api';
import { useT } from './i18n';
import { funMsg } from './locales/fun';
import { isSpeechSupported, speakAsync } from './speech';

interface Wod {
  date: string;
  word: string;
  partOfSpeech: string;
  level: string;
  definitionEn: string;
  example: string;
  translation: string;
}

/** Bosh sahifadagi "Kunlik so'z" (mehmonlar uchun ham). */
export function WordOfDay({ loggedIn, onLogin, onAdded }: { loggedIn: boolean; onLogin?: () => void; onAdded?: () => void }) {
  const t = useT(funMsg);
  const [w, setW] = useState<Wod | null>(null);
  const [saved, setSaved] = useState<{ ok: boolean; text: string } | null>(null);

  useEffect(() => {
    apiJson<Wod>(`/api/word-of-day?tzOffsetMinutes=${new Date().getTimezoneOffset()}`).then(setW).catch(() => setW(null));
  }, []);

  if (!w) return null;

  async function add() {
    if (!w) return;
    try {
      await postJson('/api/vocab', { word: w.word, translation: w.translation, partOfSpeech: w.partOfSpeech, definitionEn: w.definitionEn, example: w.example });
      setSaved({ ok: true, text: t.added });
      onAdded?.();
    } catch (e) {
      setSaved({ ok: false, text: e instanceof Error ? e.message : String(e) });
    }
  }

  return (
    <div className="card" data-testid="word-of-day">
      <div className="spread" style={{ alignItems: 'baseline' }}>
        <h3 style={{ margin: 0 }}>{t.wodTitle}</h3>
        <span className="badge">{w.level}</span>
      </div>
      <p style={{ margin: '10px 0 2px', fontSize: '1.6rem', fontWeight: 800 }} lang="en">
        {w.word} <span className="muted small" style={{ fontWeight: 400 }}>({w.partOfSpeech})</span>
      </p>
      <p style={{ margin: '0 0 6px' }}><strong>{w.translation}</strong></p>
      {w.translation !== w.definitionEn && <p className="muted small" style={{ margin: '0 0 6px' }} lang="en">{w.definitionEn}</p>}
      <p className="quote small" style={{ margin: '0 0 10px' }} lang="en">{w.example}</p>
      <div className="row" style={{ flexWrap: 'wrap', gap: 8 }}>
        {isSpeechSupported() && (
          <button className="btn btn-outline" onClick={() => void speakAsync(`${w.word}. ${w.example}`)}>{t.listen}</button>
        )}
        {loggedIn ? (
          !saved?.ok && <button className="btn btn-primary" onClick={() => void add()}>{t.add}</button>
        ) : (
          onLogin && <button className="btn-link small" onClick={onLogin}>{t.loginToAdd}</button>
        )}
      </div>
      {saved && <p className={saved.ok ? 'success small' : 'error small'} role="status" style={{ margin: '8px 0 0' }}>{saved.text}</p>}
    </div>
  );
}

/** Viktorinaga kirish kartasi. */
export function QuizTile({ go }: { go: (route: string) => void }) {
  const t = useT(funMsg);
  return (
    <div className="card cta" data-testid="quiz-tile">
      <div>
        <strong>{t.quizTile}</strong>
        <div className="muted small">{t.quizTileText}</div>
      </div>
      <button className="btn btn-primary" onClick={() => go('quiz')} aria-label={t.quizTile}>▶</button>
    </div>
  );
}
