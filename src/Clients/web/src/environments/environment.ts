export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:8081',
  /** Pełny prefix Content API (kanoniczna ścieżka za Nginx). */
  contentApiUrl: 'http://localhost:8081/api/v1/content',
  /**
   * Gdy ustawione, aplikacja inicjuje Keycloak (OIDC). Puste = brak logowania, tryb publiczny.
   * Przykład dev: url `http://localhost:8083`, realm `jjdevhub`, clientId `jjdevhub-web`.
   */
  keycloak: {
    url: '',
    realm: '',
    clientId: '',
  },
  /**
   * Dev: pozwala wejść na route admin bez Keycloak gdy keycloak.url jest pusty.
   * W produkcji ustaw false (build prod).
   */
  allowAdminWithoutAuth: true,
};
