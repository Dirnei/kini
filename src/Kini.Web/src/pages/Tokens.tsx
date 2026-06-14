import { useState, type FormEvent } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { getToken } from '../lib/api'

type ApiTokenRow = {
  id: string
  name: string
  scopes: string[]
  createdAt: string
  lastUsedAt: string | null
  revokedAt: string | null
}

export function Tokens() {
  const [issuedToken, setIssuedToken] = useState<{ name: string; token: string } | null>(null)

  return (
    <main className="flex-1 max-w-[1400px] w-full mx-auto px-6 md:px-10 py-12">
      <p className="eyebrow reveal">API tokens</p>
      <h1 className="reveal font-display text-5xl md:text-6xl mt-3 leading-[1.02]" style={{ fontVariationSettings: '"opsz" 96', animationDelay: '80ms' }}>
        Bearer tokens for <span className="italic text-[var(--color-oxblood)]">things that aren't you</span>.
      </h1>
      <p className="reveal mt-4 text-lg text-[var(--color-ink-muted)] max-w-2xl" style={{ animationDelay: '160ms' }}>
        Long-lived bearer tokens for the CLI, CI, and any automation that needs to call the Kini API.
        Use them in <code className="font-mono text-base text-[var(--color-ink)]">Authorization: Bearer &lt;token&gt;</code>.
        The plaintext is shown <em>once</em>, on create — never again.
      </p>

      <CreateForm onIssued={(name, token) => setIssuedToken({ name, token })} />

      {issuedToken && <IssuedTokenCard name={issuedToken.name} token={issuedToken.token} onDismiss={() => setIssuedToken(null)} />}

      <TokenList />
    </main>
  )
}

/* ────────────────────────────────────────────────────────── */

function CreateForm({ onIssued }: { onIssued: (name: string, token: string) => void }) {
  const queryClient = useQueryClient()
  const [name, setName] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const res = await fetch('/v1/api-tokens', {
        method: 'POST',
        headers: { 'content-type': 'application/json', Authorization: `Bearer ${getToken()}` },
        body: JSON.stringify({ name: name.trim() }),
      })
      const body = await res.json().catch(() => ({}))
      if (!res.ok) {
        setError(body.message ?? body.code ?? `Create failed (${res.status}).`)
        return
      }
      onIssued(body.name, body.token)
      setName('')
      queryClient.invalidateQueries({ queryKey: ['api-tokens'] })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={submit} className="reveal mt-12 border border-[var(--color-rule)] rounded-sm p-8 bg-[var(--color-parchment-deep)] flex flex-wrap items-end gap-4" style={{ animationDelay: '220ms' }}>
      <label className="block flex-1 min-w-[280px]">
        <span className="eyebrow block mb-2">Token name</span>
        <input required value={name} onChange={(e) => setName(e.target.value)} placeholder="kini-cli on alice's laptop"
          className="w-full px-4 py-3 bg-[var(--color-parchment)] border border-[var(--color-rule)] rounded-sm text-sm focus:outline-none focus:border-[var(--color-ink)] transition-colors" />
      </label>
      <button type="submit" disabled={submitting || !name.trim()} className="btn-primary disabled:opacity-40">
        {submitting ? 'Minting…' : <>Mint token <span aria-hidden>→</span></>}
      </button>
      {error && <div className="basis-full text-xs text-[var(--color-oxblood-deep)]">{error}</div>}
    </form>
  )
}

/* ────────────────────────────────────────────────────────── */

function IssuedTokenCard({ name, token, onDismiss }: { name: string; token: string; onDismiss: () => void }) {
  const [copied, setCopied] = useState(false)
  async function copy() {
    try {
      await navigator.clipboard.writeText(token)
      setCopied(true)
      window.setTimeout(() => setCopied(false), 1800)
    } catch { /* clipboard may be unavailable */ }
  }
  return (
    <section className="mt-8 border-2 border-[var(--color-oxblood)] rounded-sm p-7 bg-[var(--color-parchment)]">
      <p className="eyebrow text-[var(--color-oxblood)]">⚠ Plaintext token — shown once</p>
      <h3 className="font-display text-2xl mt-2" style={{ fontVariationSettings: '"opsz" 48' }}>{name}</h3>
      <p className="mt-2 text-sm text-[var(--color-ink-muted)]">
        Copy this now. It can't be recovered later; if you lose it you'll need to mint a new one.
      </p>
      <div className="mt-4 flex items-center justify-between bg-[var(--color-ink)] text-[var(--color-parchment)] rounded-sm p-4">
        <code className="font-mono text-sm truncate">{token}</code>
        <button onClick={copy} className="text-[10px] font-mono uppercase tracking-[0.18em] text-[var(--color-parchment)]/45 hover:text-[var(--color-gold)] transition-colors px-2">
          {copied ? '✓ copied' : 'copy'}
        </button>
      </div>
      <div className="mt-4 flex justify-end">
        <button onClick={onDismiss} className="text-xs font-mono uppercase tracking-[0.18em] text-[var(--color-ink-muted)] hover:text-[var(--color-ink)] transition-colors">
          I've stored it
        </button>
      </div>
    </section>
  )
}

/* ────────────────────────────────────────────────────────── */

function TokenList() {
  const queryClient = useQueryClient()
  const { data, isLoading } = useQuery<ApiTokenRow[]>({
    queryKey: ['api-tokens'],
    queryFn: async () => {
      const res = await fetch('/v1/api-tokens', {
        headers: { Authorization: `Bearer ${getToken()}` },
      })
      if (!res.ok) throw new Error(`list ${res.status}`)
      return res.json()
    },
    staleTime: 30_000,
  })

  async function revoke(id: string, name: string) {
    if (!confirm(`Revoke "${name}"? Anything authenticating with it stops working immediately.`)) return
    await fetch(`/v1/api-tokens/${id}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${getToken()}` },
    })
    queryClient.invalidateQueries({ queryKey: ['api-tokens'] })
  }

  if (isLoading) return <div className="mt-10 font-mono text-xs text-[var(--color-ink-muted)]">loading…</div>
  if (!data || data.length === 0) return <div className="mt-10 text-sm text-[var(--color-ink-muted)] italic">No tokens yet.</div>

  return (
    <section className="reveal mt-10 border border-[var(--color-rule)] rounded-sm divide-y divide-[var(--color-rule)] bg-[var(--color-parchment)]" style={{ animationDelay: '320ms' }}>
      {data.map((t) => (
        <article key={t.id} className="px-6 py-5 flex items-center justify-between gap-4">
          <div className="min-w-0">
            <p className="font-display text-xl leading-tight" style={{ fontVariationSettings: '"opsz" 32' }}>{t.name}</p>
            <p className="mt-1 font-mono text-[11px] text-[var(--color-ink-muted)]">
              created {new Date(t.createdAt).toLocaleDateString()} · last used {t.lastUsedAt ? new Date(t.lastUsedAt).toLocaleDateString() : 'never'}
            </p>
          </div>
          <button onClick={() => revoke(t.id, t.name)}
            className="text-xs font-mono uppercase tracking-[0.18em] text-[var(--color-ink-muted)] hover:text-[var(--color-oxblood)] transition-colors">
            revoke
          </button>
        </article>
      ))}
    </section>
  )
}
