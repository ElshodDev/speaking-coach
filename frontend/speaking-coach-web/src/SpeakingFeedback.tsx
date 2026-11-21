import { useT } from './i18n';
import { speakingMsg } from './locales/speaking';
import { bandTone, criteriaRows, formatBand, overallBandOf, priorityFixOf, type SpeakingEvaluationLike } from './speakingFeedback';

/**
 * Gapirish natijasi: umumiy taxminiy band, BITTA asosiy tuzatish va to'rt rasmiy IELTS mezoni (Mock Speaking bilan bir xil).
 * Talaffuz alohida "taxminiy" deb belgilanadi (AI uni ovoz sifatidan taxmin qiladi).
 * Yangi ma'lumot bo'lmasa — null (chaqiruvchi eski ko'rinishni ishlatadi).
 */
export function SpeakingFeedback({ evaluation }: { evaluation: SpeakingEvaluationLike }) {
  const t = useT(speakingMsg).four;
  const rows = criteriaRows(evaluation);
  const overall = overallBandOf(evaluation);
  if (!rows || overall === null) return null;
  const fix = priorityFixOf(evaluation);

  return (
    <div className="four" data-testid="four-criteria">
      <div className="four-overall">
        <div>
          <div className="muted small">{t.overall}</div>
          <div className={`four-band txt-${bandTone(overall)}`} data-testid="overall-band">
            ≈ {formatBand(overall)}
          </div>
        </div>
        <span className="badge" title={t.overallNote}>{t.estimate}</span>
      </div>
      <p className="muted tiny" style={{ margin: '4px 0 0' }}>{t.overallNote}</p>

      {fix && (
        <section className="priority-fix" aria-labelledby="pf-title" data-testid="priority-fix">
          <h3 id="pf-title">{t.priorityTitle}</h3>
          <p className="muted small" style={{ margin: '0 0 6px' }}>{t.priorityHint}</p>
          {fix.original && (
            <p className="pf-line">
              <s>{fix.original}</s> → <strong>{fix.corrected}</strong>
            </p>
          )}
          {!fix.original && <p className="pf-line"><strong>{fix.corrected}</strong></p>}
          <p className="small" style={{ margin: '6px 0 0' }}>{fix.issue}</p>
          {fix.tip && (
            <p className="small" style={{ margin: '4px 0 0' }}>
              <strong>{t.tip}:</strong> {fix.tip}
            </p>
          )}
        </section>
      )}

      <div className="four-rows">
        {rows.map((r) => (
          <div className="four-row" key={r.key} data-testid={`criterion-${r.key}`}>
            <div className="four-row-top">
              <span>
                {t.criteria[r.key]}
                {r.key === 'pronunciation' && <span className="badge" style={{ marginLeft: 6 }}>{t.estimate}</span>}
              </span>
              <span className={`four-row-band txt-${bandTone(r.band)}`}>{formatBand(r.band)}</span>
            </div>
            {r.reasoning && <div className="why">{r.reasoning}</div>}
            {r.key === 'pronunciation' && <div className="muted tiny">{t.pronunciationNote}</div>}
          </div>
        ))}
      </div>
    </div>
  );
}
