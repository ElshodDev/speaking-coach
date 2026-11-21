import { lazy, Suspense, useCallback, useEffect, useRef, useState } from 'react';
import { onRoute } from './analytics';
import { MicConsentHost } from './consent';
import { statsPath, type ReviewStats } from './reviewStats';
import { EXERCISES, ExerciseTiles, Home, type ExerciseKind } from './Home';
import { JoinByCode, JoinGroup, TasksPage } from './StudentTasks';
import { savePendingJoin, takePendingJoin } from './groupLogic';
import { teacherMsg } from './locales/teacher';
import { mockPickMsg, type MockPickTitle } from './locales/mockPick';
import { networkMsg } from './locales/network';
import { Logo } from './Logo';
import { PageHeader } from './ui';
import { UsageNote } from './Usage';
import { TelegramCard } from './TelegramCard';
import { ReferralCard } from './ReferralCard';
import { ErrorBoundary } from './ErrorBoundary';
import { DemoBanner, DemoOffer, DemoStart, isDemoEmail } from './Demo';
import { cleanLaunchUrl, genuineTelegram, inTelegram, readyTelegram, syncTgBack, telegramLaunch } from './tgApp';
import {
  accountLabel,
  ApiError,
  AUTH_EXPIRED_EVENT,
  apiFetch,
  apiJson,
  getSavedEmail,
  getToken,
  postJson,
  setLevel,
  setSavedEmail,
  setToken,
  sameSession,
  type Profile as ProfileData,
} from './api';
import { ServerWake } from './ServerWake';
import { Footer } from './Footer';
import { setIsAdmin } from './adminFlag';
import { confirmLeave } from './leaveGuard';
import { common, LangSelect, useT } from './i18n';
import { appMsg } from './locales/app';
import { Icon } from './Icon';
import { homeMsg } from './locales/home';
import { planMsg } from './locales/plan';
import { shouldOnboard } from './planLogic';

// Kam ochiladigan og'ir sahifalar — alohida fayllarda, faqat kerak bo'lganda
// yuklanadi (bosh sahifa tezroq ochiladi). Ularning matnlari (locales) ham birga keladi.
const AuthPanel = lazy(() => import('./AuthPanel').then((m) => ({ default: m.AuthPanel })));
const Review = lazy(() => import('./Review').then((m) => ({ default: m.Review })));
const Vocab = lazy(() => import('./Vocab').then((m) => ({ default: m.Vocab })));
const MockPicker = lazy(() => import('./MockPicker').then((m) => ({ default: m.MockPicker })));
const SignInMethods = lazy(() => import('./SignInMethods').then((m) => ({ default: m.SignInMethods })));
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
const Mistakes = lazy(() => import('./Mistakes').then((m) => ({ default: m.Mistakes })));
const TalkHub = lazy(() => import('./Talk').then((m) => ({ default: m.TalkHub })));
const Part2Page = lazy(() => import('./Part2').then((m) => ({ default: m.Part2Page })));
const TalkSession = lazy(() => import('./Talk').then((m) => ({ default: m.TalkSession })));
const Dictation = lazy(() => import('./Dictation').then((m) => ({ default: m.Dictation })));
const Recorder = lazy(() => import('./Recorder').then((m) => ({ default: m.Recorder })));
const WritingCoach = lazy(() => import('./WritingCoach').then((m) => ({ default: m.WritingCoach })));
const Comprehension = lazy(() => import('./Comprehension').then((m) => ({ default: m.Comprehension })));
const Onboarding = lazy(() => import('./Onboarding').then((m) => ({ default: m.Onboarding })));
const Settings = lazy(() => import('./Settings').then((m) => ({ default: m.Settings })));
const AccountData = lazy(() => import('./AccountData').then((m) => ({ default: m.AccountData })));
const LearnHub = lazy(() => import('./Learn').then((m) => ({ default: m.LearnHub })));
const GrammarLessonPage = lazy(() => import('./Learn').then((m) => ({ default: m.GrammarLessonPage })));
const VocabTopicPage = lazy(() => import('./Learn').then((m) => ({ default: m.VocabTopicPage })));
const L1Quiz = lazy(() => import('./L1Traps').then((m) => ({ default: m.L1Quiz })));

/**
 * Oddiy "hash" marshrutlash: manzil #/review, #/practice/writing kabi.
 * Tashqi kutubxonasiz, lekin telefondagi "Orqaga" tugmasi va brauzer
 * tarixi to'g'ri ishlaydi — ilova sahifalari haqiqiy sahifalardek.
 */
function useRoute(): [string, (route: string) => void] {
  const read = () => window.location.hash.replace(/^#\/?/, '') || 'home';
  const [route, setRoute] = useState(read);
  const routeRef = useRef(route);

  useEffect(() => {
    const onChange = () => {
      const next = read();
      // Ochiq imtihon (leaveGuard): "Orqaga", Telegram BackButton, havola — avval so'raymiz.
      // Rad etilsa — manzilni joyiga qaytaramiz (replaceState hashchange chaqirmaydi).
      if (next !== routeRef.current && !confirmLeave()) {
        window.history.replaceState(null, '', `#/${routeRef.current}`);
        return;
      }
      routeRef.current = next;
      setRoute(next);
      window.scrollTo(0, 0);
    };
    window.addEventListener('hashchange', onChange);
    return () => window.removeEventListener('hashchange', onChange);
  }, []);

  const go = useCallback((next: string) => {
    if (next !== routeRef.current && !confirmLeave()) return;
    window.location.hash = `/${next}`;
  }, []);

  return [route, go];
}

// Pastki menyuda 5 ta bo'lim — telefonda undan ko'pi siqilib qoladi.
// Profil tepadagi avatar tugmasida (ko'p ilovalardagi odatiy joy).
const NAV = [{ id: 'home' }, { id: 'review' }, { id: 'vocab' }, { id: 'practice' }, { id: 'progress' }] as const;

function App() {
  const t = useT(appMsg);
  const tHome = useT(homeMsg);
  const tMockPick = useT(mockPickMsg);
  const tNet = useT(networkMsg);
  const c = useT(common);
  const [route, go] = useRoute();
  // Analitika: sahifa ko'rinishi va marshrutdan kelib chiqadigan hodisalar (faqat VITE_PLAUSIBLE_DOMAIN berilganda).
  useEffect(() => onRoute(route), [route]);
  // Oxirgi kirgan hisob darhol tiklanadi: server uyg'onguncha (30–60 s) ham
  // ilova "kirgan" holatda turadi — mehmon ekrani chiqib, ish yo'qolmaydi.
  const [email, setEmailState] = useState<string | null>(getSavedEmail);
  const setEmail = useCallback((next: string | null) => {
    setSavedEmail(next);
    setEmailState(next);
  }, []);
  const [stats, setStats] = useState<ReviewStats | null>(null);
  const [profile, setProfile] = useState<ProfileData | null>(null);
  // Sessiya tugadi (server tokenni tanimadi) — api.ts tokenni o'chirib, hodisa yuboradi.
  // Hisobdan chiqaramiz va "qaytadan kiring" ogohlantirishini ko'rsatamiz.
  const [sessionExpired, setSessionExpired] = useState(false);
  useEffect(() => {
    const onExpired = () => {
      setEmail(null);
      setSessionExpired(true);
    };
    window.addEventListener(AUTH_EXPIRED_EVENT, onExpired);
    return () => window.removeEventListener(AUTH_EXPIRED_EVENT, onExpired);
  }, [setEmail]);
  // Qayta kirdi (yoki Telegram o'zi kiritdi) — ogohlantirish kerak emas.
  useEffect(() => {
    if (email) setSessionExpired(false);
  }, [email]);

  // Sahifa ochilganda: brauzerda token bo'lsa — hali yaroqlimi deb serverdan
  // so'raymiz. Faqat 401 (token yaroqsiz) — chiqarib yuboramiz; tarmoq xatosi yoki
  // server uyg'onayotgan bo'lsa (Render: 30–60 s) — hisobdan chiqarmay, qayta so'raymiz.
  // Telegram ichida (Mini App): token yo'q yoki eskirgan bo'lsa — initData bilan
  // parolsiz kirish (hisob bo'lmasa, server o'zi ochadi), keyin kerakli bo'lim.
  useEffect(() => {
    const launch = telegramLaunch;
    let cancelled = false;
    let timer: number | undefined;

    const telegramLogin = async () => {
      // Faqat haqiqiy Telegram ichida — begona havola orqali boshqa hisobga kiritib bo'lmasin.
      if (!launch?.initData || !genuineTelegram()) return;
      try {
        const r = await postJson<{ token: string; email: string }>('/api/auth/telegram', { initData: launch.initData });
        if (cancelled) return;
        setToken(r.token);
        setEmail(r.email);
      } catch {
        // Telegram tasdiqlamadi yoki server javob bermadi — mehmon sifatida davom etadi.
      }
    };

    const check = (attempt: number) => {
      // Javob kelguncha foydalanuvchi chiqib (yoki boshqa hisobga kirib) ulgurgan bo'lsa — eski javobni tashlaymiz.
      const same = sameSession();
      apiJson<{ email: string }>('/api/auth/me')
        .then((me) => {
          if (!cancelled && same()) setEmail(me.email);
        })
        .catch(async (e) => {
          if (cancelled) return;
          if (e instanceof ApiError && e.status === 401) {
            // Eskirgan tokenni api.ts o'zi o'chirgan. Shu orada qayta kirgan bo'lsa — tegmaymiz.
            if (getToken()) return;
            setEmail(null);
            await telegramLogin();
          } else if (same() && attempt < 4) {
            timer = window.setTimeout(() => check(attempt + 1), 5000 * (attempt + 1));
          }
        });
    };

    (async () => {
      if (launch) await readyTelegram();
      if (getToken()) check(0);
      else await telegramLogin();
      if (launch) cleanLaunchUrl(launch.route);
    })();
    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, [setEmail]);

  const loggedIn = email !== null;

  // Profil: taxallus, daraja, musobaqa, admin-mi. Serverdagi daraja
  // brauzerdagisidan ustun — boshqa qurilmada tanlangan bo'lsa ham shu yerda
  // qo'llanadi.
  useEffect(() => {
    if (!loggedIn) {
      setProfile(null);
      return;
    }
    // Chiqish / demo'dan chiqish / boshqa hisob: eski so'rov bekor qilinadi, kechikkan
    // javob oldingi foydalanuvchi profilini qaytarib qo'ymasin.
    const controller = new AbortController();
    const same = sameSession();
    apiJson<ProfileData>('/api/profile', { signal: controller.signal })
      .then((p) => {
        if (controller.signal.aborted || !same()) return;
        setProfile(p);
        setLevel(p.level);
      })
      .catch(() => undefined);
    return () => controller.abort();
  }, [loggedIn, email]);

  const reloadProfile = useCallback(() => {
    const same = sameSession();
    apiJson<ProfileData>('/api/profile')
      .then((p) => {
        if (same()) setProfile(p);
      })
      .catch(() => undefined);
  }, []);

  // Ichki vositalar (barqarorlik testi) faqat admin'ga.
  useEffect(() => setIsAdmin(!!profile?.isAdmin), [profile]);

  // Telegram'ning "‹ Orqaga" tugmasi: bosh sahifadan boshqa joyda ko'rinadi.
  useEffect(() => {
    if (!inTelegram()) return;
    syncTgBack(route !== 'home', () => {
      if (window.history.length > 1) window.history.back();
      else go('home');
    });
  }, [route, go]);

  // Streak, kunlik maqsad va navbatdagi kartalar soni — bosh sahifa va
  // menyudagi raqam uchun. Mashqdan keyin yangi kartalar qo'shilsa yoki
  // karta takrorlansa, qayta so'raladi.
  // statsSeq: faqat eng oxirgi so'rov javobi qo'llanadi (javoblar teskari tartibda kelishi mumkin).
  const statsSeq = useRef(0);
  const refreshStats = useCallback(() => {
    const seq = ++statsSeq.current;
    if (!loggedIn) {
      setStats(null);
      return;
    }
    const same = sameSession();
    apiJson<ReviewStats>(statsPath())
      .then((s) => {
        if (seq === statsSeq.current && same()) setStats(s);
      })
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

  const onDemoStarted = useCallback((next: string) => {
    setEmail(next);
    go('home');
  }, [go]);

  // Demo'dan o'z hisobiga: demo sessiyasini yopib, ro'yxatdan o'tish formasi.
  async function leaveDemo() {
    await apiFetch('/api/auth/logout', { method: 'POST' }).catch(() => undefined);
    setToken(null);
    setEmail(null);
    go('register');
  }

  const [section, sub, third, fourth, fifth] = route.split('/') as (string | undefined)[] as [string, string?, string?, string?, string?];
  const exercise = EXERCISES.find((e) => e.kind === sub);
  // key: kirish/chiqishda mashq ekranlari qaytadan yaratiladi — tarix
  // yangi foydalanuvchi uchun qayta yuklanadi, eski natijalar ko'rinmaydi.
  const userKey = email ?? 'guest';
  const toLogin = () => go('login');

  let page;
  if (section === 'demo') {
    page = <DemoStart email={email} onStarted={onDemoStarted} go={go} />;
  } else if (section === 'learn' && sub === 'grammar' && third) {
    page = <GrammarLessonPage key={third} id={third} go={go} />;
  } else if (section === 'learn' && sub === 'words' && third) {
    page = <VocabTopicPage key={`${third}-${userKey}`} id={third} go={go} loggedIn={loggedIn} onLogin={toLogin} />;
  } else if (section === 'learn' && sub === 'traps' && third) {
    page = <L1Quiz key={`${third}-${userKey}`} setKey={third} go={go} loggedIn={loggedIn} onCardsAdded={refreshStats} />;
  } else if (section === 'learn') {
    page = <LearnHub tab={sub === 'words' ? 'words' : sub === 'traps' ? 'traps' : 'grammar'} go={go} />;
  } else if (section === 'review' && sub === 'vocab') {
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
          title={tHome.exercises[exercise.kind].title}
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
        <h2 className="section-title">{t.sections.basic}</h2>
        <ExerciseTiles onOpen={(kind: ExerciseKind) => go(`practice/${kind}`)} />
        <h2 className="section-title">{t.sections.exam}</h2>
        <div className="card-grid">
          {(['mock', 'part2'] as const).map((k) => <PracticeLink key={k} id={k} go={go} />)}
        </div>
        <h2 className="section-title">{t.sections.more}</h2>
        <div className="card-grid">
          {(['talk', 'dictation', 'shadowing', 'learn'] as const).map((k) => <PracticeLink key={k} id={k} go={go} />)}
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
  } else if (section === 'mock' && loggedIn && sub && MOCK_PICK[sub] && (!third || (third === 't' && !fourth))) {
    // Bo'lim sahifasi: 10+ tayyor test/variant ro'yxati — foydalanuvchi o'zi tanlaydi.
    const m = MOCK_PICK[sub];
    page = <MockPicker key={`pick-${sub}-${userKey}`} exam={m.exam} module={m.module} variant={m.variant} allowAi={m.module === 'listening' || m.module === 'reading'} base={`mock/${sub}`} title={tMockPick[m.title]} go={go} />;
  } else if (section === 'mock' && loggedIn && sub && MOCK_PICK[sub]) {
    // third: "t" + id — tanlangan test; "ai" — AI yangisini tuzadi; "next" — keyingi ishlanmagan.
    const m = MOCK_PICK[sub];
    const picked = third === 't' && fourth ? decodeURIComponent(fourth) : undefined;
    const source = third === 'ai' ? 'ai' : 'bank';
    const k = `${sub}-${userKey}-${third ?? ''}-${fourth ?? ''}`;
    if (m.module === 'listening') page = <MockListening key={k} exam={m.exam} go={go} source={source} testId={picked} />;
    else if (m.module === 'reading') page = <MockReading key={k} exam={m.exam} variant={m.variant ?? 'academic'} go={go} source={source} testId={picked} />;
    else if (m.module === 'speaking') page = <MockSpeaking key={k} exam={m.exam} go={go} setId={picked} />;
    else if (m.exam === 'cefr') page = <CefrWriting key={k} go={go} setId={picked} />;
    else page = <MockWriting key={k} variant={m.variant ?? 'academic'} go={go} setId={picked} />;
  } else if (section === 'mock') {
    page = <MockHub key={userKey} loggedIn={loggedIn} go={go} onLogin={toLogin} />;
  } else if (section === 'talk' && sub) {
    page = <TalkSession key={`${sub}-${userKey}`} scenario={sub} loggedIn={loggedIn} onLogin={toLogin} onCardsAdded={refreshStats} go={go} />;
  } else if (section === 'talk') {
    page = <TalkHub go={go} />;
  } else if (section === 'part2') {
    page = <Part2Page key={`${sub ?? ''}-${userKey}`} cardId={sub} loggedIn={loggedIn} onLogin={toLogin} onCardsAdded={refreshStats} go={go} />;
  } else if (section === 'dictation') {
    page = <Dictation key={userKey} loggedIn={loggedIn} go={go} />;
  } else if (section === 'mistakes') {
    page = <Mistakes key={userKey} loggedIn={loggedIn} onLogin={toLogin} go={go} />;
  } else if (section === 'progress') {
    page = <Progress key={userKey} loggedIn={loggedIn} onLogin={toLogin} go={go} name={profile?.displayName ?? (email ? accountLabel(email).split('@')[0] : null)} streak={stats?.streakDays ?? 0} />;
  } else if (section === 'admin' && profile?.isAdmin) {
    page = <Admin />;
  } else if (section === 'profile' || section === 'admin' || section === 'login' || section === 'register') {
    page = (
      <Profile
        key={loggedIn ? 'user' : `guest-${section}`}
        authMode={section === 'register' ? 'register' : 'login'}
        email={email}
        profile={profile}
        onAuthChange={onAuthChange}
        onProfileSaved={setProfile}
        onEmailChanged={(next) => {
          setEmail(next);
          reloadProfile();
        }}
        onReloadProfile={reloadProfile}
        go={go}
      />
    );
  } else {
    page = <Home email={email} stats={stats} go={go} displayName={profile?.displayName} />;
  }

  // Profil va Admin menyuda yo'q — ularda menyuning hech biri belgilanmaydi.
  const onProfile = section === 'profile' || section === 'admin' || section === 'teacher' || section === 'login' || section === 'register';
  const activeNav = onProfile
    ? null
    : section === 'review' && sub === 'vocab'
      ? 'vocab'
      : section === 'mock' || section === 'shadowing' || section === 'learn' || section === 'talk' || section === 'dictation' || section === 'part2'
        ? 'practice'
      : section === 'mistakes'
        ? 'progress'
      : NAV.some((n) => n.id === section)
        ? section
        : 'home';
  const initial = (profile?.displayName || accountLabel(email) || '?').charAt(0).toUpperCase();

  return (
    <>
      <ServerWake />
      <div className="app">
        <header className="topbar">
          <a className="brand" href="#/">
            <Logo size={30} />
            <span className="brand-name">FluentUZ</span>
          </a>
          <div className="topbar-right">
            <LangSelect />
            {loggedIn && stats && stats.streakDays > 0 && <span className="badge streak"><Icon name="flame" size={14} className="streak-ico" /> {t.streak(stats.streakDays)}</span>}
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
          {/* Kompyuterda menyu chap tomonda: logo uning tepasida. */}
          <a className="brand nav-brand" href="#/">
            <Logo size={30} />
            <span className="brand-name">FluentUZ</span>
          </a>
          <div className="nav-inner">
            {NAV.map((n) => (
              <button key={n.id} aria-current={activeNav === n.id ? 'page' : undefined} onClick={() => go(n.id)}>
                <span className="icon" aria-hidden>
                  <Icon name={n.id} size={22} />
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

        <MicConsentHost />
        <main className={pageWidth(section, sub, third)}>
          {isDemoEmail(email) && section !== 'demo' && <DemoBanner onRegister={leaveDemo} />}
          {sessionExpired && !loggedIn && (
            <div className="demo-banner" role="alert" data-testid="session-expired">
              <span>{tNet.sessionExpired}</span>
              {section !== 'login' && (
                <button className="btn-link" onClick={toLogin}>{c.login} →</button>
              )}
            </div>
          )}
          <ErrorBoundary resetKey={route} onHome={() => go('home')}>
            <Suspense fallback={<p className="muted" role="status" style={{ marginTop: 24 }}>…</p>}>{page}</Suspense>
          </ErrorBoundary>
        </main>
        <Footer section={section} />
      </div>
    </>
  );
}

/** Kompyuterda ham tor qoladigan sahifalar: formalar va bittalik kartalar keng ekranda cho'zilmasin. */
const NARROW = new Set(['login', 'register', 'welcome', 'demo', 'join', 'quiz']);
/** O'rtacha kenglik: ketma-ket oqim (speaking, listening) va natijalar — satrlar juda uzun bo'lmasin. */
const MEDIUM_MOCK = /^(speaking|cefr-speaking|listening|cefr-listening|result|session)$/;

/** Mock bo'limlari: marshrut → imtihon, bo'lim va sarlavha (ro'yxat sahifasi va tanlangan test uchun). */
const MOCK_PICK: Record<string, { exam: 'ielts' | 'cefr'; module: 'listening' | 'reading' | 'speaking' | 'writing'; variant?: 'academic' | 'general'; title: MockPickTitle }> = {
  listening: { exam: 'ielts', module: 'listening', title: 'ieltsListening' },
  'reading-academic': { exam: 'ielts', module: 'reading', variant: 'academic', title: 'ieltsReadingA' },
  'reading-general': { exam: 'ielts', module: 'reading', variant: 'general', title: 'ieltsReadingG' },
  speaking: { exam: 'ielts', module: 'speaking', title: 'ieltsSpeaking' },
  'writing-academic': { exam: 'ielts', module: 'writing', variant: 'academic', title: 'ieltsWritingA' },
  'writing-general': { exam: 'ielts', module: 'writing', variant: 'general', title: 'ieltsWritingG' },
  'cefr-listening': { exam: 'cefr', module: 'listening', title: 'cefrListening' },
  'cefr-reading': { exam: 'cefr', module: 'reading', title: 'cefrReading' },
  'cefr-speaking': { exam: 'cefr', module: 'speaking', title: 'cefrSpeaking' },
  'cefr-writing': { exam: 'cefr', module: 'writing', title: 'cefrWriting' },
};

function pageWidth(section: string, sub?: string, third?: string): string {
  if (NARROW.has(section)) return 'page-narrow';
  // Testlar ro'yxati — keng ekranda bir necha ustun.
  if (section === 'mock' && sub && MOCK_PICK[sub] && !third) return 'page-wide';
  if (section === 'mock' && sub && MEDIUM_MOCK.test(sub)) return 'page-medium';
  return 'page-wide';
}

function Profile({
  email,
  profile,
  onAuthChange,
  onProfileSaved,
  onEmailChanged,
  onReloadProfile,
  go,
  authMode = 'login',
}: {
  authMode?: 'login' | 'register';
  email: string | null;
  profile: ProfileData | null;
  onAuthChange: (email: string | null) => void;
  onProfileSaved: (p: ProfileData) => void;
  onEmailChanged: (email: string) => void;
  onReloadProfile: () => void;
  go: (route: string) => void;
}) {
  const tt = useT(teacherMsg);
  const tp = useT(planMsg);
  const t = useT(appMsg);
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
        <DemoOffer go={go} />
        <details className="card" style={{ padding: '12px 18px' }} data-testid="guest-settings">
          <summary className="small" style={{ cursor: 'pointer' }}>{t.guestSettings}</summary>
          <Settings key="guest" profile={null} onSaved={onProfileSaved} bare />
        </details>
      </div>
    );
  }
  return (
    <>
      <PageHeader title={t.profile} />
      <div className="masonry">
      <AuthPanel email={email} onChange={onAuthChange} displayName={profile?.displayName} />
      {profile?.signIn && !isDemoEmail(email) && (
        <SignInMethods methods={profile.signIn} onEmailChanged={onEmailChanged} onPasswordSaved={onReloadProfile} />
      )}
      {/* key: profil serverdan kelganda forma qiymatlari yangilansin */}
      {(!email || profile) && <Settings key={profile ? 'user' : 'guest'} profile={profile} onSaved={onProfileSaved} />}
      {email && !isDemoEmail(email) && <TelegramCard key={email} />}
      {email && !isDemoEmail(email) && <ReferralCard key={`ref-${email}`} />}
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
      </div>
    </>
  );
}

export default App;

/** Mashqlar sahifasidagi bo'lim kartasi: butun karta bosiladi (bir xil "Ochish" tugmalari o'rniga). */
function PracticeLink({ id, go }: { id: 'learn' | 'part2' | 'talk' | 'dictation' | 'shadowing' | 'mock'; go: (route: string) => void }) {
  const t = useT(appMsg);
  return (
    <button className="card cta link-card" onClick={() => go(id)} data-testid={`${id}-entry`}>
      <span className="link-icon"><Icon name={id} size={22} /></span>
      <div style={{ flex: 1 }}>
        <strong>{t.tiles[id].title}</strong>
        <div className="muted small">{t.tiles[id].text}</div>
      </div>
      <Icon name="arrow" className="link-arrow" />
    </button>
  );
}
