import { useEffect, useState } from 'react';
import { apiJson } from './api';
import { BarChart } from './charts';
import { useT } from './i18n';
import { fluencyMsg } from './locales/fluency';
import { fillerLevel, fluencyTips, paceLevel, type FluencyPoint, type FluencyStats } from './fluencyLogic';

/** Speaking natijasi ostida: sur'at, to'ldiruvchi so'zlar, pauzalar, takrorlar va 1–3 maslahat. */
export function FluencyCard({ metrics }: { metrics: FluencyStats }) {
  const t = useT(fluencyMsg);
  const tips = fluencyTips(metrics);
  const pace = paceLevel(metrics.pace);
  const fill = fillerLevel(metrics.fillersPerMinute);
  return (
    <section style={{ borderTop: '1px solid var(--border)', marginTop: 16, paddingTop: 14 }} data-testid="fluency-card" aria-labelledby="fluency-title">
      <h3 id="fluency-title" style={{ marginBottom: 2 }}>{t.title}</h3>
      <p className="muted tiny" style={{ marginTop: 0 }}>{t.subtitle}</p>
      <div className="stats" style={{ marginTop: 10 }}>
        <div className="stat">
          <div className={`value${pace ? ` txt-${pace}` : ''}`}>{metrics.wpm ?? '—'}</div>
          <div className="label">{t.wpm}</div>
          <div className="label">{t.pace[metrics.pace]}</div>
        </div>
        <div className="stat">
          <div className={`value${fill ? ` txt-${fill}` : ''}`}>{metrics.fillers}</div>
          <div className="label">{t.fillers}</div>
          {metrics.fillersPerMinute !== null && <div className="label">{t.perMinute(metrics.fillersPerMinute)}</div>}
        </div>
        <div className="stat">
          <div className="value">{metrics.longPauses ?? '—'}</div>
          <div className="label">{t.longPauses}</div>
        </div>
      </div>
      <p className="small" style={{ marginBottom: 4 }}>
        {metrics.repetitions} {t.repetitions} · {metrics.selfCorrections} {t.selfCorrections}
        {metrics.topFillers.length > 0 && (
          <>
            {' · '}
            {t.topFillers}{' '}
            {metrics.topFillers.map((f) => (
              <span key={f.word} className="badge" style={{ marginRight: 4 }}>
                {f.word} ×{f.count}
              </span>
            ))}
          </>
        )}
      </p>
      {metrics.pace === 'unknown' ? (
        <p className="muted tiny">{t.noTiming}</p>
      ) : (
        <p className="muted tiny" style={{ marginTop: 0 }}>{t.paceHint}</p>
      )}
      {tips.length > 0 && (
        <ul className="small" style={{ paddingLeft: 18, marginBottom: 6 }}>
          {tips.map((k) => (
            <li key={k} style={{ marginBottom: 4 }}>{t.tips[k]}</li>
          ))}
        </ul>
      )}
      <p className="muted tiny" style={{ marginBottom: 0 }}>{t.estimate}</p>
    </section>
  );
}

/** Progress → Gapirish: so'z/daqiqa va to'ldiruvchi so'zlar dinamikasi. */
export function FluencyTrend() {
  const t = useT(fluencyMsg);
  const [points, setPoints] = useState<FluencyPoint[] | null>(null);

  useEffect(() => {
    let alive = true;
    apiJson<FluencyPoint[]>('/api/speaking/fluency')
      .then((p) => alive && setPoints(p))
      .catch(() => alive && setPoints([]));
    return () => {
      alive = false;
    };
  }, []);

  if (points === null) return null;
  const wpm = points.filter((p) => p.wpm !== null).map((p) => ({ date: p.at, value: p.wpm! }));
  const fillers = points.filter((p) => p.fillersPerMinute !== null).map((p) => ({ date: p.at, value: p.fillersPerMinute! }));
  return (
    <div style={{ marginTop: 16 }} data-testid="fluency-trend">
      <h4 style={{ margin: '0 0 2px' }}>{t.trendTitle}</h4>
      <p className="muted tiny" style={{ margin: '0 0 6px' }}>{t.trendSources}</p>
      {wpm.length >= 2 ? (
        <>
          <p className="muted tiny" style={{ margin: '0 0 4px' }}>{t.trendWpm} · {t.paceHint}</p>
          <BarChart data={wpm} label={t.trendWpm} />
          <p className="muted tiny" style={{ margin: '10px 0 4px' }}>{t.trendFillers}</p>
          <BarChart data={fillers} label={t.trendFillers} />
        </>
      ) : (
        <p className="muted small">{t.trendEmpty}</p>
      )}
    </div>
  );
}
