// Sahifa ichidagi kutilmagan xato (yoki yangi deploydan so'ng eski bo'lak
// yuklanmay qolishi) butun ilovani oq ekranga aylantirmasin: xabar va
// "Qayta yuklash" tugmasi ko'rsatiladi. `resetKey` o'zgarsa (boshqa sahifa) —
// holat tozalanadi.
import { Component, type ErrorInfo, type ReactNode } from 'react';
import { common, useT } from './i18n';

function Fallback({ onHome }: { onHome?: () => void }) {
  const t = useT(common);
  return (
    <div className="card" role="alert" data-testid="crash" style={{ marginTop: 24 }}>
      <h3 style={{ marginTop: 0 }}>{t.crashTitle}</h3>
      <p className="muted small">{t.crashHint}</p>
      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
        <button className="btn" onClick={() => window.location.reload()}>
          {t.reload}
        </button>
        {onHome && (
          <button className="btn-link" onClick={onHome}>
            {t.home}
          </button>
        )}
      </div>
    </div>
  );
}

interface Props {
  children: ReactNode;
  resetKey: string;
  onHome?: () => void;
}

export class ErrorBoundary extends Component<Props, { failed: boolean; key: string }> {
  state = { failed: false, key: this.props.resetKey };

  static getDerivedStateFromError() {
    return { failed: true };
  }

  static getDerivedStateFromProps(props: Props, state: { failed: boolean; key: string }) {
    return props.resetKey !== state.key ? { failed: false, key: props.resetKey } : null;
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Sahifa xatosi', error, info.componentStack);
  }

  render() {
    return this.state.failed ? <Fallback onHome={this.props.onHome} /> : this.props.children;
  }
}

const RELOAD_KEY = 'speakingCoach.chunkReload';

/**
 * Yangi deploydan keyin eski sahifadagi dinamik import (lazy bo'lak) topilmay
 * qolishi mumkin — sahifani BIR marta avtomatik yangilaymiz (cheksiz aylanmasin).
 */
export function installChunkReload() {
  window.addEventListener('vite:preloadError', (event) => {
    try {
      const last = Number(sessionStorage.getItem(RELOAD_KEY) ?? 0);
      if (Date.now() - last < 60_000) return; // yaqinda yangilangan — xato ErrorBoundary'da ko'rsatiladi
      sessionStorage.setItem(RELOAD_KEY, String(Date.now()));
    } catch {
      return;
    }
    event.preventDefault();
    window.location.reload();
  });
}
