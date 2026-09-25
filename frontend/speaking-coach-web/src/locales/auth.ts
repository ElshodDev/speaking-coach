import { defineMessages } from '../i18n';

/** AuthPanel.tsx — kirish / roʻyxatdan oʻtish formasi va hisob kartasi. */
export const authMsg = defineMessages(
  {
    account: 'Hisob',
    logout: 'Chiqish',
    modeGroup: 'Kirish turi',
    register: 'Roʻyxatdan oʻtish',
    login: 'Kirish',
    registerHint: 'Hisob ochsangiz, natijalaringiz saqlanadi va xatolaringiz takrorlash kartalariga aylanadi.',
    loginHint: 'Email va parolingiz bilan kiring.',
    email: 'Email',
    password: 'Parol',
    newPassword: 'Parol (kamida 8 ta belgi)',
    wait: 'Kuting...',
    createAccount: 'Hisob ochish',
  },
  {
    ru: {
      account: 'Аккаунт',
      logout: 'Выйти',
      modeGroup: 'Способ входа',
      register: 'Регистрация',
      login: 'Вход',
      registerHint: 'С аккаунтом ваши результаты сохраняются, а ошибки превращаются в карточки для повторения.',
      loginHint: 'Войдите с помощью email и пароля.',
      email: 'Email',
      password: 'Пароль',
      newPassword: 'Пароль (не менее 8 символов)',
      wait: 'Подождите...',
      createAccount: 'Создать аккаунт',
    },
    en: {
      account: 'Account',
      logout: 'Log out',
      modeGroup: 'Sign-in method',
      register: 'Sign up',
      login: 'Log in',
      registerHint: 'With an account, your results are saved and your mistakes become review cards.',
      loginHint: 'Log in with your email and password.',
      email: 'Email',
      password: 'Password',
      newPassword: 'Password (at least 8 characters)',
      wait: 'Please wait...',
      createAccount: 'Create account',
    },
  },
);
