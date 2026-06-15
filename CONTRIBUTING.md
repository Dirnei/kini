# Contributing to Kini

Welcome. This doc covers how to get a working local stack, how the repo is
laid out, and the conventions we follow. For *what's left to build*, see
[`docs/roadmap.md`](docs/roadmap.md).

## Local development

```sh
docker compose up -d            # docs (:8080) + api (:5001) + mongo (:27017)
open http://localhost:5001/     # the SPA
open http://localhost:8080/     # the OpenAPI docs (Redoc)
```

That's the whole stack. The api container builds the SPA into Kestrel's
`wwwroot/`, so a single `docker compose build api` cycles both halves.

To iterate on individual pieces without rebuilding the image each time:

```sh
# .NET API (with hot reload):
cd src/Kini.Api && dotnet watch
# Hits http://localhost:5000 by default; point the SPA's Vite proxy there.

# SPA:
cd src/Kini.Web && npm install && npm run dev
# Vite serves http://localhost:5173 with HMR; proxies /v1/* and /.well-known/*
# to whatever's listening on :5000 or :5001.

# CLI:
cd src/kini-cli && make build && ./kini --help
```

Requirements (latest minor of each is fine):

- Docker + Docker Compose v2
- .NET 10 SDK (if iterating on the API outside Docker)
- Node.js 22+ (if iterating on the SPA outside Docker)
- Go 1.20+ (if iterating on the CLI)
- `ssh-keygen` and `gpg` on `PATH` for the runtime; both are already in
  the api Docker image.

## Repo layout

```
.
├── docs/             product brief, API doc viewers, roadmap
├── spec/             OpenAPI 3.1 contracts — source of truth
│   ├── api.openapi.yaml          authenticated management API
│   └── well-known.openapi.yaml   public WKD + .keys + .gpg endpoints
├── docker/           Dockerfiles + nginx.conf for the docs container
└── src/
    ├── Kini.Api/     .NET 10 / Akka.NET / MongoDB backend
    ├── Kini.Web/     Vite + React + Tailwind frontend
    └── kini-cli/     Go CLI client
```

The OpenAPI specs in `spec/` are the source of truth. C# DTOs (NSwag) and TS
types (`openapi-typescript`) regenerate on every build; if they drift the
build fails.

## Architecture conventions

### Vertical slice, Microsoft-style namespaces

Each capability owns its own folder + namespace. Inside a slice you'll find
the entity record, its Mongo collection wrapper, its actor (if applicable),
its endpoint handlers, and its endpoint-mapping extension method — all
side-by-side.

```
src/Kini.Api/Authentication/
├── Identities/                # Kini.Api.Authentication.Identities
│   ├── Identity.cs
│   ├── IdentityActor.cs
│   ├── IdentitiesCollection.cs
│   ├── CreateIdentity.cs
│   └── IdentitiesEndpoints.cs
├── Credentials/               # SshCredential + WebAuthnCredential side by side
├── Challenges/
├── Sessions/
├── SignIn/
└── Registration/
```

What we explicitly **don't** do:

- No `Domain/`, `Infrastructure/`, `Application/`, `Features/` layer folders.
- No `Repositories/` or `Services/` cross-cutting folders — each slice owns
  its own data access.
- No type names that duplicate folder paths (`Credentials/SshCredential.cs`,
  not `Credentials/Ssh/SshCredential.cs` — the prefix is on the type).

This mirrors what `Microsoft.AspNetCore.*` does. If you're tempted to add
a "shared" or "common" folder, please first see whether the thing actually
needs to be shared, or whether it belongs inside one slice.

### Adding a new vertical slice

1. Update the OpenAPI spec in `spec/api.openapi.yaml` first — schemas + paths.
2. Create the slice folder under `src/Kini.Api/<Area>/<Slice>/`.
3. Write `EntityName.cs`, `EntityNameCollection.cs` (with `EnsureIndexes`),
   an actor (if writes need ordering / cross-entity coordination), one
   handler file per endpoint, and `<Slice>Endpoints.cs` (the
   `MapXxxEndpoints` extension method).
4. Register the collection in `Program.cs`'s DI section. Call
   `EnsureIndexes` in the index-setup block. Call your mapper alongside
   `app.MapOrganizationsEndpoints()` etc.
5. Wire audit log writes where useful (`AuditLog.Record(http, ...)`).
6. Add a smoke-test cycle to your dev loop — sign-up, do the new flow,
   tear down — and commit only when it works end-to-end.

### Frontend pages

Pages under `/app/*` nest under `AppShell` (auth gate + shared header).
Add a new page:

1. `src/Kini.Web/src/pages/Something.tsx` — read `me` via
   `useOutletContext<AppOutletContext>()`.
2. Add `<Route path="something" element={<Something />} />` inside the
   `AppShell` route in `App.tsx`.
3. Add the nav entry to `NAV_ITEMS` in `components/AppHeader.tsx`.

Visual language: editorial-heraldic. Fraunces (display, italic for emphasis)
+ Mona Sans (body) + JetBrains Mono (technical). Parchment / ink / oxblood /
antique-gold palette via the CSS variables in `index.css`. Keep cultural
references — Bavarian heritage, "Kini" = "the king" — in the *name only*;
product copy stays neutral and international.

## Commit style

- Imperative mood subject line, 50 chars-ish.
- Body wrapped at ~72, explaining *why* and any non-obvious tradeoffs.
- One logical change per commit. Mechanical refactors and feature changes
  go in separate commits.
- No `Co-Authored-By: Claude …` footer when an AI assistant helped.
  Commits are yours.

## Tests

We're early. There are none yet. When you add the first one, please use
xUnit for .NET and Testcontainers to spin up Mongo so tests are
self-contained. The first test should walk the end-to-end sign-up →
publish → resolve flow, exercising the same path we currently verify
by hand. See `docs/roadmap.md` for the slot we've reserved.

## License

[AGPL-3.0-or-later](LICENSE). Contributions are accepted under the same
terms.
