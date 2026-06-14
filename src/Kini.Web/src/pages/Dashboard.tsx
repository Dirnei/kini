import { Link } from 'react-router'
import { Seal } from '../components/Seal'

export function Dashboard() {
  return (
    <div className="min-h-screen flex flex-col">
      <header className="border-b border-[var(--color-rule)] px-6 md:px-10 py-5 flex items-center justify-between">
        <Link to="/" className="flex items-center gap-3 group">
          <span className="text-[var(--color-ink)] group-hover:text-[var(--color-oxblood)] transition-colors">
            <Seal size={28} />
          </span>
          <span className="font-display text-lg tracking-tight" style={{ fontVariationSettings: '"opsz" 24' }}>
            Kini
          </span>
        </Link>

        <nav className="hidden md:flex items-center gap-7 text-sm">
          {['Identities', 'Keys', 'Domains', 'Tokens', 'Audit'].map((label) => (
            <a key={label} href="#" className="text-[var(--color-ink-muted)] hover:text-[var(--color-ink)] transition-colors">
              {label}
            </a>
          ))}
        </nav>

        <div className="flex items-center gap-3">
          <span className="font-mono text-xs text-[var(--color-ink-muted)] hidden md:inline">alice@acme.tld</span>
          <span className="w-8 h-8 rounded-full bg-[var(--color-oxblood)] text-[var(--color-parchment)] flex items-center justify-center font-display text-sm italic">A</span>
        </div>
      </header>

      <main className="flex-1 max-w-[1400px] w-full mx-auto px-6 md:px-10 py-12">
        <p className="eyebrow reveal">Console · Acme, Inc.</p>
        <h1 className="reveal font-display text-5xl md:text-6xl mt-3 leading-[1.02]" style={{ fontVariationSettings: '"opsz" 96', animationDelay: '80ms' }}>
          Welcome back, <span className="italic text-[var(--color-oxblood)]">Alice</span>.
        </h1>
        <p className="reveal mt-4 text-lg text-[var(--color-ink-muted)] max-w-2xl" style={{ animationDelay: '160ms' }}>
          Your organization's directory at a glance. This is a scaffold — the real surface for
          identities, keys, domain claims, tokens, and audit will grow into it.
        </p>

        <div className="reveal mt-16 grid grid-cols-1 md:grid-cols-4 gap-px bg-[var(--color-rule)]" style={{ animationDelay: '260ms' }}>
          {[
            { label: 'Identities', value: '47', sub: '+3 this week' },
            { label: 'Keys', value: '112', sub: '94 hardware-attested' },
            { label: 'Domains', value: '2', sub: 'acme.tld, ops.acme.tld' },
            { label: 'Pending', value: '1', sub: 'awaiting attestation' },
          ].map((m) => (
            <article key={m.label} className="bg-[var(--color-parchment)] p-7">
              <p className="eyebrow">{m.label}</p>
              <p className="font-display text-5xl mt-3" style={{ fontVariationSettings: '"opsz" 96' }}>{m.value}</p>
              <p className="text-xs text-[var(--color-ink-muted)] mt-2 font-mono">{m.sub}</p>
            </article>
          ))}
        </div>

        <div className="reveal mt-20 border border-[var(--color-rule)] p-10 md:p-14 rounded-sm" style={{ animationDelay: '360ms' }}>
          <p className="eyebrow">Next</p>
          <h2 className="font-display text-3xl md:text-4xl mt-4 leading-snug max-w-2xl" style={{ fontVariationSettings: '"opsz" 64' }}>
            The real management UI lives here — <span className="italic text-[var(--color-oxblood)]">one vertical slice at a time</span>.
          </h2>
          <p className="mt-4 text-[var(--color-ink-muted)] max-w-2xl">
            Identities, Keys, Domain Claims, Tokens, Audit. Each rendered from the same OpenAPI
            spec the backend honors, via the generated TypeScript client. No drift.
          </p>
          <div className="mt-8">
            <Link to="/" className="btn-ghost">Back to landing</Link>
          </div>
        </div>
      </main>
    </div>
  )
}
