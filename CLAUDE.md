# CLAUDE.md

Conventions for Claude (and Claude-like assistants) working in this repo.
These mirror what an experienced human contributor would learn from
[`CONTRIBUTING.md`](CONTRIBUTING.md); this file just makes them visible to
AI agents that wouldn't otherwise read it.

## Stack — defaults for greenfield work in this repo

- **Backend:** C# .NET 10, Akka.NET 1.5.x, Akka.Streams, MongoDB.Driver 3.x.
- **Database:** MongoDB **8+** (the local stack pins `mongo:8`).
- **Frontend:** Vite + React 19 + Tailwind 4 + shadcn/ui + React Router v7
  + TanStack Query + `openapi-fetch` over `openapi-typescript`-generated types.
- **CLI:** Go 1.20+, `spf13/cobra`, stdlib HTTP, shell out to `ssh-keygen`
  for crypto operations (so hardware tokens work via the agent).
- **OpenAPI:** 3.1, hand-written, in `spec/`. NSwag regenerates C# DTOs
  (`Kini.Api.Generated`) on every `dotnet build`; `openapi-typescript`
  regenerates the SPA's types on `npm run gen:api`.

## Architecture rules

- **Vertical slice.** Each capability owns its records, collection wrapper,
  actor (if any), handlers, and endpoint mapper inside one folder.
- **Microsoft-style namespaces.** `Kini.Api.Authentication.Identities`, not
  `Kini.Api.Domain.User.Aggregate`. Layered Clean-Architecture / DDD
  buzzwords (`Domain`, `Infrastructure`, `Application`, `Features`) do not
  appear in this codebase.
- **Type-prefix > folder nesting.** `Credentials/SshCredential.cs` and
  `Credentials/WebAuthnCredential.cs` live side-by-side in the same folder.
  Don't create `Credentials/Ssh/` and `Credentials/WebAuthn/` subfolders;
  the prefix on the type already does that work.
- **Don't introduce shared layers prematurely.** No `Services/`,
  `Repositories/`, or `Helpers/` cross-cutting folders. Cross-cutting code
  earns its own folder only when it becomes a capability of its own
  (`Mongo/`, `Authentication/`, `Audit/`).

## Don'ts (hard rules)

- **Never `git commit` on the user's behalf** unless explicitly asked,
  and even then ask once to confirm.
- **Never add `Co-Authored-By: Claude …`** footers. Ever. Commits are the
  human's.
- **Never make persistent test data in the live dev database.** Smoke
  tests that hit real endpoints must clean up in the same flow they
  created data. The user has real records (their YubiKey-backed identity,
  their published key) in the same Mongo instance; orphaned test
  signups crowd that out.
- **Never split cultural flavor across the codebase.** "Kini" is Bavarian
  for "the king" — that's the cultural fingerprint. Don't sprinkle
  Bavarian dialect (Servus / Pfiat di / Oktoberfest cues) through product
  copy. The product reads as a serious international devtool.

## Visual language

Editorial-heraldic. Parchment + ink + oxblood + antique gold palette
(see `src/Kini.Web/src/index.css`'s `@theme` block). Fraunces (display,
variable, italic for emphasis lines), Mona Sans (body), JetBrains Mono
(technical labels & code). The wax-seal logomark in
`src/Kini.Web/src/components/Seal.tsx` is the brand anchor — extend it
as the visual primitive rather than replacing it.

When designing new surfaces: commit hard to one distinctive aesthetic
direction. Defaults that read "Tailwind-starter generic" undersell the
product. Big serifs, generous space, asymmetric grids are welcome;
purple-on-white gradients are not.

## Workflow expectations

- Use `TaskCreate` for any session that will touch 3+ files or run for
  multiple build/verify cycles. Mark tasks `completed` as soon as they
  ship, not in batches.
- Before claiming a feature works, *run it* against the live stack and
  capture the response. The "verify before commit" rule from
  [`feedback-never-commit`](#) in the project's private memory applies
  to AI sessions too.
- When you discover new work mid-stream, add it to
  [`docs/roadmap.md`](docs/roadmap.md) rather than leaving it only in
  chat scrollback.

## Where things live

- `spec/` — OpenAPI source of truth. Edit here first, then code.
- `src/Kini.Api/` — backend slices under `Kini.Api.*` namespaces.
- `src/Kini.Web/` — frontend pages under `src/pages/`, components in
  `src/components/`, shared lib code in `src/lib/`.
- `src/kini-cli/` — Go CLI, cobra subcommands under `cmd/`, plumbing
  under `internal/`.
- `docs/` — product brief (`one-pager.md`), roadmap, OpenAPI viewers.
- `docker/` — Dockerfiles and nginx config for the docs container.

## When in doubt

Match the prevailing style. The codebase already shows preferences for
named-tuple returns, `record` over `class`, file-scoped namespaces,
`required init` properties, and one-handler-per-file. If you can't tell
which pattern wins, ask the user before introducing a third.
