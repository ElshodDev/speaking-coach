import { useT } from './i18n';
import { mockObjMsg } from './locales/mockObjective';

export interface ClientQuestion {
  number: number;
  prompt: string;
  options: string[] | null;
}

export interface MapSpot {
  label: string;
  x: number;
  y: number;
}

export interface MapPlan {
  title: string;
  landmarks: MapSpot[];
  spots: MapSpot[];
}

export interface ClientGroup {
  type: 'mcq' | 'tfng' | 'ynng' | 'gap' | 'match' | 'map';
  instructions: string;
  maxWords: number | null;
  questions: ClientQuestion[];
  /** match: umumiy variantlar (A, B, C…) */
  options?: string[] | null;
  /** map: xarita (A–H joylar va nomli mo'ljallar) */
  map?: MapPlan | null;
}

export type Answers = Record<string, string>;

export const LETTERS = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J'];
const FIXED: Record<string, string[]> = { tfng: ['TRUE', 'FALSE', 'NOT GIVEN'], ynng: ['YES', 'NO', 'NOT GIVEN'] };

/** Javob harfi uchun matn: "B — Hotel" (natija sahifasida harfning o'zi yetarli emas). */
export function choiceLabel(group: ClientGroup | undefined, q: ClientQuestion | undefined, value: string): string {
  const i = LETTERS.indexOf(value.trim().toUpperCase());
  if (!group || i < 0) return value;
  const text = group.type === 'mcq' ? q?.options?.[i] : group.type === 'match' ? group.options?.[i] : undefined;
  return text ? `${LETTERS[i]} — ${text}` : value;
}

/**
 * Xarita: 5×5 katak. Nomli mo'ljallar (kulrang) va harfli joylar (A–H,
 * ko'k doira). Tinglovchi yo'nalishlarga qarab harfni tanlaydi.
 */
export function MapView({ map }: { map: MapPlan }) {
  const cell = 64;
  const size = cell * 5;
  const center = (v: number) => v * cell + cell / 2;
  return (
    <figure style={{ margin: 0 }} data-testid="map">
      <svg viewBox={`0 0 ${size} ${size}`} role="img" aria-label={map.title} style={{ width: '100%', maxWidth: 360, display: 'block', margin: '0 auto' }}>
        <rect x={1} y={1} width={size - 2} height={size - 2} rx={12} fill="var(--surface-2, #f1f5f9)" stroke="var(--border)" strokeWidth={2} />
        {[1, 2, 3, 4].map((i) => (
          <g key={i} stroke="var(--border)" strokeDasharray="3 5">
            <line x1={i * cell} y1={4} x2={i * cell} y2={size - 4} />
            <line x1={4} y1={i * cell} x2={size - 4} y2={i * cell} />
          </g>
        ))}
        {map.landmarks.map((l) => (
          <g key={`l-${l.label}`}>
            <rect x={l.x * cell + 3} y={center(l.y) - 16} width={cell - 6} height={32} rx={6} fill="var(--surface)" stroke="var(--muted, #64748b)" />
            <text x={center(l.x)} y={center(l.y)} textAnchor="middle" dominantBaseline="central" fontSize={l.label.length > 9 ? 8.5 : 10.5} fill="var(--text)">
              {l.label.length > 14 ? `${l.label.slice(0, 13)}…` : l.label}
            </text>
          </g>
        ))}
        {map.spots.map((s) => (
          <g key={`s-${s.label}`}>
            <circle cx={center(s.x)} cy={center(s.y)} r={17} fill="var(--primary)" />
            <text x={center(s.x)} y={center(s.y)} textAnchor="middle" dominantBaseline="central" fontSize={16} fontWeight={700} fill="#fff">
              {s.label}
            </text>
          </g>
        ))}
      </svg>
      <figcaption className="muted tiny" style={{ textAlign: 'center', marginTop: 4 }}>{map.title}</figcaption>
    </figure>
  );
}

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
            {g.type === 'match' && g.options && (
              <ol className="small" style={{ listStyle: 'none', padding: '8px 12px', margin: 0, background: 'var(--surface-2, #f1f5f9)', borderRadius: 10 }} data-testid="match-options">
                {g.options.map((o, i) => (
                  <li key={i} style={{ padding: '3px 0' }}><strong>{LETTERS[i]}</strong>&nbsp; {o}</li>
                ))}
              </ol>
            )}
            {g.type === 'map' && g.map && <MapView map={g.map} />}
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
              const letters =
                g.type === 'match' ? LETTERS.slice(0, g.options?.length ?? 0)
                : g.type === 'map' ? (g.map?.spots ?? []).map((sp) => sp.label)
                : null;
              const choices =
                g.type === 'mcq' ? (q.options ?? []).map((o, i) => ({ value: LETTERS[i], label: `${LETTERS[i]}  ${o}` }))
                : letters ? letters.map((v) => ({ value: v, label: v }))
                : FIXED[g.type].map((v) => ({ value: v, label: v }));
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
