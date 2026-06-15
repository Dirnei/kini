# Roadmap

A living list of what's not yet done. Reorganize and check off as we go.

Last updated: 2026-06-15 (after `1cd8e87 CLI: pick the right signing key`).

---

## Near-term polish (small, do anytime)

- [ ] **Audit log UI** (`/app/audit`). Backend writes + `GET /v1/audit` already work; no page renders it yet. ~1 session.
- [ ] **First test.** We have zero tests across the whole repo. Start with one xUnit project against `Kini.Api` that uses Testcontainers for Mongo and exercises sign-up → verify → publish → resolve in one go. The smoke flow we've been re-running by hand should be code.
- [ ] **Transitive-dep CVE warnings.** `MongoDB.Driver 3.2.0` pulls in `SharpCompress 0.30.1` (moderate) and `Snappier 1.0.0` (high). Not exploitable in our usage but worth a `<PackageReference VersionOverride>` to nudge them up.
- [ ] **Better error UX.** Inline `<div className="text-oxblood-deep">…</div>` everywhere works but is ugly. Single toast component, surfaced from a query/mutation hook, would clean it up across `/app/*`.
- [ ] **Key expiry enforcement.** `Key.ExpiresAt` is stored on publish but the `.keys` / `.gpg` / WKD endpoints don't filter on it. One-line `Builders<Key>.Filter.And(... ExpiresAt > now OR null)` per query.
- [ ] **Vendor the web fonts.** `index.html` still links Fraunces / Mona Sans / JetBrains Mono from `fonts.googleapis.com`. Swap to `@fontsource/*` before any GDPR-sensitive launch.
- [ ] **SPA bundle split.** ~349 KB JS is one chunk. `React.lazy` per page (Landing / Sign-in / Sign-up / App) shrinks the landing-page first paint.
- [ ] **README on root.** Currently a minimal pointer. Should at least describe `docker compose up`, the three services, and how to bootstrap your first owner.

## Product surface (v1 launch path)

- [ ] **Domain claims slice + edge TLS.** `Organizations/DomainClaims/` is in the spec but no code. Production needs:
  - `POST /v1/orgs/{id}/domains` returns a DNS TXT challenge.
  - `POST /v1/orgs/{id}/domains/{domain}/verify` polls DNS and flips `status` to `verified`.
  - Request routing by `Host` header so `keys.acme.tld` serves Acme's identities, not someone else's.
  - Automated Let's Encrypt at the edge (most-likely: Caddy or `acmesharp` running ahead of Kestrel).
- [ ] **Invite-link email flow.** Today owners create members with no notification; the member only knows because someone tells them. v1: signed invite token in a URL the owner shares; member visits, signs up against the existing identity record.
- [ ] **Email verification.** Brand-new sign-ups self-verify by holding the SSH key, which is fine for the bootstrap. Member identities created by owners shouldn't be `verified` until the member proves email ownership via a one-time link.
- [ ] **OIDC sign-in.** Google Workspace, Microsoft 365, Okta. Big chunk: SAML / OIDC client setup, IdP-bound identities, JIT provisioning. Required by every enterprise buyer above ~20 seats.
- [ ] **Multi-admin roles.** Currently exactly one Owner per org. Need `admin` role for "manage members but don't transfer ownership." `Identity.Role` is already a string column — pure logic change.
- [ ] **Org ownership transfer.** Right now Owner is set at sign-up and never changes. Should be an endpoint: `POST /v1/orgs/{id}/transfer-ownership { newOwnerIdentityId }`.

## Hardware-attestation differentiator

- [ ] **YubiKey PIV attestation chain verification.** The product's pitch ("hardware-attested provenance") means we should actually verify the attestation chain when a key uploads with `provenance: binary_attested`. Today we trust whoever the request comes from. Standard Yubico attestation parsing (X.509 chain rooted at Yubico's intermediate cert) — ~150 LOC + a vendored root.
- [ ] **FIDO2 attestation verification.** Fido2NetLib accepts `AttestationConveyancePreference.None` in our current registration code; switch to `Direct` and validate the attestation when the org policy demands `binary_attested` for that credential.
- [ ] **`kini provision` (Go CLI)** — currently the CLI does `signup --generate` which makes a software key. The real provisioning command should generate the keypair **inside** the YubiKey via PIV (via `ykman` shell-out, or libykcs11). Then attestation is real.
- [ ] **Provenance policy enforcement.** "Only `binary_attested` keys may be published under this org" — an org-level setting that rejects uploads of lower-provenance keys. The provenance tiers exist; the gate doesn't.

## Production readiness / ops

- [ ] **Choose the real domain.** `kini.*` availability still gated on Christian's domain + trademark check from the original plan.
- [ ] **Deploy somewhere.** Fly.io / Hetzner / Render — anywhere with persistent volumes for Mongo. The Docker stack is the deploy artifact.
- [ ] **TLS at the API edge.** Not just the public `.keys`/WKD endpoints (which need per-customer-domain TLS via Let's Encrypt) — the management API too. Reverse proxy in front of Kestrel.
- [ ] **Secrets management.** Mongo connection string + future OIDC client secrets shouldn't live in `appsettings.json`. Sealed-secrets / Doppler / a `.env` reader, pick one.
- [ ] **CI pipeline.** GitHub Actions (or equivalent): build api + cli, run tests, build images, push on tag.
- [ ] **Reproducible Go binary builds.** Cross-compile `kini-cli` for darwin-{amd64,arm64}, linux-{amd64,arm64}, windows-amd64. Sign macOS builds. Publish on GitHub Releases.
- [ ] **Observability.** Structured logs, OpenTelemetry traces, a `/metrics` endpoint. Nothing here is on fire but anyone running this in prod will want it on day 1.
- [ ] **Rate limiting.** Sign-in challenge endpoint is currently unauth + unlimited. ASP.NET Core's `RateLimiter` middleware, IP-bucketed.
- [ ] **Backup / restore for Mongo.** `mongodump`/`mongorestore` in a cron container; or hand it off to whatever managed Mongo we end up on.

## Open-source / community

- [ ] **License.** README + `Kini.Api.csproj` say `TBD`. Pick AGPL-3.0 (matches step-ca / Hashicorp Vault) and commit.
- [ ] **CONTRIBUTING.md.** Repo structure overview, how to run locally, conventional-commit style if we want one.
- [ ] **CLAUDE.md.** Project-level conventions for future Claude sessions — vertical-slice + Microsoft-style namespaces, no DDD jargon, no commit-on-my-behalf, etc. Reflects what's already in private memory but should be in the repo so contributors see it.
- [ ] **One-pager refresh.** `docs/one-pager.md` was written pre-implementation. Reality is now substantially different (multi-user, audit, API tokens, GPG, CLI). Bring it up to date or split into a separate "current state" doc.
- [ ] **Customize OpenAPI docs render.** The `/docs/` Redoc viewers serve the raw spec. A small custom theme + intro page would make the docs feel like part of the product.

## Smaller follow-ups noted along the way

- [ ] Webhook payload schemas (`key.created`, `key.revoked`, `key.expiring`) — declared in the one-pager, not in the spec or code yet.
- [ ] Self-service org domain disambiguation. The WKD direct-method walks all identities computing hashes per request; index by `WkdHash` or pre-compute when there are >1k identities.
- [ ] GPG key lookup by short ID / long ID, not just by username. Useful for `gpg --recv-keys` interop.
- [ ] CLI: `kini config` subcommand to view/edit the persisted config without a text editor.
- [ ] CLI: `kini publish --gpg` flag (currently the CLI only does SSH).
- [ ] CLI: shell-completion install instructions (`kini completion bash` etc.) — already works via cobra, just needs README mention.

---

## How to use this file

- When picking up work, scan top-to-bottom and pick something whose scope matches your appetite for the session.
- When closing an item, check the box (or delete it) **in the same commit** as the work; the file should be a true state-of-the-world.
- When discovering new work, add it to the right section rather than letting it live only in memory or chat scrollback.
