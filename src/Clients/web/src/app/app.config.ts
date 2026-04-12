import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHighlightOptions } from 'ngx-highlightjs';
import {
  provideKeycloak,
  includeBearerTokenInterceptor,
  INCLUDE_BEARER_TOKEN_INTERCEPTOR_CONFIG,
} from 'keycloak-angular';

import { routes } from './app.routes';
import { environment } from '../environments/environment';

const keycloakEnabled = environment.keycloak.url.length > 0;

const keycloakProviders = keycloakEnabled
  ? [
      provideKeycloak({
        config: {
          url: environment.keycloak.url,
          realm: environment.keycloak.realm,
          clientId: environment.keycloak.clientId,
        },
        initOptions: {
          onLoad: 'check-sso',
        },
      }),
    ]
  : [];

const httpClientProviders = keycloakEnabled
  ? [
      provideHttpClient(withInterceptors([includeBearerTokenInterceptor])),
      {
        provide: INCLUDE_BEARER_TOKEN_INTERCEPTOR_CONFIG,
        useValue: [
          {
            urlPattern: /\/api\/v1\/content\//,
          },
        ],
      },
    ]
  : [provideHttpClient()];

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideAnimations(),
    provideHighlightOptions({
      fullLibraryLoader: () => import('highlight.js'),
    }),
    ...keycloakProviders,
    ...httpClientProviders,
  ],
};
