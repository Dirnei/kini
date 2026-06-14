import { Link, NavLink } from 'react-router'
import { Seal } from './Seal'
import { Avatar } from './Avatar'
import { useSignOut, type MeData } from '../lib/auth'

const NAV_ITEMS: ReadonlyArray<{ to: string; label: string; end?: boolean; soon?: boolean }> = [
  { to: '/app',            label: 'Console', end: true },
  { to: '/app/identities', label: 'Identities' },
  { to: '/app/keys',       label: 'Keys' },
  { to: '/app/tokens',     label: 'Tokens' },
  { to: '#', label: 'Domains', soon: true },
  { to: '#', label: 'Audit',   soon: true },
]

const activeCls = 'text-[var(--color-ink)] underline underline-offset-8 decoration-[var(--color-oxblood)] decoration-2'
const inactiveCls = 'text-[var(--color-ink-muted)] hover:text-[var(--color-ink)] transition-colors'
const disabledCls = 'text-[var(--color-ink-muted)]/40 cursor-not-allowed'

export function AppHeader({ me }: { me: MeData }) {
  const signOut = useSignOut()
  return (
    <header className="border-b border-[var(--color-rule)] px-6 md:px-10 py-5 flex items-center justify-between">
      <Link to="/" className="flex items-center gap-3 group">
        <span className="text-[var(--color-ink)] group-hover:text-[var(--color-oxblood)] transition-colors">
          <Seal size={28} />
        </span>
        <span className="font-display text-lg tracking-tight" style={{ fontVariationSettings: '"opsz" 24' }}>Kini</span>
      </Link>

      <nav className="hidden md:flex items-center gap-7 text-sm">
        {NAV_ITEMS.map((item) =>
          item.soon ? (
            <span key={item.label} title="Coming soon" className={disabledCls}>{item.label}</span>
          ) : (
            <NavLink
              key={item.label}
              to={item.to}
              end={item.end}
              className={({ isActive }) => (isActive ? activeCls : inactiveCls)}
            >
              {item.label}
            </NavLink>
          )
        )}
      </nav>

      <div className="flex items-center gap-4">
        <span className="font-mono text-xs text-[var(--color-ink-muted)] hidden md:inline">{me.identity.email}</span>
        <button onClick={signOut} className="text-xs text-[var(--color-ink-muted)] hover:text-[var(--color-oxblood)] transition-colors">Sign out</button>
        <Avatar email={me.identity.email} size={32} fallbackLabel={me.identity.displayName ?? undefined} />
      </div>
    </header>
  )
}
