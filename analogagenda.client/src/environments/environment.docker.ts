/**
 * Environment for Docker Compose deployment (e.g. Ubuntu server).
 * Use relative apiUrl when behind a reverse proxy that serves frontend and backend on the same origin.
 * Otherwise set apiUrl to the backend base URL (e.g. http://<host>:<backend-port>) at build time.
 */
export const environment = {
  production: true,
  apiUrl: '',
  functionsUrl: ''
};
