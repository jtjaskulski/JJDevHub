export const environment = {
  production: true,
  apiBaseUrl: '',
  contentApiUrl: '/api/v1/content',
  /**
   * Browser-side Keycloak URL (host port mapped from compose).
   * Production behind a public domain: replace with the public issuer base
   * (e.g. https://jjdevhub.pl/auth) and matching redirect URIs in the realm.
   */
  keycloak: {
    url: 'http://localhost:8083',
    realm: 'jjdevhub',
    clientId: 'jjdevhub-web',
  },
  allowAdminWithoutAuth: false,
};
