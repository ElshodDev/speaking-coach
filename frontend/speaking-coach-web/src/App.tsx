import { lazy, Suspense, useCallback, useEffect, useState } from 'react';
import { Recorder } from './Recorder';
import { WritingCoach } from './WritingCoach';
import { Comprehension } from './Comprehension';
import { Review, statsPath, type ReviewStats } from './Review';
import { AuthPanel } from './AuthPanel';
import { EXERCISES, ExerciseTiles, Home, type ExerciseKind } from './Home';
import { JoinByCode, JoinGroup, TasksPage } from './StudentTasks';
import { savePendingJoin, takePendingJoin } from './groupLogic';
import { teacherMsg } from './locales/teacher';
import { mockMsg } from './locales/mock';
import { shadowingMsg } from './locales/shadowing';
import { PageHeader } from './ui';
import { Settings } from './Settings';
import { Vocab } from './Vocab';
import { UsageNote } from './Usage';
import { AccountData } from './AccountData';
import { TelegramCard } from './TelegramCard';
import { apiJson, getToken, setLevel, setToken, type Profile as ProfileData } from './api';
import { common, LangSelect, useLang, useT } from './i18n';
import { appMsg } from './locales/app';
import { homeMsg } from './locales/home';
import { planMsg } from './locales/plan';
import { Onboarding } from './Onboarding';
import { shouldOnboard } from './planLogic';

// Kam ochiladigan og'ir sahifalar — alohida fayllarda, faqat kerak bo'lganda
// yuklanadi (bosh sahifa, takrorlash va mashqlar tezroq ochiladi).
const MockHub = lazy(() => import('./Mock').then((m) => ({ default: m.MockHub })));
const MockResultView = lazy(() => import('./Mock').then((m) => ({ default: m.MockResultView })));
const MockSpeaking = lazy(() => import('./MockSpeaking').then((m) => ({ default: m.MockSpeaking })));
const ShadowingList = lazy(() => import('./Shadowing').then((m) => ({ default: m.ShadowingList })));
const ShadowingLessonPage = lazy(() => import('./Shadowing').then((m) => ({ default: m.ShadowingLessonPage })));
const MockFullStart = lazy(() => import('./MockFull').then((m) => ({ default: m.MockFullStart })));
const MockFullStep = lazy(() => import('./MockFull').then((m) => ({ default: m.MockFullStep })));
const MockSessionView = lazy(() => import('./MockFull').then((m) => ({ default: m.MockSessionView })));
const MockListening = lazy(() => import('./MockListening').then((m) => ({ default: m.MockListening })));
const MockReading = lazy(() => import('./MockReading').then((m) => ({ default: m.MockReading })));
const MockWriting = lazy(() => import('./MockWriting').then((m) => ({ default: m.MockWriting })));
const CefrWriting = lazy(() => import('./CefrWriting').then((m) => ({ default: m.CefrWriting })));
const TeacherHome = lazy(() => import('./Teacher').then((m) => ({ default: m.TeacherHome })));
const TeacherGroup = lazy(() => import('./Teacher').then((m) => ({ default: m.TeacherGroup })));
const Progress = lazy(() => import('./Progress').then((m) => ({ default: m.Progress })));
const Admin = lazy(() => import('./Admin').then((m) => ({ default: m.Admin })));
const Quiz = lazy(() => import('./Quiz').then((m) => ({ default: m.Quiz })));

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

// Pastki menyuda 5 ta bo'lim — telefonda undan ko'pi siqilib qoladi.
// Profil tepadagi avatar tugmasida (ko'p ilovalardagi odatiy joy).
const NAV = [
  { id: 'home', icon: '🏠' },
  { id: 'review', icon: '🔁' },
  { id: 'vocab', icon: '📚' },
  { id: 'practice', icon: '🎯' },
  { id: 'progress', icon: '📈' },
] as const;

function App() {
  const t = useT(appMsg);
  const tHome = useT(homeMsg);
  const tMock = useT(mockMsg);
  const tShadow = useT(shadowingMsg);
  const c = useT(common);
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

  // Birinchi kirish: maqsad hali so'ralmagan — bosh sahifa o'rniga tanishtiruv.
  useEffect(() => {
    if (shouldOnboard(route, profile)) go('welcome');
  }, [route, profile, go]);

  // Kirgandan ham, chiqqandan ham keyin bosh sahifaga qaytamiz: kirgan
  // foydalanuvchi o'z holatini, chiqqan esa mehmon sahifasini ko'radi.
  function onAuthChange(next: string | null) {
    setEmail(next);
    // Taklif havolasidan kelib, keyin kirgan bo'lsa — o'sha taklifga qaytaramiz.
    const pending = next ? takePendingJoin() : null;
    go(pending ? `join/${pending}` : 'home');
  }

  const [section, sub, third, fourth, fifth] = route.split('/') as (string | undefined)[] as [string, string?, string?, string?, string?];
  const exercise = EXERCISES.find((e) => e.kind === sub);
  // key: kirish/chiqishda mashq ekranlari qaytadan yaratiladi — tarix
  // yangi foydalanuvchi uchun qayta yuklanadi, eski natijalar ko'rinmaydi.
  const userKey = email ?? 'guest';
  const toLogin = () => go('login');

  let page;
  if (section === 'review' && sub === 'vocab') {
    page = (
      <Review
        key={`vocab-${userKey}`}
        scope="vocab"
        loggedIn={loggedIn}
        onChanged={refreshStats}
        onLogin={toLogin}
        onBack={() => go('vocab')}
      />
    );
  } else if (section === 'review') {
    page = <Review key={userKey} loggedIn={loggedIn} onChanged={refreshStats} onLogin={toLogin} />;
  } else if (section === 'vocab') {
    page = <Vocab key={userKey} loggedIn={loggedIn} onLogin={toLogin} onChanged={refreshStats} go={go} />;
  } else if (section === 'practice' && exercise) {
    const common = { loggedIn, onCardsAdded: refreshStats, onLogin: toLogin };
    const k = `${sub}-${userKey}`;
    page = (
      <>
        <PageHeader
          title={`${exercise.emoji} ${tHome.exercises[exercise.kind].title}`}
          subtitle={tHome.exercises[exercise.kind].blurb}
          onBack={() => go('practice')}
        />
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
        <PageHeader title={t.practiceTitle} subtitle={t.practiceSubtitle} />
        <UsageNote userKey={userKey} />
        <ExerciseTiles onOpen={(kind: ExerciseKind) => go(`practice/${kind}`)} />
        <div className="card cta" style={{ marginTop: 16 }} data-testid="shadowing-entry">
          <div>
            <strong>{tShadow.tile}</strong>
            <div className="muted small">{tShadow.tileText}</div>
          </div>
          <button className="btn btn-primary" onClick={() => go('shadowing')}>
            {t.open}
          </button>
        </div>
        <div className="card cta" style={{ marginTop: 16 }}>
          <div>
            <strong>{tMock.tile}</strong>
            <div className="muted small">{tMock.tileText}</div>
          </div>
          <button className="btn btn-primary" onClick={() => go('mock')}>
            {t.open}
          </button>
        </div>
      </>
    );
  } else if (section === 'shadowing' && sub) {
    page = <ShadowingLessonPage key={`${sub}-${userKey}`} id={sub} go={go} loggedIn={loggedIn} onLogin={toLogin} />;
  } else if (section === 'shadowing') {
    page = <ShadowingList go={go} />;
  } else if (section === 'mock' && sub === 'result' && third) {
    page = <MockResultView key={`${third}-${userKey}`} id={third} go={go} />;
  } else if (section === 'teacher' && sub && loggedIn) {
    page = <TeacherGroup key={`${sub}-${userKey}`} id={sub} go={go} />;
  } else if (section === 'teacher' && loggedIn) {
    page = <TeacherHome key={userKey} go={go} />;
  } else if (section === 'join' && sub) {
    page = <JoinGroup key={`${sub}-${userKey}`} code={sub} loggedIn={loggedIn} go={go} onLogin={() => { savePendingJoin(sub); toLogin(); }} />;
  } else if (section === 'quiz') {
    page = <Quiz go={go} loggedIn={loggedIn} />;
  } else if (section === 'welcome' && loggedIn) {
    page = <Onboarding key={userKey} profile={profile} go={go} onSaved={setProfile} />;
  } else if (section === 'tasks' && loggedIn) {
    page = <TasksPage key={userKey} go={go} />;
  } else if (section === 'mock' && sub === 'session' && third && loggedIn) {
    page = <MockSessionView key={`${third}-${userKey}`} sessionId={third} go={go} />;
  } else if (section === 'mock' && sub === 'full' && (third === 'academic' || third === 'general' || third === 'cefr') && fourth && loggedIn) {
    page = <MockFullStep key={route} variant={third} sessionId={fourth} step={Number(fifth ?? 0) || 0} go={go} />;
  } else if (section === 'mock' && sub === 'full' && loggedIn) {
    page = <MockFullStart go={go} />;
  } else if (section === 'mock' && sub === 'cefr-full' && loggedIn) {
    page = <MockFullStart go={go} exam="cefr" />;
  } else if (section === 'mock' && sub === 'cefr-listening' && loggedIn) {
    page = <MockListening key={`cl-${userKey}`} exam="cefr" go={go} />;
  } else if (section === 'mock' && sub === 'cefr-reading' && loggedIn) {
    page = <MockReading key={`cr-${userKey}`} exam="cefr" variant="academic" go={go} />;
  } else if (section === 'mock' && sub === 'listening' && loggedIn) {
    page = <MockListening key={`ml-${userKey}`} go={go} />;
  } else if (section === 'mock' && (sub === 'reading-academic' || sub === 'reading-general') && loggedIn) {
    page = <MockReading key={`${sub}-${userKey}`} variant={sub === 'reading-general' ? 'general' : 'academic'} go={go} />;
  } else if (section === 'mock' && sub === 'cefr-speaking' && loggedIn) {
    page = <MockSpeaking key={`cs-${userKey}`} exam="cefr" go={go} />;
  } else if (section === 'mock' && sub === 'cefr-writing' && loggedIn) {
    page = <CefrWriting key={`cw-${userKey}`} go={go} />;
  } else if (section === 'mock' && sub === 'speaking' && loggedIn) {
    page = <MockSpeaking key={`ms-${userKey}`} go={go} />;
  } else if (section === 'mock' && (sub === 'writing-academic' || sub === 'writing-general') && loggedIn) {
    page = <MockWriting key={`${sub}-${userKey}`} variant={sub === 'writing-general' ? 'general' : 'academic'} go={go} />;
  } else if (section === 'mock') {
    page = <MockHub key={userKey} loggedIn={loggedIn} go={go} onLogin={toLogin} />;
  } else if (section === 'progress') {
    page = <Progress key={userKey} loggedIn={loggedIn} onLogin={toLogin} go={go} />;
  } else if (section === 'admin' && profile?.isAdmin) {
    page = <Admin />;
  } else if (section === 'profile' || section === 'admin' || section === 'login' || section === 'register') {
    page = (
      <Profile
        key={`${userKey}-${loggedIn ? '' : section}`}
        authMode={section === 'register' ? 'register' : 'login'}
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

  // Profil va Admin menyuda yo'q — ularda menyuning hech biri belgilanmaydi.
  const onProfile = section === 'profile' || section === 'admin' || section === 'teacher' || section === 'login' || section === 'register';
  const activeNav = onProfile
    ? null
    : section === 'review' && sub === 'vocab'
      ? 'vocab'
      : section === 'mock' || section === 'shadowing'
        ? 'practice'
      : NAV.some((n) => n.id === section)
        ? section
        : 'home';
  const initial = (profile?.displayName || email || '?').charAt(0).toUpperCase();

  return (
    <>
      <div className="app">
        <header className="topbar">
          <a className="brand" href="#/">
            <img src="/icons/icon-192.png" alt="" />
            <span className="brand-name">Speaking Coach</span>
          </a>
          <div className="topbar-right">
            <LangSelect />
            {loggedIn && stats && <span className="badge streak">{t.streak(stats.streakDays)}</span>}
            {loggedIn ? (
              <button
                className="avatar"
                aria-label={t.profile}
                aria-current={onProfile ? 'page' : undefined}
                onClick={() => go('profile')}
              >
                {initial}
              </button>
            ) : (
              <button className="btn-link" onClick={toLogin}>
                {c.login}
              </button>
            )}
          </div>
        </header>

        <nav className="nav" aria-label={t.navLabel}>
          <div className="nav-inner">
            {NAV.map((n) => (
              <button key={n.id} aria-current={activeNav === n.id ? 'page' : undefined} onClick={() => go(n.id)}>
                <span className="icon" aria-hidden>
                  {n.icon}
                </span>
                {t.nav[n.id]}
                {n.id === 'review' && stats && stats.due > 0 && (
                  <span className="dot" aria-label={t.dueDot(stats.due)}>
                    {stats.due}
                  </span>
                )}
              </button>
            ))}
          </div>
        </nav>

        <main>
          <Suspense fallback={<p className="muted" role="status" style={{ marginTop: 24 }}>⏳</p>}>{page}</Suspense>
        </main>
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
  authMode = 'login',
}: {
  authMode?: 'login' | 'register';
  email: string | null;
  profile: ProfileData | null;
  onAuthChange: (email: string | null) => void;
  onProfileSaved: (p: ProfileData) => void;
  go: (route: string) => void;
}) {
  const tt = useT(teacherMsg);
  const tp = useT(planMsg);
  const t = useT(appMsg);
  const { lang } = useLang();
  const goalText = profile?.goal
    ? [
        tp.goals[profile.goal]?.[0] ?? profile.goal,
        profile.targetScore ? tp.target(profile.targetScore) : null,
        profile.examDate ?? null,
        profile.dailyMinutes ? tp.perDay(profile.dailyMinutes) : null,
      ].filter(Boolean).join(' · ')
    : tp.goalNone;
  // Mehmon: faqat kirish / ro'yxatdan o'tish formasi — boshqa kartalar chalg'itmasin.
  if (!email) {
    return (
      <div style={{ marginTop: 16 }}>
        <AuthPanel email={null} onChange={onAuthChange} initialMode={authMode} />
        <details className="card" style={{ padding: '12px 18px' }} data-testid="guest-settings">
          <summary className="small" style={{ cursor: 'pointer' }}>{t.guestSettings}</summary>
          <Settings key="guest" profile={null} onSaved={onProfileSaved} bare />
        </details>
        <p className="small" style={{ marginTop: 16 }}>
          <a href={`/privacy.html#${lang}`}>{t.privacy}</a>
        </p>
      </div>
    );
  }
  return (
    <>
      <PageHeader title={t.profile} />
      <AuthPanel email={email} onChange={onAuthChange} />
      {/* key: profil serverdan kelganda forma qiymatlari yangilansin */}
      {(!email || profile) && <Settings key={profile ? 'user' : 'guest'} profile={profile} onSaved={onProfileSaved} />}
      {email && <TelegramCard key={email} />}
      {email && profile && (
        <div className="card cta" data-testid="goal-card">
          <div>
            <strong>{tp.goalCard}</strong>
            <div className="muted small">{goalText}</div>
          </div>
          <button className="btn btn-outline" onClick={() => go('welcome')}>{tp.change}</button>
        </div>
      )}
      {email && (
        <div className="card cta" data-testid="teacher-entry">
          <div>
            <strong>{tt.panelTitle}</strong>
            <div className="muted small">{tt.panelText}</div>
          </div>
          <button className="btn btn-primary" onClick={() => go('teacher')}>
            {tt.groupsTitle}
          </button>
        </div>
      )}
      {email && <JoinByCode go={go} />}
      {email && <AccountData email={email} onDeleted={() => onAuthChange(null)} />}

      {profile?.isAdmin && (
        <div className="card cta">
          <div>
            <strong>{t.adminTitle}</strong>
            <div className="muted small">{t.adminText}</div>
          </div>
          <button className="btn btn-primary" onClick={() => go('admin')}>
            {t.open}
          </button>
        </div>
      )}

      <div className="card">
        <h3>{t.installTitle}</h3>
        <p className="muted small">{t.installText}</p>
        <ul className="small" style={{ paddingLeft: 18, margin: 0 }}>
          <li>
            <strong>{t.installAndroid}</strong> {t.installAndroidSteps}
          </li>
          <li>
            <strong>{t.installIphone}</strong> {t.installIphoneSteps}
          </li>
        </ul>
      </div>

      <div className="card soft small muted">{t.themeNote}</div>

      <p className="center small" style={{ marginTop: 16 }}>
        <a href={`/privacy.html#${lang}`}>{t.privacy}</a>
      </p>
    </>
  );
}

export default App;
