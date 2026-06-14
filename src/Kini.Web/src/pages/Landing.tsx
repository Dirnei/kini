import { Link } from 'react-router'
import { Seal } from '../components/Seal'
import { Nav } from '../components/Nav'
import { Footer } from '../components/Footer'

export function Landing() {
  return (
    <>
      <Nav />
      <Hero />
      <TrustEssay />
      <Pillars />
      <HowItWorks />
      <HardwareStory />
      <Status />
      <Footer />
    </>
  )
}

/* ─────────────────────────────────────────────────────────────────
 *  HERO
 * ─────────────────────────────────────────────────────────────── */
function Hero() {
  return (
    <section className="relative max-w-[1400px] mx-auto px-8 md:px-16 lg:px-24 pt-16 pb-32 md:pt-24 md:pb-40">
      {/* Decorative seal floating top-right — visible but unobtrusive. */}
      <div className="absolute top-12 right-6 md:right-16 lg:right-24 text-[var(--color-oxblood-deep)] opacity-[0.18] drift pointer-events-none select-none">
        <Seal size={420} ariaLabel="" />
      </div>

      <div className="relative max-w-4xl">
        <p className="reveal eyebrow mb-6" style={{ animationDelay: '0ms' }}>
          Hardware-anchored key directory · Private alpha · MMXXVI
        </p>

        <h1
          className="reveal font-display text-[3rem] sm:text-[4rem] md:text-[5.5rem] lg:text-[6.5rem] leading-[0.95] tracking-tight"
          style={{
            fontVariationSettings: '"opsz" 144, "SOFT" 50',
            animationDelay: '120ms',
            fontWeight: 400,
          }}
        >
          Your keys are
          <span className="block italic text-[var(--color-oxblood)]" style={{ fontWeight: 400 }}>
            seals of identity.
          </span>
          <span className="block">Treat them like one.</span>
        </h1>

        <p
          className="reveal mt-10 max-w-2xl text-lg md:text-xl leading-relaxed text-[var(--color-ink-muted)]"
          style={{ animationDelay: '260ms' }}
        >
          Kini is a public-key directory for organizations — anchored to hardware tokens,
          served at <span className="font-mono text-[var(--color-ink)]">your own domain</span>,
          discovered by the tools your team already runs.
          WKD-compliant, SSH-compatible, with provenance and a lifecycle that actually propagates.
        </p>

        <div className="reveal mt-12 flex flex-wrap items-center gap-4" style={{ animationDelay: '400ms' }}>
          <Link to="/sign-in" className="btn-primary">
            Request access
            <span aria-hidden>→</span>
          </Link>
          <Link to="/sign-in" className="btn-ghost">
            Sign in with your key
          </Link>
        </div>

        <div className="reveal mt-20 flex items-center gap-8 text-sm text-[var(--color-ink-muted)]" style={{ animationDelay: '560ms' }}>
          <Snippet command="gpg --locate-keys alice@acme.tld" />
          <span className="hidden lg:inline font-mono text-xs opacity-60">↑ that's it. the spec already works.</span>
        </div>
      </div>
    </section>
  )
}

function Snippet({ command }: { command: string }) {
  return (
    <code className="inline-flex items-center gap-2 font-mono text-sm px-4 py-2.5 bg-[var(--color-ink)] text-[var(--color-parchment)] rounded-sm">
      <span className="text-[var(--color-gold)]">$</span>
      {command}
    </code>
  )
}

/* ─────────────────────────────────────────────────────────────────
 *  TRUST ESSAY
 * ─────────────────────────────────────────────────────────────── */
function TrustEssay() {
  return (
    <section className="max-w-[1400px] mx-auto px-8 md:px-16 lg:px-24 py-24 md:py-32">
      <div className="grid grid-cols-1 md:grid-cols-12 gap-12">
        <div className="md:col-span-3">
          <p className="eyebrow">I.</p>
          <h2 className="font-display text-4xl md:text-5xl leading-tight mt-4" style={{ fontVariationSettings: '"opsz" 96' }}>
            Public keys belong on the public web.
          </h2>
        </div>

        <div className="md:col-span-8 md:col-start-5">
          <p className="dropcap font-display text-xl md:text-[1.3rem] leading-[1.65] text-[var(--color-ink-muted)]" style={{ fontVariationSettings: '"opsz" 24' }}>
            For two decades we let GitHub host them by accident. <code className="font-mono text-base text-[var(--color-ink)]">/<wbr/>.keys</code> works.
            Until your contractor leaves, your CI logs into your prod boxes with a key
            nobody owns anymore, and the audit trail dead-ends at a personal account.
          </p>

          <div className="rule my-10" />

          <p className="font-display text-xl md:text-[1.3rem] leading-[1.65]" style={{ fontVariationSettings: '"opsz" 24' }}>
            Kini gives every organization the directory it should have always had —
            <em className="text-[var(--color-oxblood)]"> hosted at your own domain</em>,
            tied to identities your IdP already knows, with provenance,
            lifecycle, and revocation that actually arrives where it needs to.
          </p>
        </div>
      </div>
    </section>
  )
}

/* ─────────────────────────────────────────────────────────────────
 *  PILLARS
 * ─────────────────────────────────────────────────────────────── */
function Pillars() {
  const items = [
    {
      n: '·',
      title: 'WKD',
      tag: 'OpenPGP Web Key Directory',
      body: (
        <>
          <code className="font-mono text-sm">gpg --locate-keys</code> finds your team's keys
          at your domain. No paste, no instructions. The protocol your tools already speak.
        </>
      ),
    },
    {
      n: '·',
      title: '.keys / .gpg',
      tag: 'GitHub-compatible URL shape',
      body: (
        <>
          Ansible's <code className="font-mono text-sm">authorized_key</code> already understands
          this URL. Point it at <code className="font-mono text-sm">keys.acme.tld/alice.keys</code> —
          done.
        </>
      ),
    },
    {
      n: '·',
      title: 'Provenance',
      tag: 'Hardware-attested origins',
      body: (
        <>
          Every key carries proof of how it was born. Policies enforce
          <em> hardware-attested only</em> on the surfaces that need it. Audited on every fetch.
        </>
      ),
    },
  ]

  return (
    <section className="bg-[var(--color-ink)] text-[var(--color-parchment)] py-24 md:py-32">
      <div className="max-w-[1400px] mx-auto px-8 md:px-16 lg:px-24">
        <div className="max-w-3xl mb-16">
          <p className="eyebrow text-[var(--color-gold)]">II. What it actually does</p>
          <h2 className="font-display text-4xl md:text-5xl leading-tight mt-4" style={{ fontVariationSettings: '"opsz" 96' }}>
            Three endpoints. Standards-compliant. <span className="italic text-[var(--color-gold)]">No surprises.</span>
          </h2>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-px bg-[var(--color-ink-muted)]/30">
          {items.map((it) => (
            <article key={it.title} className="bg-[var(--color-ink)] p-8 md:p-10">
              <span className="font-display text-5xl text-[var(--color-gold)] block leading-none">{it.n}</span>
              <h3 className="font-display text-3xl mt-6" style={{ fontVariationSettings: '"opsz" 48' }}>{it.title}</h3>
              <p className="font-mono text-xs uppercase tracking-[0.18em] text-[var(--color-gold)]/80 mt-1">{it.tag}</p>
              <p className="mt-6 text-base leading-relaxed text-[var(--color-parchment)]/80">
                {it.body}
              </p>
            </article>
          ))}
        </div>
      </div>
    </section>
  )
}

/* ─────────────────────────────────────────────────────────────────
 *  HOW IT WORKS
 * ─────────────────────────────────────────────────────────────── */
function HowItWorks() {
  const steps = [
    {
      n: '01',
      title: 'Provision',
      body: (
        <>
          The Kini CLI generates a keypair, writes the private half to a hardware token
          (YubiKey, smartcard, TPM), and submits the public half — with its
          <em> attestation chain</em>, when the hardware supports it.
        </>
      ),
      code: 'kini provision --identity alice@acme.tld --yubikey',
    },
    {
      n: '02',
      title: 'Publish',
      body: (
        <>
          CNAME a domain to Kini. We provision TLS, serve the standards-compliant
          endpoints, and listen for changes. <em>Your domain. Your identity. Our plumbing.</em>
        </>
      ),
      code: 'keys.acme.tld   CNAME   edge.kini.example',
    },
    {
      n: '03',
      title: 'Access',
      body: (
        <>
          Your existing tools — <code className="font-mono text-base">gpg</code>,
          <code className="font-mono text-base"> ssh</code>,
          <code className="font-mono text-base"> ansible</code>,
          <code className="font-mono text-base"> git</code> — just resolve.
          Nothing to install on the consuming side.
        </>
      ),
      code: 'gpg --locate-keys alice@acme.tld',
    },
  ]

  return (
    <section id="how" className="max-w-[1400px] mx-auto px-8 md:px-16 lg:px-24 py-24 md:py-32">
      <div className="max-w-3xl mb-20">
        <p className="eyebrow">III. The path</p>
        <h2 className="font-display text-4xl md:text-5xl leading-tight mt-4" style={{ fontVariationSettings: '"opsz" 96' }}>
          From hardware to <span className="italic text-[var(--color-oxblood)]">"it just resolved"</span>.
        </h2>
      </div>

      <div className="space-y-20 md:space-y-28">
        {steps.map((s, i) => (
          <article key={s.n} className="grid grid-cols-1 md:grid-cols-12 gap-8 items-start">
            <div className="md:col-span-3 md:sticky md:top-10">
              <span className="step-numeral block">{s.n}</span>
              <div className="rule w-16 mt-4 mb-4" />
              <h3 className="font-display text-3xl md:text-4xl" style={{ fontVariationSettings: '"opsz" 64' }}>
                {s.title}
              </h3>
            </div>

            <div className="md:col-span-8 md:col-start-5">
              <p className="font-display text-xl md:text-[1.35rem] leading-[1.6] text-[var(--color-ink-muted)]" style={{ fontVariationSettings: '"opsz" 24' }}>
                {s.body}
              </p>

              <pre className="mt-8 font-mono text-sm bg-[var(--color-ink)] text-[var(--color-parchment)] rounded-sm p-5 overflow-x-auto leading-relaxed">
                <span className="text-[var(--color-gold)]">{i === 1 ? '#' : '$'} </span>
                {s.code}
              </pre>
            </div>
          </article>
        ))}
      </div>
    </section>
  )
}

/* ─────────────────────────────────────────────────────────────────
 *  HARDWARE STORY
 * ─────────────────────────────────────────────────────────────── */
function HardwareStory() {
  return (
    <section id="hardware" className="bg-[var(--color-parchment-deep)] py-24 md:py-32">
      <div className="max-w-[1400px] mx-auto px-8 md:px-16 lg:px-24 grid grid-cols-1 md:grid-cols-12 gap-12 items-center">
        <div className="md:col-span-7">
          <p className="eyebrow">IV. The seal</p>
          <h2 className="font-display text-4xl md:text-6xl leading-[1.02] mt-4" style={{ fontVariationSettings: '"opsz" 144' }}>
            Your YubiKey
            <span className="block italic text-[var(--color-oxblood)]">is the seal.</span>
          </h2>

          <p className="mt-8 text-lg leading-relaxed max-w-xl text-[var(--color-ink-muted)]">
            The same token that signs your commits and unlocks your servers
            authenticates you to Kini — natively, in the browser, through
            FIDO2 / WebAuthn. No passwords. No SMS. <em>Touch to sign in.</em>
          </p>

          <p className="mt-6 text-lg leading-relaxed max-w-xl text-[var(--color-ink-muted)]">
            Prefer the classics? Sign in with an SSH-key challenge instead —
            <code className="font-mono text-base text-[var(--color-ink)]"> ssh-keygen -Y sign</code> a
            nonce, paste the signature back. The hardware never leaves your laptop.
          </p>

          <div className="mt-10 flex flex-wrap items-center gap-4">
            <Link to="/sign-in" className="btn-primary">Try sign in</Link>
            <span className="font-mono text-xs text-[var(--color-ink-muted)] uppercase tracking-wider">No account required to look</span>
          </div>
        </div>

        <div className="md:col-span-5 flex justify-center">
          <div className="relative">
            <div className="absolute inset-0 bg-[var(--color-oxblood)]/8 blur-2xl rounded-full" aria-hidden />
            <div className="relative text-[var(--color-oxblood)] drift">
              <Seal size={340} ariaLabel="" />
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}

/* ─────────────────────────────────────────────────────────────────
 *  STATUS
 * ─────────────────────────────────────────────────────────────── */
function Status() {
  return (
    <section className="max-w-[1400px] mx-auto px-8 md:px-16 lg:px-24 py-28 md:py-40">
      <div className="text-center max-w-3xl mx-auto">
        <p className="eyebrow">V. Status</p>
        <h2 className="font-display text-5xl md:text-7xl leading-[1.02] mt-6" style={{ fontVariationSettings: '"opsz" 144' }}>
          In private alpha.
          <span className="block italic text-[var(--color-oxblood)]">Limited cohorts.</span>
        </h2>
        <p className="mt-8 text-lg text-[var(--color-ink-muted)] leading-relaxed">
          We're onboarding a handful of organizations whose threat model
          actually matches the product. If yours does, we'd like to hear from you.
        </p>
        <div className="mt-12 flex justify-center">
          <Link to="/sign-in" className="btn-primary text-base">
            Apply for access
            <span aria-hidden>→</span>
          </Link>
        </div>
      </div>
    </section>
  )
}
