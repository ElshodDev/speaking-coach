import { useT } from './i18n';
import { mockObjMsg } from './locales/mockObjective';

export interface ClientQuestion {
  number: number;
  prompt: string;
  options: string[] | null;
}

export interface ClientGroup {
  type: 'mcq' | 'tfng' | 'ynng' | 'gap';
  instructions: string;
  maxWords: number | null;
  questions: ClientQuestion[];
}

export type Answers = Record<string, string>;

export const LETTERS = ['A', 'B', 'C', 'D', 'E'];
const FIXED: Record<string, string[]> = { tfng: ['TRUE', 'FALSE', 'NOT GIVEN'], ynng: ['YES', 'NO', 'NOT GIVEN'] };

export function questionNumbers(groups: ClientGroup[]): number[] {
  return groups.flatMap((g) => g.questions.map((q) => q.number));
}

export function answeredCount(answers: Answers, numbers: number[]): number {
  return numbers.filter((n) => (answers[String(n)] ?? '').trim().length > 0).length;
}

/** "Meet at the ___ gate" → ["Meet at the ", " gate"] (birinchi bo'sh joy bo'yicha). */
export function splitGap(prompt: string): [string, string] {
  const i = prompt.indexOf('___');
  if (i < 0) return [prompt, ''];
  const after = prompt.slice(i).replace(/^_+/, '');
  return [prompt.slice(0, i), after];
}

/**
 * Savol guruhlari: ko'rsatma + savollar. Variantlar — katta bosiladigan
 * tugmalar (telefonda qulay), bo'sh joy — gap ichidagi kiritish maydoni.
 */
export function ObjectiveQuestions({
  groups,
  answers,
  onChange,
  disabled = false,
}: {
  groups: ClientGroup[];
  answers: Answers;
  onChange: (number: number, value: string) => void;
  disabled?: boolean;
}) {
  const t = useT(mockObjMsg);
  return (
    <div className="stack">
      {groups.map((g, gi) => {
        const nums = g.questions.map((q) => q.number);
        return (
          <section key={gi} className="stack" aria-label={t.questionsRange(nums[0], nums[nums.length - 1])}>
            <div className="small">
              <strong>{t.questionsRange(nums[0], nums[nums.length - 1])}</strong>
              <div className="muted" style={{ whiteSpace: 'pre-wrap' }}>{g.instructions}</div>
            </div>
            {g.questions.map((q) => {
              const value = answers[String(q.number)] ?? '';
              if (g.type === 'gap') {
                const [before, after] = splitGap(q.prompt);
                return (
                  <label key={q.number} id={`q-${q.number}`} className="small" style={{ display: 'block', lineHeight: 2, scrollMarginTop: 140 }}>
                    <strong>{q.number}.</strong> {before}
                    <input
                      className="input"
                      style={{ display: 'inline-block', width: 150, padding: '4px 8px', margin: '0 4px' }}
                      value={value}
                      onChange={(e) => onChange(q.number, e.target.value)}
                      disabled={disabled}
                      aria-label={t.qLabel(q.number)}
                      autoComplete="off"
                      autoCorrect="off"
                      autoCapitalize="none"
                      spellCheck={false}
                      maxLength={60}
                    />
                    {after}
                  </label>
                );
              }
              const choices = g.type === 'mcq' ? (q.options ?? []).map((o, i) => ({ value: LETTERS[i], label: `${LETTERS[i]}  ${o}` })) : FIXED[g.type].map((v) => ({ value: v, label: v }));
              return (
                <fieldset key={q.number} id={`q-${q.number}`} style={{ border: 0, padding: 0, margin: 0, scrollMarginTop: 140 }}>
                  <legend className="small" style={{ marginBottom: 6 }}>
                    <strong>{q.number}.</strong> {q.prompt}
                  </legend>
                  <div className="chips" style={{ flexDirection: g.type === 'mcq' ? 'column' : 'row', alignItems: 'stretch' }}>
                    {choices.map((c) => (
                      <button
                        key={c.value}
                        type="button"
                        aria-pressed={value === c.value}
                        onClick={() => onChange(q.number, value === c.value ? '' : c.value)}
                        disabled={disabled}
                        style={{ textAlign: 'left' }}
                      >
                        {c.label}
                      </button>
                    ))}
                  </div>
                </fieldset>
              );
            })}
          </section>
        );
      })}
    </div>
  );
}

/** Pastki "savollar xaritasi": 1–40, javob berilganlari belgilangan; bosilsa — shu savolga o'tadi. */
export function QuestionMap({ numbers, answers, onJump }: { numbers: number[]; answers: Answers; onJump: (n: number) => void }) {
  const t = useT(mockObjMsg);
  return (
    <div className="qmap" style={{ display: 'flex', flexWrap: 'wrap', gap: 4 }}>
      {numbers.map((n) => {
        const done = (answers[String(n)] ?? '').trim().length > 0;
        return (
          <button
            key={n}
            type="button"
            onClick={() => onJump(n)}
            className="small"
            aria-label={t.goTo(n)}
            style={{
              width: 30,
              height: 30,
              borderRadius: 6,
              border: '1px solid var(--border)',
              background: done ? 'var(--primary)' : 'var(--surface)',
              color: done ? '#fff' : 'var(--text)',
              padding: 0,
            }}
          >
            {n}
          </button>
        );
      })}
    </div>
  );
}
