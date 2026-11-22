import { useEffect, useId, useMemo, useRef, useState } from 'react';
import { Icon } from './Icon';
import { useT } from './i18n';
import { hubMsg } from './locales/hub';
import { HUB, HUB_GROUPS, searchHub } from './newNav';

/** "Hammasi": barcha bo'limlar va tugmalar qidiruv bilan — hech narsa yashirin qolmasin. */
export function Hub({ go, onClose, hasEmail, isAdmin }: { go: (r: string) => void; onClose: () => void; hasEmail: boolean; isAdmin: boolean }) {
  const t = useT(hubMsg);
  const titleId = useId();
  const [q, setQ] = useState('');
  const input = useRef<HTMLInputElement>(null);

  useEffect(() => {
    input.current?.focus();
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose();
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [onClose]);

  const label = (id: string) => t.items[id] ?? { title: id, text: '' };
  // Kirmaganlarga guruh va hisob ichidagi bo'limlar ko'rsatilmaydi.
  const base = useMemo(() => HUB.filter((i) => hasEmail || !['teacher', 'join', 'referral', 'telegram', 'data'].includes(i.id)), [hasEmail]);
  const items = useMemo(() => searchHub(base, q, label), [base, q, t]); // eslint-disable-line react-hooks/exhaustive-deps

  function open(route: string) {
    onClose();
    go(route);
  }

  return (
    <div className="hub-back" onClick={onClose} data-testid="hub">
      <div className="hub" role="dialog" aria-modal="true" aria-labelledby={titleId} onClick={(e) => e.stopPropagation()}>
        <div className="hub-head">
          <h2 id={titleId}>{t.allTitle}</h2>
          <button className="hub-x" onClick={onClose} aria-label={t.close}>
            <Icon name="x" size={18} />
          </button>
        </div>
        <label className="hub-search">
          <Icon name="search" size={17} />
          <input ref={input} type="search" value={q} onChange={(e) => setQ(e.target.value)} placeholder={t.searchPlaceholder} aria-label={t.searchLabel} data-testid="hub-search" />
        </label>
        <div className="hub-body">
          {HUB_GROUPS.map((g) => {
            const list = items.filter((i) => i.group === g);
            if (!list.length) return null;
            return (
              <section key={g}>
                <h3 className="hub-group">{t.groups[g]}</h3>
                {list.map((i) => (
                  <button key={i.id} className="hub-row" onClick={() => open(i.route)} data-testid={`hub-${i.id}`}>
                    <span className="hub-ico"><Icon name={i.icon} size={19} /></span>
                    <span className="hub-txt">
                      <b>{label(i.id).title}</b>
                      <small>{label(i.id).text}</small>
                    </span>
                    <Icon name="arrow" size={15} className="hub-arr" />
                  </button>
                ))}
              </section>
            );
          })}
          {isAdmin && !q && (
            <section>
              <h3 className="hub-group">Admin</h3>
              <button className="hub-row" onClick={() => open('admin')}>
                <span className="hub-ico"><Icon name="gear" size={19} /></span>
                <span className="hub-txt"><b>Admin</b></span>
                <Icon name="arrow" size={15} className="hub-arr" />
              </button>
            </section>
          )}
          {!items.length && <p className="muted hub-empty" role="status">{t.noResults}</p>}
        </div>
      </div>
    </div>
  );
}
