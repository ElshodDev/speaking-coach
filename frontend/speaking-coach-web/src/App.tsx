import { useEffect, useState, type ReactNode } from 'react';
import { Recorder } from './Recorder';
import { WritingCoach } from './WritingCoach';
import { Comprehension } from './Comprehension';
import { AuthPanel } from './AuthPanel';
import { apiJson, getToken, setToken } from './api';

type Tab = 'speaking' | 'writing' | 'reading' | 'listening';

const TABS: { id: Tab; label: string }[] = [
  { id: 'speaking', label: '🎙 Gapirish' },
  { id: 'writing', label: '✍️ Yozish' },
  { id: 'reading', label: "📖 O'qish" },
  { id: 'listening', label: '🎧 Tinglash' },
];

function App() {
  const [tab, setTab] = useState<Tab>('speaking');
  const [email, setEmail] = useState<string | null>(null);

  // Sahifa ochilganda: brauzerda token saqlangan bo'lsa, u hali yaroqlimi
  // deb serverdan so'raymiz. Yaroqsiz bo'lsa (muddati o'tgan / chiqilgan) —
  // o'chirib tashlaymiz, foydalanuvchi mehmon sifatida davom etadi.
  useEffect(() => {
    if (!getToken()) return;
    apiJson<{ email: string }>('/api/auth/me')
      .then((me) => setEmail(me.email))
      .catch(() => setToken(null));
  }, []);

  const loggedIn = email !== null;
  // key: kirish/chiqishda mashq komponentlari qaytadan yaratiladi — tarix
  // yangi foydalanuvchi uchun qayta yuklanadi, eski natijalar ko'rinmaydi.
  const userKey = email ?? 'guest';

  return (
    <main style={{ maxWidth: 560, margin: '3rem auto', padding: '0 16px', fontFamily: 'sans-serif' }}>
      <h1 style={{ marginBottom: '0.5rem' }}>AI Speaking Coach</h1>
      <p style={{ marginTop: 0 }}>
        Ingliz tilida gapiring, yozing, o'qing va tinglang — sun'iy intellekt baholab, aniq tuzatishlar beradi.
      </p>

      <AuthPanel email={email} onChange={setEmail} />

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem', marginTop: '1rem' }}>
        {TABS.map((t) => (
          <TabButton key={t.id} active={tab === t.id} onClick={() => setTab(t.id)}>
            {t.label}
          </TabButton>
        ))}
      </div>

      {tab === 'speaking' && <Recorder key={userKey} loggedIn={loggedIn} />}
      {tab === 'writing' && <WritingCoach key={userKey} loggedIn={loggedIn} />}
      {tab === 'reading' && <Comprehension key={`r-${userKey}`} mode="reading" loggedIn={loggedIn} />}
      {tab === 'listening' && <Comprehension key={`l-${userKey}`} mode="listening" loggedIn={loggedIn} />}
    </main>
  );
}

// Barcha tablar bir xil ko'rinishda bo'lishi uchun kichik yordamchi
// komponent — faol tab boshqasidan rang bilan ajralib turadi.
function TabButton({
  active,
  onClick,
  children,
}: {
  active: boolean;
  onClick: () => void;
  children: ReactNode;
}) {
  return (
    <button
      onClick={onClick}
      style={{
        padding: '0.5rem 1rem',
        fontSize: '0.95rem',
        border: '1px solid #d1d5db',
        borderRadius: '0.5rem',
        backgroundColor: active ? '#2563eb' : 'white',
        color: active ? 'white' : '#374151',
        cursor: 'pointer',
      }}
    >
      {children}
    </button>
  );
}

export default App;
