# `<service>` — one-pager

**Tagline:** A hardware-friendly key directory for organizations. Publish your team's GPG and SSH public keys at standards-compliant endpoints (WKD + `.keys`), with provenance, lifecycle, and an OpenAPI-first surface anyone can build on.

## Problem
Teams using YubiKeys (or just GPG/SSH at all) have nowhere good to publish keys. GitHub leaks the identity into a vendor; self-hosting Forgejo + nginx + WKD scripts works but every company reinvents it. There's no org model, no lifecycle, no audit, no provenance signal.

## Solution
A multi-tenant directory that:
- Serves **WKD** for GPG and **`.keys`/`.gpg`** for SSH/GPG over each customer's own domain (CNAME + automated TLS).
- Models **org → identity → key** with full lifecycle (rotation, expiry, revocation).
- Tags every key with a **provenance tier** so policies can require stronger origins (`uploaded` → `binary` → `binary_hardware` → `binary_attested`).
- Is **OpenAPI-first**: the spec is the source of truth; server, clients (TS/Go/Python), CLI, and docs are generated from it. External flows (Ansible, CI, custom dashboards) are first-class consumers.

## Core concepts
| Entity | Notes |
|---|---|
| `Organization` | Owns one or more verified domains, SSO config, policy. |
| `DomainClaim` | DNS TXT challenge → verified domain. |
| `Identity` | Email-scoped user, either org-bound or personal. |
| `Key` | GPG or SSH pubkey + fingerprint, algorithm, expires, revoked, **provenance**. |
| `Provenance` | `uploaded` / `binary` / `binary_hardware` / `binary_attested`. |
| `AuditEvent` | Append-only log of every publish/revoke/fetch-of-revoked. |

## API surface (sketch — full spec is the source of truth)

**Authenticated (org admin / self-service):**
- `POST /v1/orgs` · `POST /v1/orgs/{id}/domains` · `POST .../verify`
- `POST /v1/identities/{email}/keys` · `DELETE /v1/keys/{id}` · `POST /v1/keys/{id}/revoke`
- `GET /v1/identities/{email}/keys` · `GET /v1/audit`
- `POST /v1/api-tokens` (for CLI / CI)

**Public well-known (no auth, served per customer domain):**
- `GET /.well-known/openpgpkey/hu/{zbase32}` — WKD direct
- `GET /.well-known/openpgpkey/{domain}/hu/{zbase32}` — WKD advanced
- `GET /{user}.keys` — SSH pubkeys (GitHub-compatible URL shape, so Ansible's `authorized_key` consumes it natively)
- `GET /{user}.gpg` — armored GPG key

**Webhooks:** key.created, key.revoked, key.expiring — so downstream systems subscribe instead of polling.

## OpenAPI-first principle
- `openapi.yaml` is checked in, versioned, and reviewed like code.
- CI fails on breaking changes without an explicit version bump.
- Generated artifacts: Go server stubs, TS client (npm), Go client, Python client, CLI flags, Redoc/Stoplight docs site.
- Third-party integrations (Ansible collection, Terraform provider, GitHub Action) live in separate repos and consume the published clients — no privileged access to the backend code.

## Trust model
- **Domain claim** = DNS TXT. Mandatory for orgs.
- **Identity** within an org = SSO/OIDC (Google Workspace, M365, Okta) or admin invite. Personal accounts = OAuth (GitHub/Google) on verified email.
- **Provenance** is a published attribute, not a gate. Policies (later) consume it: "prod servers only accept `binary_attested`."

## Tech stack
- **Backend:** C# on .NET 10
- **Concurrency / domain model:** Akka.NET (actor per org / per identity is a natural fit; sharded cluster later for scale)
- **Pipelines:** Akka.Streams for ingest, fan-out, webhook delivery, audit log
- **Storage:** MongoDB (keys, orgs, audit, claims — document model fits the polymorphic key shapes well)
- **API:** OpenAPI 3.1 → server stubs generated into the .NET project; clients (TS/Go/Python) generated from the same spec
- **TLS / custom domains:** ACME (Let's Encrypt) for per-tenant CNAME'd domains, terminated at the edge

## v1 cut (ship this)
- Org signup via OAuth, single admin, manual member invite
- Domain claim via DNS TXT, CNAME-based custom domain with automated Let's Encrypt
- Key upload via web UI or API
- WKD direct method + `/{user}.keys` + `/{user}.gpg`
- GPG revocation cert publishing; SSH revocation = removal
- OpenAPI spec + generated TS & Go clients
- Open-source provisioning CLI (Go) — generates keypair, writes to YubiKey, uploads pubkeys via API token

## v2+
- SSO/OIDC for orgs (SAML later)
- YubiKey PIV attestation chain verification → `binary_attested` tier
- SSH CA mode (issue short-lived certs; servers trust one CA pubkey)
- Trust policies / enforcement
- Self-hosted edition (open core)
- Terraform provider, Ansible collection, GitHub Action

## Open questions
- Name + domain
- Pricing (likely per-user/month with generous free tier for individuals + open-source projects)
- Self-hosted licensing model (AGPL core + commercial SaaS? source-available?)
- Whether to lean into SSH CA in v2 or hold for v3 — it's a different product shape
