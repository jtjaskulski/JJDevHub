import '@fontsource-variable/inter';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router';

import { App } from './app/App';
import { AuthProvider } from './app/auth/auth';
import { applyColorScheme, readColorScheme } from './app/theme/scheme';
import './styles.css';

applyColorScheme(readColorScheme());

const root = document.getElementById('root');
if (!root) {
  throw new Error('Root element is missing.');
}

createRoot(root).render(
  <StrictMode>
    <AuthProvider>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </AuthProvider>
  </StrictMode>,
);
