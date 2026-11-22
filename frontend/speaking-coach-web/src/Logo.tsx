// Ilova logotipi: ochiq kitob — chap sahifada terminal (IT), o'ng sahifada suhbat pufagi (gapirish),
// atrofida bug'doy va paxta novdalari. Manba: public/icons/logo.svg (katta) va logo-mini.svg
// (≤ 48 px: novdasiz, qalinroq chiziqlar — kichikda o'qilishi uchun). PNG ikonkalar ham shulardan.

/** Kichik o'lchamda soddalashtirilgan versiya ishlatiladi. */
export const MINI_MAX = 48;

/** Berilgan o'lcham uchun logotip fayli. */
export function logoSrc(size: number): string {
  return size <= MINI_MAX ? '/icons/logo-mini.svg' : '/icons/logo.svg';
}

export function Logo({ size = 30, title }: { size?: number; title?: string }) {
  const src = logoSrc(size);
  return (
    <img
      src={src}
      width={size}
      height={size}
      className="logo"
      alt={title ?? ''}
      aria-hidden={title ? undefined : true}
      decoding="async"
    />
  );
}
