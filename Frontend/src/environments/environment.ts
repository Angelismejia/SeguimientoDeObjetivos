// El dev server de Angular (ng serve) proxea /api y /hubs hacia el backend
// (ver proxy.conf.json), así que el navegador solo habla con su propio origen.
// Esto evita problemas de CORS/cookies entre orígenes distintos, tanto en LAN
// como a través de un túnel de VS Code (devtunnels.ms).
export const environment = {
  production: false,
  apiUrl: '/api',
  hubUrl: '/hubs/chat'
};
