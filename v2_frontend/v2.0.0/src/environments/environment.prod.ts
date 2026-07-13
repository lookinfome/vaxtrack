// Production environment — the deployed frontend (Azure Static Web Apps) is on a different
// origin than the backend (Azure App Service), so relative /api paths won't resolve; every
// HTTP call needs the full backend URL prefixed instead.
//
// Actual App Service hostname — Azure's "secure unique default hostname" feature appends a
// random suffix to prevent subdomain-takeover attacks, so this isn't the clean vaxtrack-api.
// azurewebsites.net name, it's the real one copied from the App Service Overview page.
export const environment = {
  production: true,
  apiBaseUrl: 'https://vaxtrack-api-fpb3b3dyezefg4dq.southeastasia-01.azurewebsites.net'
};
