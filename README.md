# Kini

A hardware-friendly key directory for organizations. Publishes team GPG and SSH public keys at standards-compliant endpoints (WKD + GitHub-style `/{user}.keys`), with provenance tiers, lifecycle, and audit. OpenAPI-first.

> Project name is internal-only for now. Customer-facing surface and the spec files use a `<service>` placeholder pending domain and trademark verification — see [`docs/one-pager.md`](docs/one-pager.md).

## Repository layout

```
.
├── README.md
├── docker-compose.yml
├── docker/                          # Dockerfiles and shared config
│   ├── docs.Dockerfile              # nginx image serving the OpenAPI viewers
│   ├── api.Dockerfile               # multi-stage build: SPA → API → runtime
│   └── nginx.conf
├── docs/                            # product docs + Redoc-based API viewers
│   ├── one-pager.md
│   ├── index.html
│   ├── api.html
│   └── well-known.html
├── spec/                            # OpenAPI 3.1 (source of truth)
│   ├── api.openapi.yaml             # authenticated management API
│   └── well-known.openapi.yaml      # public WKD + .keys + .gpg endpoints
└── src/
    ├── Kini.Api/                    # .NET 10 / Akka.NET / MongoDB
    │   ├── Kini.Api.csproj
    │   ├── Program.cs
    │   ├── nswag.json               # NSwag codegen config (spec → C# DTOs)
    │   ├── Organizations/           # vertical slice — record, actor, Mongo wrapper, endpoints
    │   ├── Mongo/                   # client + BSON conventions
    │   └── Generated/               # generated DTOs (gitignored)
    └── Kini.Web/                    # Vite + React 19 + Tailwind 4 + shadcn
        ├── package.json
        ├── vite.config.ts
        └── src/
            ├── App.tsx
            ├── main.tsx
            ├── index.css
            └── lib/                 # api client + generated TS types
```

The OpenAPI specs in `spec/` are the source of truth. `Kini.Api` regenerates C# DTOs via NSwag on build; `Kini.Web` regenerates TS types via `openapi-typescript` (`npm run gen:api`).

## Local development

### Prerequisites
- .NET 10 SDK
- Node.js 22+
- MongoDB (or `docker compose up -d mongo`)

### Run the API
```sh
cd src/Kini.Api
dotnet run                    # http://localhost:5000
```

`appsettings.Development.json` points at `mongodb://localhost:27017` and database `kini-dev`. Override with environment variables (`Mongo__ConnectionString=...`) or `appsettings.Local.json`.

### Run the SPA
```sh
cd src/Kini.Web
npm install
npm run gen:api               # generate TS types from the spec
npm run dev                   # http://localhost:5173, proxies /v1, /.well-known, /healthz to :5000
```

### Build the SPA into Kestrel's wwwroot
```sh
cd src/Kini.Web
npm run build                 # writes to ../Kini.Api/wwwroot
cd ../Kini.Api
dotnet run                    # serves both API and the built SPA
```

## Docker stack

```sh
docker compose up -d          # all three services
docker compose down           # stop
docker compose down -v        # stop + wipe mongo volume
```

| Service | Image      | Port (host) | Notes                                                |
|---------|------------|-------------|------------------------------------------------------|
| docs    | nginx      | `:8080`     | OpenAPI viewers, http://localhost:8080 → /docs/      |
| api     | dotnet 10  | `:5001`     | Kini.Api + bundled SPA at http://localhost:5001 (macOS reserves :5000 for AirPlay) |
| mongo   | mongo:8    | `:27017`    | Persisted in the `mongo-data` volume                 |

The `api` image multi-stages a Node + .NET build into a single Alpine runtime. NSwag runs as part of the .NET build (using the `$(NSwagExe_Net100)` variant that matches the SDK image's runtime), so `Generated/` is always in sync with `spec/api.openapi.yaml`.

## OpenAPI codegen

| Surface  | Tool                 | Trigger                                                            |
|----------|----------------------|---------------------------------------------------------------------|
| C# DTOs  | NSwag (NSwag.MSBuild)| `dotnet build` in `src/Kini.Api/` (runs automatically before Compile) |
| TS types | openapi-typescript   | `npm run gen:api` in `src/Kini.Web/`                                |

Both read `spec/api.openapi.yaml`. If they drift, the contract is the source of truth — regenerate.

## Status

Active alpha. End-to-end auth flow (SSH-key + WebAuthn), key publishing (SSH + GPG via WKD), multi-user orgs with admin-uploads-for-members, audit log, API tokens, and a Go CLI all work. See [`docs/roadmap.md`](docs/roadmap.md) for what's still open.

## License

[AGPL-3.0-or-later](LICENSE). See [`CONTRIBUTING.md`](CONTRIBUTING.md) for how to get a local stack running and how the repo is laid out.
