import { Recorder } from './Recorder';

function App() {
  return (
    <main style={{ maxWidth: 480, margin: '4rem auto', fontFamily: 'sans-serif' }}>
      <h1>Speaking Coach — Kun 1 sinovi</h1>
      <p>Maqsad: mikrofondan yozib, serverga yuborish ishlashini tekshirish.</p>
      <Recorder />
    </main>
  );
}

export default App;
