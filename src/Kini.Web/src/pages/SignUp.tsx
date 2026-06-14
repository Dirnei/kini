import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router'
import { Seal } from '../components/Seal'
import { setToken } from '../lib/api'

export function SignUp() {
  const navigate = useNavigate()

  const [orgName, setOrgName] = useState('')
  const [primaryDomain, setPrimaryDomain] = useState('')
  const [username, setUsername] = useState('')
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [pubkey, setPubkey] = useState('')
  const [publishKey, setPublishKey] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setSubmitting(true)
    try {
      const res = await fetch('/v1/sign-up', {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({
          organizationName: orgName.trim(),
          primaryDomain: primaryDomain.trim() || null,
          username: username.trim().toLowerCase(),
          email: email.trim(),
          displayName: displayName.trim() || null,
          sshPublicKey: pubkey.trim(),
          publishKey,
        }),
      })
      const body = await res.json().catch(() => ({}))
      if (!res.ok) {
        setError(body.message ?? body.code ?? `Sign-up failed (${res.status}).`)
        return
      }
      setToken(body.session.token)
      navigate('/app')
    } catch (ex) {
      setError((ex as Error).message ?? 'Sign-up failed.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="min-h-screen flex flex-col">
      <header className="px-8 md:px-16 py-8 flex items-center justify-between max-w-[1400px] w-full mx-auto">
        <Link to="/" className="flex items-center gap-3 group">
          <span className="text-[var(--color-ink)] group-hover:text-[var(--color-oxblood)] transition-colors">
            <Seal size={32} />
          </span>
          <span className="font-display text-xl tracking-tight" style={{ fontVariationSettings: '"opsz" 36' }}>Kini</span>
        </Link>
        <Link to="/" className="text-sm text-[var(--color-ink-muted)] hover:text-[var(--color-ink)]">← Back</Link>
      </header>

      <main className="flex-1 grid grid-cols-1 lg:grid-cols-12">
        <aside className="lg:col-span-5 px-8 md:px-16 lg:px-20 py-12 lg:py-24 reveal">
          <p className="eyebrow mb-6">Register</p>
          <h1 className="font-display text-5xl lg:text-6xl leading-[0.98]" style={{ fontVariationSettings: '"opsz" 144' }}>
            Forge the
            <span className="block italic text-[var(--color-oxblood)]">seal.</span>
          </h1>
          <p className="mt-8 text-lg text-[var(--color-ink-muted)] leading-relaxed max-w-md">
            One form, four facts: who you are, what your organization is called, and the
            public key that will vouch for you from this moment on.
          </p>
          <div className="rule mt-12 mb-8 max-w-md" />
          <p className="font-mono text-xs uppercase tracking-[0.22em] text-[var(--color-ink-muted)]">
            Your private key never leaves your device.
          </p>
        </aside>

        <section className="lg:col-span-7 bg-[var(--color-parchment-deep)] px-6 md:px-12 lg:px-20 py-12 lg:py-24 flex items-center reveal" style={{ animationDelay: '180ms' }}>
          <form onSubmit={submit} className="w-full max-w-xl mx-auto space-y-6">
            <Field label="Organization">
              <input type="text" required value={orgName} onChange={(e) => setOrgName(e.target.value)}
                placeholder="Acme, Inc." className={inputCls} />
            </Field>

            <Field label="Primary domain (optional)">
              <input type="text" value={primaryDomain} onChange={(e) => setPrimaryDomain(e.target.value)}
                placeholder="acme.tld" className={inputCls} />
            </Field>

            <Field label="Username (public handle)">
              <input
                type="text"
                required
                pattern="[a-z0-9]([a-z0-9-]{0,30}[a-z0-9])?"
                minLength={2}
                maxLength={32}
                value={username}
                onChange={(e) => setUsername(e.target.value.toLowerCase())}
                placeholder="dirnei"
                className={`${inputCls} font-mono`}
              />
              <p className="mt-2 text-xs text-[var(--color-ink-muted)]">
                Globally unique. Appears in your URL as{' '}
                <code className="font-mono text-[0.7rem] text-[var(--color-ink)]">/{username || 'username'}.keys</code>.
                Lowercase letters, digits, and hyphens.
              </p>
            </Field>

            <Field label="Your email">
              <input type="email" required value={email} onChange={(e) => setEmail(e.target.value)}
                placeholder="alice@acme.tld" className={inputCls} />
              <p className="mt-2 text-xs text-[var(--color-ink-muted)]">
                Used for WKD (<code className="font-mono text-[0.7rem] text-[var(--color-ink)]">gpg --locate-keys</code>) and for sign-in.
              </p>
            </Field>

            <Field label="Display name (optional)">
              <input type="text" value={displayName} onChange={(e) => setDisplayName(e.target.value)}
                placeholder="Alice" className={inputCls} />
            </Field>

            <Field label="Your SSH public key">
              <textarea required rows={4} value={pubkey} onChange={(e) => setPubkey(e.target.value)}
                placeholder="ssh-ed25519 AAAAC3Nz...  alice@laptop"
                className={`${inputCls} font-mono text-xs leading-relaxed`} />
              <p className="mt-2 text-xs text-[var(--color-ink-muted)]">
                One line. From <code className="font-mono text-[0.7rem] text-[var(--color-ink)]">cat ~/.ssh/id_ed25519.pub</code> or <code className="font-mono text-[0.7rem] text-[var(--color-ink)]">ssh-add -L | head -1</code>.
              </p>
            </Field>

            <label className="flex items-start gap-3 cursor-pointer select-none">
              <input type="checkbox" checked={publishKey} onChange={(e) => setPublishKey(e.target.checked)}
                className="mt-1 w-4 h-4 accent-[var(--color-oxblood)] cursor-pointer" />
              <span className="text-sm text-[var(--color-ink-muted)] leading-relaxed">
                Also publish this key to the directory at
                {' '}<code className="font-mono text-[0.75rem] text-[var(--color-ink)]">/{username || 'username'}.keys</code> so your team's tools resolve it.
                <span className="block text-xs mt-1">Uncheck to keep it private for sign-in only.</span>
              </span>
            </label>

            {error && (
              <div className="border border-[var(--color-oxblood)] bg-[var(--color-oxblood)]/8 px-4 py-3 rounded-sm text-sm text-[var(--color-oxblood-deep)]">
                {error}
              </div>
            )}

            <button type="submit" disabled={submitting} className="btn-primary w-full justify-center disabled:opacity-40 disabled:cursor-not-allowed">
              {submitting ? 'Forging…' : <>Create organization <span aria-hidden>→</span></>}
            </button>

            <p className="text-xs text-[var(--color-ink-muted)] text-center">
              Already have an organization? <Link to="/sign-in" className="underline underline-offset-4">Sign in</Link>.
            </p>
          </form>
        </section>
      </main>
    </div>
  )
}

const inputCls =
  'w-full px-4 py-3 bg-[var(--color-parchment)] border border-[var(--color-rule)] rounded-sm text-sm focus:outline-none focus:border-[var(--color-ink)] transition-colors'

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="block">
      <span className="eyebrow block mb-2">{label}</span>
      {children}
    </label>
  )
}
