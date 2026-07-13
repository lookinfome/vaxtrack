// Development environment — apiBaseUrl is empty so all HTTP calls stay relative (e.g. /api/...),
// which `ng serve`'s dev proxy (proxy.conf.json) forwards to the local backend on :5119.
export const environment = {
  production: false,
  apiBaseUrl: ''
};
