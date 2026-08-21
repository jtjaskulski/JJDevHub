export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:8081',
  /** Pełny prefix Content API (kanoniczna ścieżka za Nginx). */
  contentApiUrl: 'http://localhost:8081/api/v1/content',
  /**
   * OIDC — Keycloak na porcie hosta 8083 (compose).
   * Seed: owner@test.com / Owner123! (rola Owner), student@test.com / Student123!.
   */
  keycloak: {
    url: 'http://localhost:8083',
    realm: 'jjdevhub',
    clientId: 'jjdevhub-web',
  },
  /**
   * Gdy Keycloak jest skonfigurowany, admin wymaga roli Owner.
   * true tylko gdy url Keycloak jest pusty (tryb publiczny bez IdP).
   */
  allowAdminWithoutAuth: false,
};
