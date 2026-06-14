import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { Seal } from '../components/Seal'

type Method = 'hardware' | 'ssh'

export function SignIn() {
  const [method, setMethod] = useState<Method>('hardware')

  return (
    <div className="min-h-screen flex flex-col">
      {/* Slim header */}
      <header className="px-8 md:px-16 py-8 flex items-center justify-between max-w-[1400px] w-full mx-auto">
        <Link to="/" className="flex items-center gap-3 group">
          <span className="text-[var(--color-ink)] group-hover:text-[var(--color-oxblood)] transition-colors">
            <Seal size={32} />
          </span>
          <span className="font-display text-xl tracking-tight" style={{ fontVariationSettings: '"opsz" 36' }}>
            Kini
          </span>
        </Link>
        <Link to="/" className="text-sm text-[var(--color-ink-muted)] hover:text-[var(--color-ink)]">
          ← Back to landing
        </Link>
      </header>

      <main className="flex-1 grid grid-cols-1 lg:grid-cols-12 gap-0">
        {/* Left side — credential ceremony copy */}
        <aside className="lg:col-span-5 px-8 md:px-16 lg:px-20 py-12 lg:py-24 reveal">
          <p className="eyebrow mb-6">Sign in</p>
          <h1 className="font-display text-5xl lg:text-6xl leading-[0.98]" style={{ fontVariationSettings: '"opsz" 144' }}>
            Press the
            <span className="block italic text-[var(--color-oxblood)]">seal.</span>
          </h1>
          <p className="mt-8 text-lg text-[var(--color-ink-muted)] leading-relaxed max-w-md">
            Authenticate with the same hardware token that holds your operational keys.
            Two flavors: the modern FIDO2 ceremony, or the time-honored
            SSH-key challenge for the purists.
          </p>

          <div className="rule mt-12 mb-12 max-w-md" />

          <p className="font-mono text-xs uppercase tracking-[0.22em] text-[var(--color-ink-muted)]">
            No password. No SMS. No email link.
          </p>
        </aside>

        {/* Right side — the auth panel */}
        <section className="lg:col-span-7 bg-[var(--color-parchment-deep)] px-6 md:px-12 lg:px-20 py-12 lg:py-24 flex items-center reveal" style={{ animationDelay: '180ms' }}>
          <div className="w-full max-w-xl mx-auto">
            {/* Tab selector */}
            <div role="tablist" className="grid grid-cols-2 mb-10 border border-[var(--color-rule)] rounded-sm overflow-hidden">
              <button
                role="tab"
                aria-selected={method === 'hardware'}
                onClick={() => setMethod('hardware')}
                className={`px-5 py-3.5 text-sm font-medium transition-colors ${
                  method === 'hardware'
                    ? 'bg-[var(--color-ink)] text-[var(--color-parchment)]'
                    : 'bg-transparent text-[var(--color-ink-muted)] hover:bg-[var(--color-parchment)]'
                }`}
              >
                Hardware key
                <span className="ml-2 font-mono text-[0.65rem] opacity-70">WebAuthn</span>
              </button>
              <button
                role="tab"
                aria-selected={method === 'ssh'}
                onClick={() => setMethod('ssh')}
                className={`px-5 py-3.5 text-sm font-medium transition-colors border-l border-[var(--color-rule)] ${
                  method === 'ssh'
                    ? 'bg-[var(--color-ink)] text-[var(--color-parchment)]'
                    : 'bg-transparent text-[var(--color-ink-muted)] hover:bg-[var(--color-parchment)]'
                }`}
              >
                SSH key
                <span className="ml-2 font-mono text-[0.65rem] opacity-70">ssh-keygen -Y sign</span>
              </button>
            </div>

            {method === 'hardware' ? <HardwareKeyPanel /> : <SshKeyPanel />}

            <p className="mt-10 text-xs text-[var(--color-ink-muted)] leading-relaxed">
              Don't have a YubiKey? Start with the <Link to="/sign-in" className="underline underline-offset-4">SSH key flow</Link> —
              same identity surface, browser does less of the work.
            </p>
          </div>
        </section>
      </main>
    </div>
  )
}

/* ──────────────────────────────────────────────────────────────── */
function HardwareKeyPanel() {
  const navigate = useNavigate()
  const [state, setState] = useState<'idle' | 'waiting' | 'done'>('idle')
  const [email, setEmail] = useState('')

  async function start() {
    setState('waiting')
    // Real implementation: navigator.credentials.get({ publicKey: ... }) using a
    // challenge fetched from the API. Stubbed for now — pretend we're waiting on
    // a hardware touch, then redirect to the dashboard.
    await new Promise((r) => setTimeout(r, 1400))
    setState('done')
    setTimeout(() => navigate('/app'), 500)
  }

  return (
    <div>
      <label className="block">
        <span className="eyebrow block mb-2">Email</span>
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="alice@acme.tld"
          className="w-full px-4 py-3.5 bg-[var(--color-parchment)] border border-[var(--color-rule)] rounded-sm font-mono text-sm focus:outline-none focus:border-[var(--color-ink)] transition-colors"
        />
      </label>

      <button
        onClick={start}
        disabled={state !== 'idle' || !email.includes('@')}
        className="btn-primary w-full mt-6 justify-center disabled:opacity-40 disabled:cursor-not-allowed"
      >
        {state === 'idle' && (
          <>
            Touch your security key
            <span aria-hidden>→</span>
          </>
        )}
        {state === 'waiting' && (
          <span className="flex items-center gap-3">
            <span className="inline-block w-2 h-2 bg-[var(--color-gold)] rounded-full animate-pulse" />
            Waiting for hardware touch…
          </span>
        )}
        {state === 'done' && '✓ Verified'}
      </button>

      <div className="mt-8 p-5 bg-[var(--color-parchment)] border border-[var(--color-rule)] rounded-sm">
        <p className="eyebrow mb-3">What happens</p>
        <ol className="text-sm leading-relaxed text-[var(--color-ink-muted)] space-y-2 list-decimal list-inside">
          <li>Browser asks your YubiKey to sign a server-issued challenge.</li>
          <li>You touch the key. The signature never leaves your device unsigned.</li>
          <li>Kini verifies against the FIDO2 credential bound to your identity.</li>
        </ol>
      </div>
    </div>
  )
}

/* ──────────────────────────────────────────────────────────────── */
function SshKeyPanel() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [step, setStep] = useState<'email' | 'sign' | 'done'>('email')
  const [signature, setSignature] = useState('')
  const [copied, setCopied] = useState(false)

  const challenge = 'nonce.5fK2-pX9w-r3vA-q8nB-zL4mY7tH'
  // Point `-f` at the .pub file: ssh-keygen reads the public key from disk
  // and asks ssh-agent for the matching private. Works equally for
  // file-backed keys and for YubiKey / FIDO2 / gpg-agent identities.
  const command = `echo "${challenge}" | ssh-keygen -Y sign -n kini -f ~/.ssh/id_ed25519.pub`

  async function copyCommand() {
    try {
      await navigator.clipboard.writeText(command)
      setCopied(true)
      window.setTimeout(() => setCopied(false), 1800)
    } catch {
      /* Clipboard API can be unavailable (e.g., non-secure context). Fail quiet. */
    }
  }

  function startChallenge() {
    if (!email.includes('@')) return
    setStep('sign')
  }

  function verify() {
    if (signature.trim().length < 20) return
    setStep('done')
    setTimeout(() => navigate('/app'), 600)
  }

  return (
    <div>
      <label className="block">
        <span className="eyebrow block mb-2">Email</span>
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="alice@acme.tld"
          disabled={step !== 'email'}
          className="w-full px-4 py-3.5 bg-[var(--color-parchment)] border border-[var(--color-rule)] rounded-sm font-mono text-sm focus:outline-none focus:border-[var(--color-ink)] transition-colors disabled:opacity-60"
        />
      </label>

      {step === 'email' && (
        <button onClick={startChallenge} disabled={!email.includes('@')} className="btn-primary w-full mt-6 justify-center disabled:opacity-40">
          Request challenge
          <span aria-hidden>→</span>
        </button>
      )}

      {step !== 'email' && (
        <>
          <div className="mt-8">
            <p className="eyebrow mb-3">1. Sign the challenge locally</p>
            <div className="relative group">
              <pre className="font-mono text-xs bg-[var(--color-ink)] text-[var(--color-parchment)] p-5 pr-20 rounded-sm overflow-x-auto leading-relaxed">
{`$ echo "${challenge}" \\
    | ssh-keygen -Y sign -n kini -f ~/.ssh/id_ed25519.pub`}
              </pre>
              <button
                type="button"
                onClick={copyCommand}
                aria-label="Copy command"
                className="absolute top-3 right-3 px-1.5 py-0.5 text-[10px] font-mono uppercase tracking-[0.18em] text-[var(--color-parchment)]/45 hover:text-[var(--color-gold)] focus:text-[var(--color-gold)] outline-none transition-colors"
              >
                {copied ? '✓ copied' : 'copy'}
              </button>
            </div>
            <p className="mt-3 text-xs text-[var(--color-ink-muted)] leading-relaxed">
              The challenge expires in 5 minutes. We never see your private key.
              Pointing <code className="font-mono text-[0.7rem] text-[var(--color-ink)]">-f</code> at
              the <code className="font-mono text-[0.7rem] text-[var(--color-ink)]">.pub</code> file
              lets <code className="font-mono text-[0.7rem] text-[var(--color-ink)]">ssh-agent</code> do
              the actual signing — works for YubiKey, FIDO2, and gpg-agent identities.
            </p>

            <details className="mt-4 group">
              <summary className="list-none cursor-pointer select-none flex items-center gap-2 text-xs text-[var(--color-ink-muted)] hover:text-[var(--color-ink)] transition-colors">
                <span className="font-mono text-[var(--color-gold)] transition-transform group-open:rotate-90">›</span>
                <span>No <code className="font-mono text-[0.7rem]">.pub</code> file on disk either?</span>
              </summary>
              <div className="mt-3 ml-4 pl-3 border-l border-[var(--color-rule)] space-y-3 text-[0.7rem] text-[var(--color-ink-muted)] leading-relaxed">
                <div>
                  <p className="mb-1 font-mono text-[var(--color-ink)]">Sign without writing a file (bash / zsh):</p>
                  <code className="font-mono block text-[var(--color-ink)] break-all">
                    ssh-keygen -Y sign -n kini -f &lt;(ssh-add -L | head -1)
                  </code>
                </div>
                <div>
                  <p className="mb-1 font-mono text-[var(--color-ink)]">Or save once, reuse forever:</p>
                  <code className="font-mono block text-[var(--color-ink)] break-all">
                    ssh-add -L | head -1 &gt; ~/.ssh/id_yubi.pub
                  </code>
                </div>
                <p className="pt-1 italic">
                  Multiple identities in your agent? Filter with <code className="font-mono not-italic text-[var(--color-ink)]">grep cardno:XXX</code> or
                  by the key comment instead of <code className="font-mono not-italic text-[var(--color-ink)]">head -1</code>.
                </p>
              </div>
            </details>
          </div>

          <label className="block mt-8">
            <span className="eyebrow block mb-2">2. Paste the signature</span>
            <textarea
              value={signature}
              onChange={(e) => setSignature(e.target.value)}
              rows={5}
              placeholder="-----BEGIN SSH SIGNATURE-----&#10;…"
              disabled={step === 'done'}
              className="w-full px-4 py-3 bg-[var(--color-parchment)] border border-[var(--color-rule)] rounded-sm font-mono text-xs focus:outline-none focus:border-[var(--color-ink)] transition-colors disabled:opacity-60"
            />
          </label>

          <button
            onClick={verify}
            disabled={signature.trim().length < 20 || step === 'done'}
            className="btn-primary w-full mt-6 justify-center disabled:opacity-40 disabled:cursor-not-allowed"
          >
            {step === 'sign' ? (
              <>
                Verify signature
                <span aria-hidden>→</span>
              </>
            ) : (
              '✓ Verified'
            )}
          </button>
        </>
      )}
    </div>
  )
}
