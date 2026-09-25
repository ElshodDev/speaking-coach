import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './App';
import './styles.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

// Service worker faqat production build'da (Vercel): lokal `npm run dev`da
// keshlash kod o'zgarishlarini yashirib qo'yib, chalkashtirib yuborardi.
if (import.meta.env.PROD && 'serviceWorker' in navigator) {
  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/sw.js').catch(() => {
      // Ro'yxatdan o'tmasa ham ilova oddiy sayt sifatida ishlayveradi.
    });
  });
}
