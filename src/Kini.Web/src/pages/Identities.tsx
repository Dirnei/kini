import { useState, type FormEvent } from 'react'
import { useOutletContext } from 'react-router'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Avatar } from '../components/Avatar'
import { getToken } from '../lib/api'
import type { AppOutletContext } from '../components/AppShell'

type IdentityRow = {
  id: string
  orgId: string
  username: string
  email: string
  role: 'owner' | 'member'
  displayName: string | null
  createdAt: string
  verifiedAt: string | null
}

export function Identities() {
  const { me } = useOutletContext<AppOutletContext>()
  const isOwner = me.identity.role === 'owner'

  return (
    <main className="flex-1 max-w-[1400px] w-full mx-auto px-6 md:px-10 py-12">
      <p className="eyebrow reveal">Members · {me.organization.name}</p>
      <h1 className="reveal font-display text-5xl md:text-6xl mt-3 leading-[1.02]" style={{ fontVariationSettings: '"opsz" 96', animationDelay: '80ms' }}>
        Who's <span className="italic text-[var(--color-oxblood)]">in</span>.
      </h1>
      <p className="reveal mt-4 text-lg text-[var(--color-ink-muted)] max-w-2xl" style={{ animationDelay: '160ms' }}>
        Identities in your organization. Owners can add members and publish keys on their behalf;
        members manage their own.{' '}
        <em>A member doesn't need to register</em> — once you've published their key, it resolves
        publicly; they sign in later (using that key) only if they need the console.
      </p>

      {isOwner && <AddMemberForm orgId={me.identity.orgId} />}

      <IdentityList />
    </main>
  )
}

/* ─── add member ──────────────────────────────────────────── */

function AddMemberForm({ orgId }: { orgId: string }) {
  const queryClient = useQueryClient()
  const [username, setUsername] = useState('')
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [pubkey, setPubkey] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setSuccess(null)
    setSubmitting(true)
    try {
      const createRes = await fetch(`/v1/orgs/${orgId}/identities`, {
        method: 'POST',
        headers: { 'content-type': 'application/json', Authorization: `Bearer ${getToken()}` },
        body: JSON.stringify({
          username: username.trim().toLowerCase(),
          email: email.trim(),
          displayName: displayName.trim() || null,
        }),
      })
      const body = await createRes.json().catch(() => ({}))
      if (!createRes.ok) {
        setError(body.message ?? body.code ?? `Create failed (${createRes.status}).`)
        return
      }
      const created: IdentityRow = body

      // Optional: publish a key for them right now.
      if (pubkey.trim().length >= 20) {
        const uploadRes = await fetch(`/v1/identities/${encodeURIComponent(created.email)}/keys`, {
          method: 'POST',
          headers: { 'content-type': 'application/json', Authorization: `Bearer ${getToken()}` },
          body: JSON.stringify({ type: 'ssh', publicKey: pubkey.trim() }),
        })
        if (!uploadRes.ok) {
          const ub = await uploadRes.json().catch(() => ({}))
          setError(`Member created, but key upload failed: ${ub.message ?? ub.code ?? uploadRes.status}`)
          queryClient.invalidateQueries({ queryKey: ['identities', orgId] })
          return
        }
      }

      setSuccess(`Added ${created.username}. ${pubkey.trim() ? 'Key resolves at /' + created.username + '.keys.' : ''}`)
      setUsername(''); setEmail(''); setDisplayName(''); setPubkey('')
      queryClient.invalidateQueries({ queryKey: ['identities', orgId] })
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={submit} className="reveal mt-12 border border-[var(--color-rule)] rounded-sm p-8 bg-[var(--color-parchment-deep)]" style={{ animationDelay: '220ms' }}>
      <p className="eyebrow">Onboard</p>
      <h2 className="font-display text-2xl md:text-3xl mt-2" style={{ fontVariationSettings: '"opsz" 48' }}>
        Add a member.
      </h2>
      <p className="mt-2 text-sm text-[var(--color-ink-muted)] max-w-2xl">
        Only the username and email are required. Paste their public key now to publish it on the spot — or leave it blank and they (or you) can do it later.
      </p>

      <div className="mt-6 grid grid-cols-1 md:grid-cols-3 gap-4">
        <Field label="Username">
          <input required pattern="[a-z0-9]([a-z0-9-]{0,30}[a-z0-9])?" minLength={2} maxLength={32}
            value={username} onChange={(e) => setUsername(e.target.value.toLowerCase())}
            placeholder="alice" className={`${inputCls} font-mono`} />
        </Field>
        <Field label="Email">
          <input required type="email" value={email} onChange={(e) => setEmail(e.target.value)}
            placeholder="alice@acme.tld" className={inputCls} />
        </Field>
        <Field label="Display name (optional)">
          <input value={displayName} onChange={(e) => setDisplayName(e.target.value)}
            placeholder="Alice" className={inputCls} />
        </Field>
      </div>

      <Field label="SSH public key (optional — publishes immediately if set)" className="mt-4">
        <textarea rows={3} value={pubkey} onChange={(e) => setPubkey(e.target.value)}
          placeholder="ssh-ed25519 AAAAC3Nz...  alice@laptop"
          className={`${inputCls} font-mono text-xs leading-relaxed`} />
      </Field>

      {error && <div className="mt-4 text-xs text-[var(--color-oxblood-deep)]">{error}</div>}
      {success && <div className="mt-4 text-xs text-[var(--color-gold-deep)]">✓ {success}</div>}

      <div className="mt-5 flex justify-end">
        <button type="submit" disabled={submitting} className="btn-primary disabled:opacity-40">
          {submitting ? 'Adding…' : <>Add member <span aria-hidden>→</span></>}
        </button>
      </div>
    </form>
  )
}

/* ─── list ────────────────────────────────────────────────── */

function IdentityList() {
  const { me } = useOutletContext<AppOutletContext>()
  const { data, isLoading } = useQuery<IdentityRow[]>({
    queryKey: ['identities', me.identity.orgId],
    queryFn: async () => {
      const res = await fetch(`/v1/orgs/${me.identity.orgId}/identities`, {
        headers: { Authorization: `Bearer ${getToken()}` },
      })
      if (!res.ok) throw new Error(`list ${res.status}`)
      return res.json()
    },
    staleTime: 30_000,
  })

  if (isLoading) return <div className="mt-10 font-mono text-xs text-[var(--color-ink-muted)]">loading…</div>
  if (!data || data.length === 0) return <div className="mt-10 text-sm text-[var(--color-ink-muted)] italic">No members yet.</div>

  return (
    <section className="reveal mt-12 border border-[var(--color-rule)] rounded-sm divide-y divide-[var(--color-rule)] bg-[var(--color-parchment)]" style={{ animationDelay: '320ms' }}>
      {data.map((i) => (
        <article key={i.id} className="px-6 py-5 flex items-center justify-between gap-4">
          <div className="flex items-center gap-4 min-w-0 flex-1">
            <Avatar email={i.email} size={40} fallbackLabel={i.displayName ?? i.username} />
            <div className="min-w-0">
              <p className="font-display text-xl leading-tight" style={{ fontVariationSettings: '"opsz" 32' }}>
                {i.displayName ?? i.username}
                <span className="ml-2 font-mono text-[10px] uppercase tracking-[0.18em] text-[var(--color-oxblood)]">{i.role}</span>
              </p>
              <p className="mt-1 font-mono text-[11px] text-[var(--color-ink-muted)] truncate">
                <span className="text-[var(--color-ink)]">{i.username}</span> · {i.email} · joined {new Date(i.createdAt).toLocaleDateString()}
              </p>
            </div>
          </div>
          <a href={`/${i.username}.keys`} target="_blank" rel="noreferrer"
             className="text-xs font-mono uppercase tracking-[0.18em] text-[var(--color-ink-muted)] hover:text-[var(--color-oxblood)] transition-colors">
            /{i.username}.keys ↗
          </a>
        </article>
      ))}
    </section>
  )
}

/* ─── shared field bits ───────────────────────────────────── */

const inputCls =
  'w-full px-4 py-3 bg-[var(--color-parchment)] border border-[var(--color-rule)] rounded-sm text-sm focus:outline-none focus:border-[var(--color-ink)] transition-colors'

function Field({ label, children, className = '' }: { label: string; children: React.ReactNode; className?: string }) {
  return (
    <label className={`block ${className}`}>
      <span className="eyebrow block mb-2">{label}</span>
      {children}
    </label>
  )
}
