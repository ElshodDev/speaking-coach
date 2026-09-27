// Speaking/Writing mavzusi: ilovaning tayyor bankidan (daraja va tur bo'yicha)
// yoki — foydalanuvchi xohlasa — AI yangi mavzu taklif qiladi.
import { useMemo, useState } from 'react';
import { postJson } from './api';
import { SPEAKING_TOPICS_BANK, WRITING_TOPICS_BANK } from './content/topics';
import type { Level } from './content/types';
import { useT } from './i18n';
import { topicsMsg } from './locales/topics';

export type TopicKind = 'speaking' | 'writing';

export interface PickedTopic {
  text: string;
  category: string;
  points?: string[];
  words?: [number, number];
  id?: string;
  ai?: boolean;
}

export const CATEGORIES: Record<TopicKind, string[]> = {
  speaking: ['everyday', 'ielts-part1', 'ielts-part2', 'ielts-part3', 'cefr'],
  writing: ['paragraph', 'essay-opinion', 'essay-discussion', 'letter-informal', 'letter-formal'],
};

const bankOf = (kind: TopicKind) => (kind === 'speaking' ? SPEAKING_TOPICS_BANK : WRITING_TOPICS_BANK);

/** Tanlangan tur va daraja bo'yicha mavzular (darajada bo'lmasa — shu turdagi hammasi). */
export function topicsFor(kind: TopicKind, category: string, level: string): PickedTopic[] {
  const all = bankOf(kind).filter((t) => t.category === category);
  const atLevel = all.filter((t) => t.level === level);
  return (atLevel.length > 0 ? atLevel : all).map((t) => ({
    id: t.id,
    text: t.text,
    category: t.category,
    points: 'points' in t ? t.points : undefined,
    words: 'words' in t ? t.words : undefined,
  }));
}

/** Darajaga mos boshlang'ich tur: boshlovchiga kundalik, yuqorida — imtihon uslubi. */
export function defaultCategory(kind: TopicKind, level: string): string {
  const basic = level === 'A2' || level === 'B1';
  if (kind === 'speaking') return basic ? 'everyday' : 'ielts-part2';
  return basic ? 'paragraph' : 'essay-opinion';
}

export function initialTopic(kind: TopicKind, level: string): PickedTopic {
  const cat = defaultCategory(kind, level);
  return topicsFor(kind, cat, level)[0] ?? { text: '', category: cat };
}

export function TopicPicker({
  kind,
  level,
  value,
  onChange,
  disabled,
}: {
  kind: TopicKind;
  level: Level | string;
  value: PickedTopic;
  onChange: (t: PickedTopic) => void;
  disabled?: boolean;
}) {
  const t = useT(topicsMsg);
  const labels = kind === 'speaking' ? t.speaking : t.writing;
  const [aiBusy, setAiBusy] = useState(false);
  const [error, setError] = useState('');
  const list = useMemo(() => topicsFor(kind, value.category, level), [kind, value.category, level]);

  function pickCategory(category: string) {
    setError('');
    onChange(topicsFor(kind, category, level)[0] ?? { text: '', category });
  }

  function next() {
    setError('');
    if (list.length === 0) return;
    const i = list.findIndex((x) => x.id === value.id);
    onChange(list[(i + 1) % list.length]);
  }

  async function askAi() {
    setAiBusy(true);
    setError('');
    try {
      const r = await postJson<{ text: string; points?: string[] | null }>('/api/topics/suggest', {
        kind,
        category: value.category,
        level,
        avoid: [value.text],
      });
      onChange({ text: r.text, points: r.points ?? undefined, category: value.category, words: value.words, ai: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : '');
    } finally {
      setAiBusy(false);
    }
  }

  return (
    <div className="topic-picker" data-testid="topic-picker">
      <div className="chips" role="group" style={{ margin: '0 0 10px' }}>
        {CATEGORIES[kind].map((c) => (
          <button key={c} aria-pressed={value.category === c} onClick={() => pickCategory(c)} disabled={disabled}>
            {labels[c] ?? c}
          </button>
        ))}
      </div>
      <p className="topic-text" data-testid="topic-text">{value.text}</p>
      {value.points && value.points.length > 0 && (
        <div className="small" style={{ margin: '-6px 0 10px' }}>
          <span className="muted">{t.shouldSay}</span>
          <ul style={{ margin: '4px 0 0', paddingLeft: 20 }}>
            {value.points.map((p) => (
              <li key={p}>{p}</li>
            ))}
          </ul>
        </div>
      )}
      <div className="spread small" style={{ marginBottom: 12 }}>
        <span className="muted tiny">
          {value.ai ? t.fromAi : t.count(list.length)}
          {kind === 'writing' && value.words ? ` · ${t.words(value.words[0], value.words[1])}` : ''}
        </span>
        <span className="row" style={{ gap: 12 }}>
          <button className="btn-link small" onClick={next} disabled={disabled || list.length < 2} data-testid="topic-next">
            {t.otherTopic}
          </button>
          <button className="btn-link small" onClick={askAi} disabled={disabled || aiBusy} data-testid="topic-ai">
            {aiBusy ? t.aiBusy : t.ai}
          </button>
        </span>
      </div>
      {error && <p className="error small" role="alert">{error}</p>}
    </div>
  );
}
