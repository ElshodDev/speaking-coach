import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react';
import { Recorder } from './Recorder';
import { WritingCoach } from './WritingCoach';
import { Comprehension } from './Comprehension';
import { Review, statsPath, type ReviewStats } from './Review';
import { AuthPanel } from './AuthPanel';
import { apiJson, getToken, setToken } from './api';

type Tab = 'review' | 'speaking' | 'writing' | 'reading' | 'listening';

function App() {
  const [tab, setTab] = useState<Tab>('speaking');
  const [email, setEmail] = useState<string | null>(null);
  const [due, setDue] = useState(0);

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

  // "Takrorlash" tab'idagi raqam — navbatda turgan kartalar soni. Bu
  // foydalanuvchini qaytib kelishga undaydigan eng oddiy "eslatma".
  const refreshDue = useCallback(() => {
    if (!loggedIn) {
      setDue(0);
      return;
    }
    apiJson<ReviewStats>(statsPath())
      .then((s) => setDue(s.due))
      .catch(() => undefined);
  }, [loggedIn]);

  useEffect(() => {
    refreshDue();
  }, [refreshDue]);

  // Kirgan foydalanuvchida takrorlanadigan kartalar bo'lsa — birinchi
  // bo'lib o'shani ko'rsatamiz: kunlik odat shu yerdan boshlanadi. Faqat
  // bir marta (kirgandan keyingi birinchi natijada) — keyin foydalanuvchi
  // tab'ni o'zi tanlaydi. `due` serverdan kechikib keladi, shuning uchun
  // shart `loggedIn` emas, `due` o'zgarganda tekshiriladi.
  const autoSwitched = useRef(false);
  useEffect(() => {
    if (!loggedIn) {
      autoSwitched.current = false;
      return;
    }
    if (due > 0 && !autoSwitched.current) {
      autoSwitched.current = true;
      setTab((t) => (t === 'speaking' ? 'review' : t));
    }
  }, [loggedIn, due]);

  const tabs: { id: Tab; label: string }[] = [
    { id: 'review', label: due > 0 ? `🔁 Takrorlash (${due})` : '🔁 Takrorlash' },
    { id: 'speaking', label: '🎙 Gapirish' },
    { id: 'writing', label: '✍️ Yozish' },
    { id: 'reading', label: "📖 O'qish" },
    { id: 'listening', label: '🎧 Tinglash' },
  ];

  // key: kirish/chiqishda mashq komponentlari qaytadan yaratiladi — tarix
  // yangi foydalanuvchi uchun qayta yuklanadi, eski natijalar ko'rinmaydi.
  const userKey = email ?? 'guest';

  return (
    <main style={{ maxWidth: 560, margin: '2rem auto', padding: '0 16px', fontFamily: 'system-ui, sans-serif' }}>
      <h1 style={{ marginBottom: '0.5rem' }}>AI Speaking Coach</h1>
      <p style={{ marginTop: 0 }}>
        Ingliz tilida gapiring, yozing, o'qing va tinglang — sun'iy intellekt baholaydi, xatolaringiz esa takrorlash
        kartalariga aylanib, esda qolguncha qaytib keladi.
      </p>

      <AuthPanel email={email} onChange={setEmail} />

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem', marginTop: '1rem' }}>
        {tabs.map((t) => (
          <TabButton key={t.id} active={tab === t.id} onClick={() => setTab(t.id)}>
            {t.label}
          </TabButton>
        ))}
      </div>

      {tab === 'review' && <Review key={userKey} loggedIn={loggedIn} onChanged={refreshDue} />}
      {tab === 'speaking' && <Recorder key={userKey} loggedIn={loggedIn} onCardsAdded={refreshDue} />}
      {tab === 'writing' && <WritingCoach key={userKey} loggedIn={loggedIn} onCardsAdded={refreshDue} />}
      {tab === 'reading' && (
        <Comprehension key={`r-${userKey}`} mode="reading" loggedIn={loggedIn} onCardsAdded={refreshDue} />
      )}
      {tab === 'listening' && (
        <Comprehension key={`l-${userKey}`} mode="listening" loggedIn={loggedIn} onCardsAdded={refreshDue} />
      )}
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
        minHeight: 40,
      }}
    >
      {children}
    </button>
  );
}

export default App;
