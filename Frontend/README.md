# ExplainIt CORS / HTTPS Fix

The React frontend now proxies `/api/*` to `https://localhost:7066` instead of `http://localhost:5282`, preventing the browser from following ASP.NET's HTTP->HTTPS redirect directly.

The backend `Program.cs` also enables CORS for `http://localhost:5173`.

## Apply

1. Replace the React project's `vite.config.js` with the included one.
2. Replace the backend `Program.cs` with the included one, or copy the CORS additions into your existing file.
3. Restart the ASP.NET backend.
4. Restart Vite (`npm run dev`).
5. Open `http://localhost:5173` and try login again.

If the browser reports a certificate warning for `https://localhost:7066`, open `https://localhost:7066/swagger` once in the browser and accept/trust the local development certificate if prompted, then reload the React app.
