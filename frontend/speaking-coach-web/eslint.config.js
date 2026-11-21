// Frontend lint (src/). Plaginlar `lint/` papkasidan olinadi: typescript-eslint hali TypeScript 7'ni
// qo'llamaydi, shuning uchun `lint/` da TS 6 API o'rnatiladi (asosiy build — tsc 7 — o'zgarmagan).
// Ishga tushirish: `npm run lint` (avval `npm run lint:install`).
import { createRequire } from 'node:module';
import { pathToFileURL } from 'node:url';

const req = createRequire(new URL('./lint/package.json', import.meta.url));
const load = async (name) => {
  const mod = await import(pathToFileURL(req.resolve(name)).href);
  return mod.default ?? mod;
};

const js = await load('@eslint/js');
const reactHooks = await load('eslint-plugin-react-hooks');
const globals = await load('globals');
const tseslint = await load('typescript-eslint');

export default tseslint.config(
  { ignores: ['dist/**', 'lint/**', 'node_modules/**', 'scripts/**', '*.config.js', 'vite.config.ts'] },
  {
    files: ['src/**/*.{ts,tsx}'],
    extends: [js.configs.recommended, ...tseslint.configs.recommended],
    plugins: { 'react-hooks': reactHooks },
    languageOptions: { ecmaVersion: 2022, globals: { ...globals.browser, ...globals.es2021 } },
    rules: {
      ...reactHooks.configs.recommended.rules,
      // Hook'larning asosiy qoidalari — XATO (masalan, shartli hook ilovani yiqitadi).
      'react-hooks/rules-of-hooks': 'error',
      // Bo'sh joy belgisi: "_" bilan boshlangan nomlar ataylab ishlatilmaydi.
      '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_', varsIgnorePattern: '^_', caughtErrorsIgnorePattern: '^_', ignoreRestSiblings: true }],
      // React 19 "compiler" qoidalari yangi va qat'iy: mavjud ishlaydigan kodni faqat shu sababli qayta yozmaymiz —
      // ogohlantirish sifatida ko'rinib turadi, yangi kodda tuzatiladi.
      'react-hooks/refs': 'warn',
      'react-hooks/set-state-in-effect': 'warn',
      'react-hooks/purity': 'warn',
      'react-hooks/use-memo': 'warn',
      'react-hooks/exhaustive-deps': 'warn',
      'react-hooks/immutability': 'warn',
      'react-hooks/static-components': 'warn',
      'react-hooks/preserve-manual-memoization': 'warn',
      'react-hooks/globals': 'warn',
      'react-hooks/error-boundaries': 'warn',
      'react-hooks/component-hook-factories': 'warn',
      'react-hooks/gating': 'warn',
      'react-hooks/config': 'warn',
      'react-hooks/unsupported-syntax': 'warn',
      'react-hooks/incompatible-library': 'warn',
    },
  },
);
