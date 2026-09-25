import { useState, type ReactNode } from 'react';
import { Recorder } from './Recorder';
import { WritingCoach } from './WritingCoach';

type Tab = 'speaking' | 'writing';

function App() {
  const [tab, setTab] = useState<Tab>('speaking');

  return (
    <main style={{ maxWidth: 480, margin: '4rem auto', fontFamily: 'sans-serif' }}>
      <h1>AI Speaking Coach</h1>
      <p>
        Ingliz tilida gapiring yoki yozing — sun'iy intellekt baholab, aniq
        tuzatishlar beradi.
      </p>

      <div style={{ display: 'flex', gap: '0.5rem', marginTop: '1rem' }}>
        <TabButton active={tab === 'speaking'} onClick={() => setTab('speaking')}>
          🎙 Gapirish
        </TabButton>
        <TabButton active={tab === 'writing'} onClick={() => setTab('writing')}>
          ✍️ Yozish
        </TabButton>
      </div>

      {tab === 'speaking' ? <Recorder /> : <WritingCoach />}
    </main>
  );
}

// Ikkala tab ham bir xil ko'rinishda bo'lishi uchun kichik yordamchi
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
