import { useCallback, useEffect, useState } from 'react';
import { Recorder } from './Recorder';
import { WritingCoach } from './WritingCoach';
import { Comprehension } from './Comprehension';
import { Review, statsPath, type ReviewStats } from './Review';
import { AuthPanel } from './AuthPanel';
import { EXERCISES, ExerciseTiles, Home, type ExerciseKind } from './Home';
import { PageHeader } from './ui';
import { Progress } from './Progress';
import { Admin } from './Admin';
import { Settings } from './Settings';
import { apiJson, getToken, setLevel, setToken, type Profile as ProfileData } from './api';

/**
 * Oddiy "hash" marshrutlash: manzil #/review, #/practice/writing kabi.
 * Tashqi kutubxonasiz, lekin telefondagi "Orqaga" tugmasi va brauzer
 * tarixi to'g'ri ishlaydi — ilova sahifalari haqiqiy sahifalardek.
 */
function useRoute(): [string, (route: string) => void] {
  const read = () => window.location.hash.replace(/^#\/?/, '') || 'home';
  const [route, setRoute] = useState(read);

  useEffect(() => {
    const onChange = () => {
      setRoute(read());
      window.scrollTo(0, 0);
    };
    window.addEventListener('hashchange', onChange);
    return () => window.removeEventListener('hashchange', onChange);
  }, []);

  const go = useCallback((next: string) => {
    window.location.hash = `/${next}`;
  }, []);

  return [route, go];
}

const NAV = [
  { id: 'home', icon: '🏠', label: 'Asosiy' },
  { id: 'review', icon: '🔁', label: 'Takrorlash' },
  { id: 'practice', icon: '🎯', label: 'Mashqlar' },
  { id: 'progress', icon: '📈', label: 'Natijalar' },
  { id: 'profile', icon: '👤', label: 'Profil' },
] as const;

function App() {
  const [route, go] = useRoute();
  const [email, setEmail] = useState<string | null>(null);
  const [stats, setStats] = useState<ReviewStats | null>(null);
  const [profile, setProfile] = useState<ProfileData | null>(null);

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

  // Profil: taxallus, daraja, musobaqa, admin-mi. Serverdagi daraja
  // brauzerdagisidan ustun — boshqa qurilmada tanlangan bo'lsa ham shu yerda
  // qo'llanadi.
  useEffect(() => {
    if (!loggedIn) {
      setProfile(null);
      return;
    }
    apiJson<ProfileData>('/api/profile')
      .then((p) => {
        setProfile(p);
        setLevel(p.level);
      })
      .catch(() => undefined);
  }, [loggedIn, email]);

  // Streak, kunlik maqsad va navbatdagi kartalar soni — bosh sahifa va
  // menyudagi raqam uchun. Mashqdan keyin yangi kartalar qo'shilsa yoki
  // karta takrorlansa, qayta so'raladi.
  const refreshStats = useCallback(() => {
    if (!loggedIn) {
      setStats(null);
      return;
    }
    apiJson<ReviewStats>(statsPath())
      .then(setStats)
      .catch(() => undefined);
  }, [loggedIn]);

  useEffect(() => {
    refreshStats();
  }, [refreshStats]);

  // Kirgandan ham, chiqqandan ham keyin bosh sahifaga qaytamiz: kirgan
  // foydalanuvchi o'z holatini, chiqqan esa mehmon sahifasini ko'radi.
  function onAuthChange(next: string | null) {
    setEmail(next);
    go('home');
  }

  const [section, sub] = route.split('/') as [string, string | undefined];
  const exercise = EXERCISES.find((e) => e.kind === sub);
  // key: kirish/chiqishda mashq ekranlari qaytadan yaratiladi — tarix
  // yangi foydalanuvchi uchun qayta yuklanadi, eski natijalar ko'rinmaydi.
  const userKey = email ?? 'guest';
  const toLogin = () => go('profile');

  let page;
  if (section === 'review') {
    page = <Review key={userKey} loggedIn={loggedIn} onChanged={refreshStats} onLogin={toLogin} />;
  } else if (section === 'practice' && exercise) {
    const common = { loggedIn, onCardsAdded: refreshStats, onLogin: toLogin };
    const k = `${sub}-${userKey}`;
    page = (
      <>
        <PageHeader title={`${exercise.emoji} ${exercise.title}`} subtitle={exercise.blurb} onBack={() => go('practice')} />
        {exercise.kind === 'speaking' && <Recorder key={k} {...common} />}
        {exercise.kind === 'writing' && <WritingCoach key={k} {...common} />}
        {(exercise.kind === 'reading' || exercise.kind === 'listening') && (
          <Comprehension key={k} {...common} mode={exercise.kind} />
        )}
      </>
    );
  } else if (section === 'practice') {
    page = (
      <>
        <PageHeader title="Mashqlar" subtitle="Bittasini tanlang — har biri 3-5 daqiqa oladi." />
        <ExerciseTiles onOpen={(kind: ExerciseKind) => go(`practice/${kind}`)} />
      </>
    );
  } else if (section === 'progress') {
    page = <Progress key={userKey} loggedIn={loggedIn} onLogin={toLogin} go={go} />;
  } else if (section === 'admin' && profile?.isAdmin) {
    page = <Admin />;
  } else if (section === 'profile' || section === 'admin') {
    page = (
      <Profile
        key={userKey}
        email={email}
        profile={profile}
        onAuthChange={onAuthChange}
        onProfileSaved={setProfile}
        go={go}
      />
    );
  } else {
    page = <Home email={email} stats={stats} go={go} />;
  }

  // Admin sahifasiga Profil orqali kiriladi — menyuda ham Profil belgilanadi.
  const activeNav = section === 'admin' ? 'profile' : NAV.some((n) => n.id === section) ? section : 'home';

  return (
    <>
      <div className="app">
        <header className="topbar">
          <a className="brand" href="#/">
            <img src="/icons/icon-192.png" alt="" />
            Speaking Coach
          </a>
          {loggedIn ? (
            stats && <span className="badge">🔥 {stats.streakDays} kun</span>
          ) : (
            <button className="btn-link" onClick={toLogin}>
              Kirish
            </button>
          )}
        </header>

        <nav className="nav" aria-label="Asosiy menyu">
          <div className="nav-inner">
            {NAV.map((n) => (
              <button key={n.id} aria-current={activeNav === n.id ? 'page' : undefined} onClick={() => go(n.id)}>
                <span className="icon" aria-hidden>
                  {n.icon}
                </span>
                {n.label}
                {n.id === 'review' && stats && stats.due > 0 && (
                  <span className="dot" aria-label={`${stats.due} ta karta kutyapti`}>
                    {stats.due}
                  </span>
                )}
              </button>
            ))}
          </div>
        </nav>

        <main>{page}</main>
      </div>
    </>
  );
}

function Profile({
  email,
  profile,
  onAuthChange,
  onProfileSaved,
  go,
}: {
  email: string | null;
  profile: ProfileData | null;
  onAuthChange: (email: string | null) => void;
  onProfileSaved: (p: ProfileData) => void;
  go: (route: string) => void;
}) {
  return (
    <>
      <PageHeader title="Profil" />
      <AuthPanel email={email} onChange={onAuthChange} />
      {/* key: profil serverdan kelganda forma qiymatlari yangilansin */}
      {(!email || profile) && <Settings key={profile ? 'user' : 'guest'} profile={profile} onSaved={onProfileSaved} />}

      {profile?.isAdmin && (
        <div className="card cta">
          <div>
            <strong>🛠 Admin panel</strong>
            <div className="muted small">Foydalanuvchilar, faollik va grafiklar</div>
          </div>
          <button className="btn btn-primary" onClick={() => go('admin')}>
            Ochish
          </button>
        </div>
      )}

      <div className="card">
        <h3>📱 Telefonga o'rnatish</h3>
        <p className="muted small">
          Ilovani bosh ekranga qo'shsangiz, alohida ilova kabi ochiladi va sekin internetda ham tez yuklanadi.
        </p>
        <ul className="small" style={{ paddingLeft: 18, margin: 0 }}>
          <li>
            <strong>Android (Chrome):</strong> ⋮ menyu → "Ilovani o'rnatish" yoki "Bosh ekranga qo'shish"
          </li>
          <li>
            <strong>iPhone (Safari):</strong> "Ulashish" tugmasi → "Bosh ekranga"
          </li>
        </ul>
      </div>

      <div className="card soft small muted">
        Ranglar telefoningiz sozlamasiga moslashadi: tungi rejim yoqilgan bo'lsa, ilova ham qorong'i bo'ladi.
      </div>
    </>
  );
}

export default App;
