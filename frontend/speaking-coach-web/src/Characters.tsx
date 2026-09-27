// Shadowing qahramonlari — ilovaning o'z (original) SVG rasmlari. Gapirayotganda
// og'zi ochilib-yopiladi; ovoz brauzerning speechSynthesis'idan (CHARACTER_VOICE).
import { useEffect, useState } from 'react';
import type { CharacterName } from './shadowLogic';

const INK = '#2b2f3a';

function useMouth(talking: boolean): number {
  // 0 — yopiq, 1 — yarim, 2 — to'liq ochiq
  const [open, setOpen] = useState(0);
  useEffect(() => {
    if (!talking) {
      setOpen(0);
      return;
    }
    const id = window.setInterval(() => setOpen((o) => (o === 2 ? Math.round(Math.random()) : 1 + Math.round(Math.random()))), 130);
    return () => window.clearInterval(id);
  }, [talking]);
  return open;
}

/** Og'iz: yopiq — tabassum chizig'i, ochiq — oval. */
function Mouth({ x, y, open, w = 12, color = '#7a2e2e' }: { x: number; y: number; open: number; w?: number; color?: string }) {
  if (open === 0) return <path d={`M${x - w / 2} ${y} Q${x} ${y + 5} ${x + w / 2} ${y}`} stroke={INK} strokeWidth={2.4} fill="none" strokeLinecap="round" />;
  const h = open === 1 ? 3.5 : 7;
  return <ellipse cx={x} cy={y + h / 2} rx={w / 2} ry={h} fill={color} stroke={INK} strokeWidth={2} />;
}

function Eyes({ y, dx = 16, r = 5, blink }: { y: number; dx?: number; r?: number; blink?: boolean }) {
  return (
    <g fill={INK}>
      {blink ? (
        <>
          <rect x={60 - dx - r} y={y - 1} width={r * 2} height={2.4} rx={1.2} />
          <rect x={60 + dx - r} y={y - 1} width={r * 2} height={2.4} rx={1.2} />
        </>
      ) : (
        <>
          <circle cx={60 - dx} cy={y} r={r} />
          <circle cx={60 + dx} cy={y} r={r} />
          <circle cx={60 - dx + 1.6} cy={y - 1.6} r={1.5} fill="#fff" />
          <circle cx={60 + dx + 1.6} cy={y - 1.6} r={1.5} fill="#fff" />
        </>
      )}
    </g>
  );
}

function useBlink(): boolean {
  const [blink, setBlink] = useState(false);
  useEffect(() => {
    let t: number;
    const loop = () => {
      t = window.setTimeout(() => {
        setBlink(true);
        window.setTimeout(() => setBlink(false), 140);
        loop();
      }, 2500 + Math.random() * 2500);
    };
    loop();
    return () => window.clearTimeout(t);
  }, []);
  return blink;
}

function Face({ name, open, blink }: { name: CharacterName; open: number; blink: boolean }) {
  switch (name) {
    case 'cat':
      return (
        <g stroke={INK} strokeWidth={2.4} strokeLinejoin="round">
          <path d="M26 44 L32 12 L52 32 Z" fill="#f4a24c" />
          <path d="M94 44 L88 12 L68 32 Z" fill="#f4a24c" />
          <path d="M33 22 L36 34 L44 30 Z" fill="#f7c9d3" stroke="none" />
          <path d="M87 22 L84 34 L76 30 Z" fill="#f7c9d3" stroke="none" />
          <ellipse cx="60" cy="66" rx="42" ry="38" fill="#f4a24c" />
          <ellipse cx="60" cy="80" rx="22" ry="15" fill="#fde7cf" stroke="none" />
          <Eyes y={60} blink={blink} />
          <path d="M56 71 L64 71 L60 76 Z" fill="#e0707f" />
          <g strokeWidth={1.6}>
            <path d="M28 72 L46 75 M28 81 L46 79 M92 72 L74 75 M92 81 L74 79" />
          </g>
          <Mouth x={60} y={82} open={open} w={11} />
        </g>
      );
    case 'owl':
      return (
        <g stroke={INK} strokeWidth={2.4} strokeLinejoin="round">
          <path d="M28 30 L36 10 L48 26 Z M92 30 L84 10 L72 26 Z" fill="#8b5e3c" />
          <ellipse cx="60" cy="64" rx="42" ry="42" fill="#8b5e3c" />
          <ellipse cx="60" cy="82" rx="26" ry="22" fill="#d9b48f" stroke="none" />
          <circle cx="42" cy="54" r="15" fill="#fff" />
          <circle cx="78" cy="54" r="15" fill="#fff" />
          <Eyes y={54} dx={18} r={6.5} blink={blink} />
          <path d={open === 0 ? 'M54 68 L66 68 L60 78 Z' : `M54 68 L66 68 L60 ${open === 1 ? 74 : 72} Z`} fill="#f2b33d" />
          {open > 0 && <path d={`M55 ${open === 1 ? 76 : 76} L65 ${open === 1 ? 76 : 76} L60 ${open === 1 ? 81 : 86} Z`} fill="#e39a1f" />}
          <path d="M44 92 Q48 88 52 92 M56 96 Q60 92 64 96 M68 92 Q72 88 76 92" fill="none" strokeWidth={1.6} />
        </g>
      );
    case 'robot':
      return (
        <g stroke={INK} strokeWidth={2.4} strokeLinejoin="round">
          <line x1="60" y1="22" x2="60" y2="8" />
          <circle cx="60" cy="8" r="5" fill={open ? '#ef4444' : '#fbbf24'} />
          <rect x="16" y="22" width="88" height="80" rx="18" fill="#9fb3c8" />
          <rect x="8" y="50" width="10" height="24" rx="4" fill="#7d93aa" />
          <rect x="102" y="50" width="10" height="24" rx="4" fill="#7d93aa" />
          <rect x="30" y="38" width="60" height="30" rx="10" fill="#1f2a3a" />
          {blink ? (
            <g fill="#5eead4" stroke="none">
              <rect x="38" y="52" width="14" height="3" rx="1.5" />
              <rect x="68" y="52" width="14" height="3" rx="1.5" />
            </g>
          ) : (
            <g fill="#5eead4" stroke="none">
              <rect x="38" y="45" width="14" height="14" rx="4" />
              <rect x="68" y="45" width="14" height="14" rx="4" />
            </g>
          )}
          <rect x="40" y={80 - open * 3} width="40" height={8 + open * 6} rx="4" fill="#1f2a3a" />
          <g stroke="#5eead4" strokeWidth={1.6}>
            <line x1="48" y1={81 - open * 3} x2="48" y2={87 + open * 3} />
            <line x1="56" y1={81 - open * 3} x2="56" y2={87 + open * 3} />
            <line x1="64" y1={81 - open * 3} x2="64" y2={87 + open * 3} />
            <line x1="72" y1={81 - open * 3} x2="72" y2={87 + open * 3} />
          </g>
        </g>
      );
    case 'fox':
      return (
        <g stroke={INK} strokeWidth={2.4} strokeLinejoin="round">
          <path d="M24 50 L28 8 L54 34 Z M96 50 L92 8 L66 34 Z" fill="#e8743b" />
          <path d="M31 20 L33 38 L45 32 Z M89 20 L87 38 L75 32 Z" fill="#2b2f3a" stroke="none" />
          <path d="M18 56 Q60 10 102 56 Q98 96 60 106 Q22 96 18 56 Z" fill="#e8743b" />
          <path d="M22 62 Q40 70 52 90 Q60 104 68 90 Q80 70 98 62 Q92 96 60 106 Q28 96 22 62 Z" fill="#fff7ee" />
          <Eyes y={60} dx={17} blink={blink} />
          <ellipse cx="60" cy="84" rx="5.5" ry="4" fill={INK} />
          <Mouth x={60} y={92} open={open} w={10} />
        </g>
      );
    case 'bear':
      return (
        <g stroke={INK} strokeWidth={2.4} strokeLinejoin="round">
          <circle cx="28" cy="30" r="14" fill="#9a6a44" />
          <circle cx="92" cy="30" r="14" fill="#9a6a44" />
          <circle cx="28" cy="30" r="7" fill="#d8b08c" stroke="none" />
          <circle cx="92" cy="30" r="7" fill="#d8b08c" stroke="none" />
          <ellipse cx="60" cy="64" rx="44" ry="40" fill="#9a6a44" />
          <ellipse cx="60" cy="80" rx="22" ry="17" fill="#d8b08c" />
          <Eyes y={56} dx={18} blink={blink} />
          <ellipse cx="60" cy="72" rx="7" ry="5" fill={INK} />
          <Mouth x={60} y={84} open={open} w={12} />
        </g>
      );
    default:
      return (
        <g stroke={INK} strokeWidth={2.4} strokeLinejoin="round">
          <ellipse cx="60" cy="62" rx="42" ry="44" fill="#26324b" />
          <path d="M26 66 Q28 34 60 40 Q92 34 94 66 Q92 98 60 102 Q28 98 26 66 Z" fill="#fff" />
          <circle cx="38" cy="74" r="7" fill="#f9a8c0" stroke="none" opacity={0.7} />
          <circle cx="82" cy="74" r="7" fill="#f9a8c0" stroke="none" opacity={0.7} />
          <Eyes y={58} dx={15} blink={blink} />
          <path d={`M49 70 Q60 64 71 70 Q60 ${open === 0 ? 76 : open === 1 ? 80 : 84} 49 70 Z`} fill="#f59e0b" />
          {open > 0 && <path d={`M52 73 Q60 ${open === 1 ? 77 : 81} 68 73`} fill="none" strokeWidth={1.6} />}
        </g>
      );
  }
}

/** Bitta qahramon. `talking` — og'iz harakati; `active` — hozir gapiruvchi (sal kattaroq, rangli halqa). */
export function Character({ name, talking = false, active = false, size = 120, label }: {
  name: CharacterName;
  talking?: boolean;
  active?: boolean;
  size?: number;
  label?: string;
}) {
  const open = useMouth(talking);
  const blink = useBlink();
  return (
    <svg
      className={`character${active ? ' active' : ''}${talking ? ' talking' : ''}`}
      viewBox="0 0 120 120"
      width={size}
      height={size}
      role="img"
      aria-label={label ?? name}
      data-character={name}
    >
      <Face name={name} open={open} blink={blink} />
    </svg>
  );
}
