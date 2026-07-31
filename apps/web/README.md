# ServicePilot Web

Next.js 16 App Router frontend and thin BFF for the ServicePilot MVP.

## Development

The API must be available at `http://localhost:5267`.

```powershell
Copy-Item .env.example .env.local
npm ci
npm run dev
```

Open `http://localhost:3000`. The demo tenant credentials are documented in the
repository root README.

## Quality checks

```powershell
npm run format:check
npm run lint
npm run typecheck
npm test
npm run build
```

When the API is running, refresh the checked-in OpenAPI types with:

```powershell
npm run api:generate
```

## Security boundary

The browser stores only a `Secure`/`HttpOnly`/`SameSite=Lax` session cookie.
Server Components and Server Actions call the ASP.NET Core API from the Next.js
server. The API remains the final authentication, authorization and tenant
isolation boundary for every read and mutation.
