// Ilova logotipi: ikki suhbat pufakchasi — o'quvchi (oq) va AI murabbiy (sariq, "yozmoqda…").
// Manba: public/icons/logo.svg (PNG ikonkalar ham shundan chizilgan).

/** Logoning shakllari — ShareCard canvas'da ham shu yo'llarni chizadi. */
export const LOGO = {
  bg: '#1d4ed8',
  back: 'M26 18h30a12 12 0 0 1 12 12v12a12 12 0 0 1-12 12H34l-12 9 2-9a12 12 0 0 1-10-12V30a12 12 0 0 1 12-12z',
  front: 'M50 44h24a12 12 0 0 1 12 12v10a12 12 0 0 1-12 12h-1l2 9-12-9H50a12 12 0 0 1-12-12V56a12 12 0 0 1 12-12z',
  accent: '#fbbf24',
  dots: '#1e3a8a',
} as const;

export function Logo({ size = 30, title }: { size?: number; title?: string }) {
  return (
    <svg
      viewBox="0 0 100 100"
      width={size}
      height={size}
      className="logo"
      role={title ? 'img' : undefined}
      aria-label={title}
      aria-hidden={title ? undefined : true}
    >
      <rect width="100" height="100" rx="24" fill={LOGO.bg} />
      <path d={LOGO.back} fill="#ffffff" fillOpacity="0.92" />
      <path d={LOGO.front} fill={LOGO.accent} stroke={LOGO.bg} strokeWidth="4" strokeLinejoin="round" />
      <g fill={LOGO.dots}>
        <circle cx="52" cy="61" r="3.5" />
        <circle cx="62" cy="61" r="3.5" />
        <circle cx="72" cy="61" r="3.5" />
      </g>
    </svg>
  );
}
