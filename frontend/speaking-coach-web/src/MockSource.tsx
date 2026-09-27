// Mock Listening/Reading uchun test manbai: avval ilovaning tayyor banki;
// hammasi ishlangan bo'lsa — foydalanuvchi o'zi tanlaydi: qayta ishlash yoki AI yangisini tuzsin.
import { useT } from './i18n';
import { mockObjMsg } from './locales/mockObjective';

export type MockSource = 'bank' | 'repeat' | 'ai';

/** /new javobi: test yoki "bank tugadi". */
export interface NewTestResponse<T> {
  test: T | null;
  repeated?: boolean;
  exhausted?: boolean;
  bankCount?: number;
  source?: 'bank' | 'ai';
}

export const withSource = (url: string, source: MockSource) => `${url}${url.includes('?') ? '&' : '?'}source=${source}`;

export function BankExhausted({ bankCount, onPick }: { bankCount: number; onPick: (s: MockSource) => void }) {
  const t = useT(mockObjMsg);
  return (
    <div className="card stack" data-testid="bank-exhausted">
      <h3 style={{ margin: 0 }}>{bankCount > 0 ? t.exhaustedTitle : t.emptyTitle}</h3>
      <p className="muted small" style={{ margin: 0 }}>{bankCount > 0 ? t.exhaustedText(bankCount) : t.emptyText}</p>
      <div className="source-choice">
        {bankCount > 0 && (
          <button className="btn btn-outline" onClick={() => onPick('repeat')}>
            {t.repeatOldest}
          </button>
        )}
        <button className="btn btn-primary" onClick={() => onPick('ai')}>
          {t.aiNew}
        </button>
      </div>
    </div>
  );
}
