import '@fontsource-variable/inter';
import { cssCustomProperties } from '@jjdevhub/theme';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router';

import { App } from './app/App';
import { AuthProvider } from './app/auth/auth';
import './styles.scss';

for (const [name, value] of Object.entries(cssCustomProperties())) {
  document.documentElement.style.setProperty(name, value);
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </AuthProvider>
  </StrictMode>,
);
