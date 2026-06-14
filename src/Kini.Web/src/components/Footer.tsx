import { Link } from 'react-router'
import { Seal } from './Seal'

export function Footer() {
  return (
    <footer className="border-t border-[var(--color-rule)] mt-32">
      <div className="max-w-[1400px] mx-auto px-8 md:px-16 lg:px-24 py-20 grid grid-cols-1 md:grid-cols-12 gap-10">
        <div className="md:col-span-5">
          <div className="flex items-center gap-3 mb-6">
            <span className="text-[var(--color-ink)]"><Seal size={44} /></span>
            <span className="font-display text-3xl" style={{ fontVariationSettings: '"opsz" 48' }}>Kini</span>
          </div>
          <p className="font-display italic text-lg text-[var(--color-ink-muted)] max-w-sm leading-snug">
            A directory for the keys that vouch for you.
          </p>
          <p className="eyebrow mt-8">Bavarian for "the king"</p>
        </div>

        <div className="md:col-span-2">
          <h4 className="eyebrow mb-4">Product</h4>
          <ul className="space-y-2 text-sm">
            <li><a className="hover:text-[var(--color-oxblood)] transition-colors" href="/#wkd">WKD</a></li>
            <li><a className="hover:text-[var(--color-oxblood)] transition-colors" href="/#keys">SSH .keys</a></li>
            <li><a className="hover:text-[var(--color-oxblood)] transition-colors" href="/#provenance">Provenance</a></li>
          </ul>
        </div>

        <div className="md:col-span-2">
          <h4 className="eyebrow mb-4">Docs</h4>
          <ul className="space-y-2 text-sm">
            <li><a className="hover:text-[var(--color-oxblood)] transition-colors" href="http://localhost:8080/docs/api.html">API</a></li>
            <li><a className="hover:text-[var(--color-oxblood)] transition-colors" href="http://localhost:8080/docs/well-known.html">Well-known</a></li>
          </ul>
        </div>

        <div className="md:col-span-3">
          <h4 className="eyebrow mb-4">Get in</h4>
          <ul className="space-y-2 text-sm">
            <li><Link to="/sign-in" className="hover:text-[var(--color-oxblood)] transition-colors">Sign in</Link></li>
            <li><Link to="/sign-in" className="hover:text-[var(--color-oxblood)] transition-colors">Request alpha access</Link></li>
          </ul>
        </div>
      </div>

      <div className="border-t border-[var(--color-rule)]">
        <div className="max-w-[1400px] mx-auto px-8 md:px-16 lg:px-24 py-6 flex items-center justify-between text-xs text-[var(--color-ink-muted)]">
          <span>© MMXXVI Kini</span>
          <span className="font-mono">made by hand, signed in oxblood</span>
        </div>
      </div>
    </footer>
  )
}
