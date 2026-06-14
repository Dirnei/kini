import { useState } from 'react'
import { useOutletContext } from 'react-router'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { startRegistration } from '@simplewebauthn/browser'
import { Seal } from '../components/Seal'
import { getToken } from '../lib/api'
import { authenticatorName } from '../lib/authenticators'
import type { AppOutletContext } from '../components/AppShell'

type WebAuthnRow = {
  id: string
  identityId: string
  credentialId: string
  algorithm: number
  aaguid: string | null
  nickname: string | null
  signCount: number
  createdAt: string
  lastUsedAt: string | null
  revokedAt: string | null
}

export function Dashboard() {
  const { me } = useOutletContext<AppOutletContext>()

  return (
    <main className="flex-1 max-w-[1400px] w-full mx-auto px-6 md:px-10 py-12">
      <p className="eyebrow reveal">Console · {me.organization.name}</p>
      <h1 className="reveal font-display text-5xl md:text-6xl mt-3 leading-[1.02]" style={{ fontVariationSettings: '"opsz" 96', animationDelay: '80ms' }}>
        Welcome back, <span className="italic text-[var(--color-oxblood)]">{me.identity.displayName ?? me.identity.username}</span>.
      </h1>
      <p className="reveal mt-4 text-lg text-[var(--color-ink-muted)] max-w-2xl" style={{ animationDelay: '160ms' }}>
        Your organization's directory at a glance. This is a scaffold — the real surface for
        identities, keys, domain claims, tokens, and audit will grow into it.
      </p>

      <HardwareSection />

      <div className="reveal mt-16 grid grid-cols-1 md:grid-cols-4 gap-px bg-[var(--color-rule)]" style={{ animationDelay: '260ms' }}>
        {[
          { label: 'Identities', value: '1', sub: 'just you, for now' },
          { label: 'Keys', value: '—', sub: 'see Keys tab' },
          { label: 'Domains', value: me.organization.primaryDomain ? '1' : '0', sub: me.organization.primaryDomain ?? 'unclaimed' },
          { label: 'Sessions', value: '1', sub: 'this browser' },
        ].map((m) => (
          <article key={m.label} className="bg-[var(--color-parchment)] p-7">
            <p className="eyebrow">{m.label}</p>
            <p className="font-display text-5xl mt-3" style={{ fontVariationSettings: '"opsz" 96' }}>{m.value}</p>
            <p className="text-xs text-[var(--color-ink-muted)] mt-2 font-mono">{m.sub}</p>
          </article>
        ))}
      </div>
    </main>
  )
}

/* ─── Hardware enroll + list ─────────────────────────────────────── */

function HardwareSection() {
  const { data: list, isLoading } = useQuery<WebAuthnRow[]>({
    queryKey: ['credentials', 'webauthn'],
    queryFn: async () => {
      const res = await fetch('/v1/auth/credentials/webauthn', {
        headers: { Authorization: `Bearer ${getToken()}` },
      })
      if (!res.ok) throw new Error(`webauthn list ${res.status}`)
      return res.json()
    },
    staleTime: 30_000,
  })

  return (
    <section className="reveal mt-16 border border-[var(--color-rule)] rounded-sm bg-[var(--color-parchment-deep)]" style={{ animationDelay: '320ms' }}>
      <div className="p-10 md:p-12">
        <p className="eyebrow">Hardware</p>
        <h2 className="font-display text-3xl md:text-4xl mt-3 leading-snug max-w-2xl" style={{ fontVariationSettings: '"opsz" 64' }}>
          Hardware tokens bound to your <span className="italic text-[var(--color-oxblood)]">identity</span>.
        </h2>
        <p className="mt-4 text-[var(--color-ink-muted)] max-w-2xl">
          One-tap sign-in via FIDO2 / WebAuthn. Bind as many as you like — a daily YubiKey, a backup token, your laptop's secure enclave.
        </p>

        <HardwareEnroll />
      </div>

      <div className="border-t border-[var(--color-rule)]">
        {isLoading && (
          <div className="p-10 font-mono text-xs text-[var(--color-ink-muted)]">loading registered tokens…</div>
        )}
        {!isLoading && list && list.length === 0 && (
          <div className="p-10 text-sm text-[var(--color-ink-muted)] italic">
            No hardware tokens registered yet. Enroll one above and it'll show up here.
          </div>
        )}
        {!isLoading && list && list.length > 0 && (
          <ul className="divide-y divide-[var(--color-rule)]">
            {list.map((c) => (
              <HardwareRow key={c.id} cred={c} />
            ))}
          </ul>
        )}
      </div>
    </section>
  )
}

function HardwareRow({ cred }: { cred: WebAuthnRow }) {
  const queryClient = useQueryClient()
  const [revoking, setRevoking] = useState(false)
  const generic = authenticatorName(cred.aaguid)
  const label = cred.nickname?.trim() || generic

  async function revoke() {
    if (!confirm(`Remove "${label}"? You'll lose one-tap sign-in with this token.`)) return
    setRevoking(true)
    try {
      await fetch(`/v1/auth/credentials/webauthn/${cred.id}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${getToken()}` },
      })
      queryClient.invalidateQueries({ queryKey: ['credentials', 'webauthn'] })
    } finally {
      setRevoking(false)
    }
  }

  return (
    <li className="px-10 md:px-12 py-6 flex items-center justify-between gap-6">
      <div className="flex items-center gap-5">
        <span aria-hidden className="w-10 h-10 rounded-sm border border-[var(--color-rule)] bg-[var(--color-parchment)] flex items-center justify-center text-[var(--color-oxblood)]">
          <Seal size={24} />
        </span>
        <div>
          <p className="font-display text-xl leading-tight" style={{ fontVariationSettings: '"opsz" 32' }}>
            {label}
            {cred.nickname && (
              <span className="ml-2 font-mono text-[10px] uppercase tracking-[0.18em] text-[var(--color-ink-muted)] align-middle">
                {generic}
              </span>
            )}
          </p>
          <p className="mt-1 font-mono text-[11px] text-[var(--color-ink-muted)]">
            registered {formatDate(cred.createdAt)} · last used {formatDate(cred.lastUsedAt)}
          </p>
        </div>
      </div>
      <button onClick={revoke} disabled={revoking}
        className="text-xs font-mono uppercase tracking-[0.18em] text-[var(--color-ink-muted)] hover:text-[var(--color-oxblood)] transition-colors disabled:opacity-40">
        {revoking ? 'removing…' : 'revoke'}
      </button>
    </li>
  )
}

function HardwareEnroll() {
  const queryClient = useQueryClient()
  const [state, setState] = useState<'idle' | 'running' | 'done'>('idle')
  const [error, setError] = useState<string | null>(null)
  const [nickname, setNickname] = useState('')

  async function register() {
    setState('running')
    setError(null)
    try {
      const beginRes = await fetch('/v1/auth/credentials/webauthn/begin-registration', {
        method: 'POST',
        headers: { Authorization: `Bearer ${getToken()}` },
      })
      if (!beginRes.ok) throw new Error(`begin ${beginRes.status}`)
      const { ceremonyId, options } = await beginRes.json()

      const attestation = await startRegistration({ optionsJSON: options })

      const completeRes = await fetch('/v1/auth/credentials/webauthn/complete-registration', {
        method: 'POST',
        headers: { 'content-type': 'application/json', Authorization: `Bearer ${getToken()}` },
        body: JSON.stringify({ ceremonyId, nickname: nickname.trim() || null, attestation }),
      })
      if (!completeRes.ok) {
        const body = await completeRes.json().catch(() => ({}))
        throw new Error(body.message ?? `complete ${completeRes.status}`)
      }
      setState('done')
      setNickname('')
      queryClient.invalidateQueries({ queryKey: ['credentials', 'webauthn'] })
      setTimeout(() => setState('idle'), 1600)
    } catch (ex) {
      setError((ex as Error).message ?? 'WebAuthn registration failed.')
      setState('idle')
    }
  }

  return (
    <div className="mt-8 flex flex-wrap items-end gap-4">
      <label className="block flex-1 min-w-[220px]">
        <span className="eyebrow block mb-2">Nickname (optional)</span>
        <input value={nickname} onChange={(e) => setNickname(e.target.value)} placeholder="Work YubiKey"
          className="w-full px-4 py-3 bg-[var(--color-parchment)] border border-[var(--color-rule)] rounded-sm text-sm focus:outline-none focus:border-[var(--color-ink)] transition-colors" />
      </label>
      <button onClick={register} disabled={state !== 'idle'} className="btn-primary disabled:opacity-40">
        {state === 'idle' && <>Touch your key to enroll <span aria-hidden>→</span></>}
        {state === 'running' && (
          <span className="flex items-center gap-2">
            <span className="inline-block w-2 h-2 bg-[var(--color-gold)] rounded-full animate-pulse" />
            waiting for touch…
          </span>
        )}
        {state === 'done' && '✓ Enrolled'}
      </button>
      {error && <div className="basis-full mt-2 text-xs text-[var(--color-oxblood-deep)]">{error}</div>}
    </div>
  )
}

function formatDate(iso: string | null | undefined): string {
  if (!iso) return 'never'
  const d = new Date(iso)
  return d.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}
