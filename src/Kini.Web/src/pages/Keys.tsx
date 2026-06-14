import { useState, type FormEvent } from 'react'
import { useOutletContext } from 'react-router'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { getToken } from '../lib/api'
import type { AppOutletContext } from '../components/AppShell'

type PublishedKey = {
  id: string
  identityId: string
  orgId: string
  type: 'ssh' | 'gpg'
  fingerprint: string
  algorithm: string
  publicKey: string
  comment: string | null
  provenance: string
  createdAt: string
  expiresAt: string | null
  revokedAt: string | null
}

export function Keys() {
  const { me } = useOutletContext<AppOutletContext>()
  const publicUrl = `${window.location.origin}/${me.identity.username}.keys`

  return (
    <main className="flex-1 max-w-[1400px] w-full mx-auto px-6 md:px-10 py-12">
      <p className="eyebrow reveal">Published keys</p>
      <h1 className="reveal font-display text-5xl md:text-6xl mt-3 leading-[1.02]" style={{ fontVariationSettings: '"opsz" 96', animationDelay: '80ms' }}>
        What the <span className="italic text-[var(--color-oxblood)]">world</span> can resolve.
      </h1>
      <p className="reveal mt-4 text-lg text-[var(--color-ink-muted)] max-w-2xl" style={{ animationDelay: '160ms' }}>
        Keys you publish here are served at the URL below — Ansible's <code className="font-mono text-base text-[var(--color-ink)]">authorized_key</code> module
        and GitHub-style consumers resolve them without any config on the consumer side.
      </p>

      <PublicUrl url={publicUrl} />

      <UploadForm email={me.identity.email} />

      <KeysList email={me.identity.email} />
    </main>
  )
}

/* ────────────────────────────────────────────────────────── */

function PublicUrl({ url }: { url: string }) {
  const [copied, setCopied] = useState(false)
  async function copy() {
    try {
      await navigator.clipboard.writeText(url)
      setCopied(true)
      window.setTimeout(() => setCopied(false), 1800)
    } catch { /* clipboard may be unavailable */ }
  }
  return (
    <div className="reveal mt-12 flex items-center justify-between bg-[var(--color-ink)] text-[var(--color-parchment)] rounded-sm p-5" style={{ animationDelay: '220ms' }}>
      <code className="font-mono text-sm md:text-base truncate">
        <span className="text-[var(--color-gold)]">GET </span>{url}
      </code>
      <button onClick={copy} className="text-[10px] font-mono uppercase tracking-[0.18em] text-[var(--color-parchment)]/45 hover:text-[var(--color-gold)] transition-colors px-2">
        {copied ? '✓ copied' : 'copy'}
      </button>
    </div>
  )
}

/* ────────────────────────────────────────────────────────── */

function UploadForm({ email }: { email: string }) {
  const queryClient = useQueryClient()
  const [pubkey, setPubkey] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setSuccess(false)
    setSubmitting(true)
    try {
      const res = await fetch(`/v1/identities/${encodeURIComponent(email)}/keys`, {
        method: 'POST',
        headers: { 'content-type': 'application/json', Authorization: `Bearer ${getToken()}` },
        body: JSON.stringify({ type: 'ssh', publicKey: pubkey.trim() }),
      })
      const body = await res.json().catch(() => ({}))
      if (!res.ok) {
        setError(body.message ?? body.code ?? `Upload failed (${res.status}).`)
        return
      }
      setPubkey('')
      setSuccess(true)
      queryClient.invalidateQueries({ queryKey: ['keys', email] })
      window.setTimeout(() => setSuccess(false), 2000)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={submit} className="reveal mt-10 border border-[var(--color-rule)] rounded-sm p-8 bg-[var(--color-parchment-deep)]" style={{ animationDelay: '300ms' }}>
      <p className="eyebrow">Publish</p>
      <h2 className="font-display text-2xl md:text-3xl mt-2" style={{ fontVariationSettings: '"opsz" 48' }}>
        Add an SSH key.
      </h2>
      <p className="mt-2 text-sm text-[var(--color-ink-muted)] max-w-2xl">
        Paste one <code className="font-mono text-xs text-[var(--color-ink)]">authorized_keys</code>-format line.
        The fingerprint is derived server-side; duplicates are rejected.
      </p>

      <textarea required rows={4} value={pubkey} onChange={(e) => setPubkey(e.target.value)}
        placeholder="ssh-ed25519 AAAAC3Nz...  alice@laptop"
        className="mt-5 w-full px-4 py-3 bg-[var(--color-parchment)] border border-[var(--color-rule)] rounded-sm font-mono text-xs leading-relaxed focus:outline-none focus:border-[var(--color-ink)] transition-colors" />

      {error && <div className="mt-4 text-xs text-[var(--color-oxblood-deep)]">{error}</div>}
      {success && <div className="mt-4 text-xs text-[var(--color-gold-deep)]">✓ Published. Resolves at the URL above.</div>}

      <div className="mt-5 flex justify-end">
        <button type="submit" disabled={submitting || pubkey.trim().length < 20} className="btn-primary disabled:opacity-40">
          {submitting ? 'Publishing…' : <>Publish key <span aria-hidden>→</span></>}
        </button>
      </div>
    </form>
  )
}

/* ────────────────────────────────────────────────────────── */

function KeysList({ email }: { email: string }) {
  const queryClient = useQueryClient()
  const { data: keys, isLoading } = useQuery<PublishedKey[]>({
    queryKey: ['keys', email],
    queryFn: async () => {
      const res = await fetch(`/v1/identities/${encodeURIComponent(email)}/keys`, {
        headers: { Authorization: `Bearer ${getToken()}` },
      })
      if (!res.ok) throw new Error(`list ${res.status}`)
      return res.json()
    },
    staleTime: 30_000,
  })

  async function revoke(keyId: string, fp: string) {
    if (!confirm(`Revoke key ${fp}? Consumers will stop seeing it on next fetch.`)) return
    await fetch(`/v1/keys/${keyId}/revoke`, {
      method: 'POST',
      headers: { 'content-type': 'application/json', Authorization: `Bearer ${getToken()}` },
      body: JSON.stringify({ reason: 'revoked from web UI' }),
    })
    queryClient.invalidateQueries({ queryKey: ['keys', email] })
  }

  if (isLoading) {
    return <div className="mt-10 font-mono text-xs text-[var(--color-ink-muted)]">loading…</div>
  }
  if (!keys || keys.length === 0) {
    return <div className="mt-10 text-sm text-[var(--color-ink-muted)] italic">No keys published yet. Add one above.</div>
  }

  return (
    <section className="reveal mt-10 border border-[var(--color-rule)] rounded-sm divide-y divide-[var(--color-rule)] bg-[var(--color-parchment)]" style={{ animationDelay: '380ms' }}>
      {keys.map((k) => (
        <article key={k.id} className="px-6 py-5 flex flex-col md:flex-row md:items-center md:justify-between gap-3">
          <div className="min-w-0 flex-1">
            <div className="flex items-center gap-3">
              <span className="font-mono text-[0.7rem] uppercase tracking-[0.18em] text-[var(--color-oxblood)]">{k.algorithm}</span>
              <span className="font-mono text-[0.7rem] uppercase tracking-[0.18em] text-[var(--color-ink-muted)]">{k.provenance}</span>
            </div>
            <code className="font-mono text-xs text-[var(--color-ink)] break-all block mt-2">{k.fingerprint}</code>
            <p className="mt-1 font-mono text-[11px] text-[var(--color-ink-muted)]">
              {k.comment ?? '—'} · added {new Date(k.createdAt).toLocaleDateString()}
            </p>
          </div>
          <button onClick={() => revoke(k.id, k.fingerprint)}
            className="text-xs font-mono uppercase tracking-[0.18em] text-[var(--color-ink-muted)] hover:text-[var(--color-oxblood)] transition-colors">
            revoke
          </button>
        </article>
      ))}
    </section>
  )
}
