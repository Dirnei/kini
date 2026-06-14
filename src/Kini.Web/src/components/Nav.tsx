import { Link, NavLink } from 'react-router'
import { Seal } from './Seal'

export function Nav() {
  return (
    <nav className="reveal flex items-center justify-between py-6 px-8 md:px-16 lg:px-24 max-w-[1400px] mx-auto">
      <Link to="/" className="flex items-center gap-3 group">
        <span className="text-[var(--color-ink)] transition-colors group-hover:text-[var(--color-oxblood)]">
          <Seal size={36} />
        </span>
        <span className="font-display text-2xl tracking-tight" style={{ fontVariationSettings: '"opsz" 48' }}>
          Kini
        </span>
      </Link>

      <ul className="hidden md:flex items-center gap-10 text-sm">
        {[
          { to: '/', label: 'Product' },
          { to: '/#how', label: 'How it works' },
          { to: '/#hardware', label: 'Hardware' },
        ].map((item) => (
          <li key={item.label}>
            <a
              href={item.to}
              className="text-[var(--color-ink-muted)] hover:text-[var(--color-ink)] transition-colors"
            >
              {item.label}
            </a>
          </li>
        ))}
      </ul>

      <div className="flex items-center gap-3">
        <NavLink to="/sign-in" className="text-sm text-[var(--color-ink-muted)] hover:text-[var(--color-ink)] transition-colors px-3 py-2">
          Sign in
        </NavLink>
        <Link to="/sign-in" className="btn-primary text-sm">
          Request access
        </Link>
      </div>
    </nav>
  )
}
