import { useMemo, useState } from 'react'
import { useOutletContext } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { Avatar } from '../components/Avatar'
import { getToken } from '../lib/api'
import type { AppOutletContext } from '../components/AppShell'

type AuditEvent = {
  id: string
  orgId: string
  action: string
  actor: { type: string; identityId: string | null; email: string | null }
  target: { type: string; id: string | null; name: string | null } | null
  occurredAt: string
  detail: Record<string, string> | null
}

type AuditPage = { items: AuditEvent[]; nextCursor: string | null }

type FilterKey = 'all' | 'auth' | 'identity' | 'key' | 'token' | 'org'
const FILTERS: ReadonlyArray<{ key: FilterKey; label: string; matches: (a: string) => boolean }> = [
  { key: 'all',      label: 'All',         matches: () => true },
  { key: 'auth',     label: 'Sign-ins',    matches: a => a.startsWith('signin.') || a.startsWith('session.') },
  { key: 'identity', label: 'Identities',  matches: a => a.startsWith('identity.') || a.startsWith('credential.') },
  { key: 'key',      label: 'Keys',        matches: a => a.startsWith('key.') },
  { key: 'token',    label: 'API tokens',  matches: a => a.startsWith('apitoken.') },
  { key: 'org',      label: 'Org',         matches: a => a.startsWith('org.') },
]

export function Audit() {
  // The page is org-scoped; me.identity.orgId is what filters the query.
  // Eslint-style unused destructure is the convention for "we need the context guard but not the value here."
  useOutletContext<AppOutletContext>()
  const [filter, setFilter] = useState<FilterKey>('all')

  const { data, isLoading, error } = useQuery<AuditPage>({
    queryKey: ['audit'],
    queryFn: async () => {
      const res = await fetch('/v1/audit?limit=100', {
        headers: { Authorization: `Bearer ${getToken()}` },
      })
      if (!res.ok) throw new Error(`audit ${res.status}`)
      return res.json()
    },
    staleTime: 15_000,
  })

  const filtered = useMemo(() => {
    if (!data) return []
    const f = FILTERS.find(x => x.key === filter)!
    return data.items.filter(e => f.matches(e.action))
  }, [data, filter])

  return (
    <main className="flex-1 max-w-[1400px] w-full mx-auto px-6 md:px-10 py-12">
      <p className="eyebrow reveal">Audit</p>
      <h1 className="reveal font-display text-5xl md:text-6xl mt-3 leading-[1.02]" style={{ fontVariationSettings: '"opsz" 96', animationDelay: '80ms' }}>
        Every state change, <span className="italic text-[var(--color-oxblood)]">stamped</span>.
      </h1>
      <p className="reveal mt-4 text-lg text-[var(--color-ink-muted)] max-w-2xl" style={{ animationDelay: '160ms' }}>
        Append-only record of who did what, when. Scoped to your organization. Latest 100 events; older entries paginate (coming soon).
      </p>

      <div className="reveal mt-10 flex flex-wrap items-center gap-1 border border-[var(--color-rule)] rounded-sm overflow-hidden self-start" style={{ animationDelay: '240ms', display: 'inline-flex' }}>
        {FILTERS.map(f => (
          <button key={f.key}
            onClick={() => setFilter(f.key)}
            className={`px-4 py-2 text-xs font-mono uppercase tracking-[0.18em] transition-colors ${
              filter === f.key
                ? 'bg-[var(--color-ink)] text-[var(--color-parchment)]'
                : 'text-[var(--color-ink-muted)] hover:text-[var(--color-ink)] hover:bg-[var(--color-parchment-deep)]'
            }`}>
            {f.label}
          </button>
        ))}
      </div>

      {isLoading && <div className="mt-10 font-mono text-xs text-[var(--color-ink-muted)]">loading…</div>}
      {error && <div className="mt-10 text-xs text-[var(--color-oxblood-deep)]">Failed to load audit log: {(error as Error).message}</div>}

      {!isLoading && filtered.length === 0 && (
        <div className="mt-10 text-sm text-[var(--color-ink-muted)] italic">
          {filter === 'all' ? 'No events yet.' : 'No events match this filter.'}
        </div>
      )}

      {filtered.length > 0 && (
        <ol className="reveal mt-10 border border-[var(--color-rule)] rounded-sm divide-y divide-[var(--color-rule)] bg-[var(--color-parchment)]" style={{ animationDelay: '320ms' }}>
          {filtered.map(e => <Row key={e.id} event={e} />)}
        </ol>
      )}
    </main>
  )
}

function Row({ event }: { event: AuditEvent }) {
  return (
    <li className="px-6 py-4 flex items-start gap-5">
      <div className="shrink-0">
        {event.actor.email
          ? <Avatar email={event.actor.email} size={32} />
          : <div className="w-8 h-8 rounded-full bg-[var(--color-rule)] flex items-center justify-center font-mono text-[10px] uppercase text-[var(--color-ink-muted)]">
              {event.actor.type[0] ?? '·'}
            </div>}
      </div>
      <div className="min-w-0 flex-1 flex flex-col md:flex-row md:items-baseline md:gap-4">
        <div className="min-w-0 flex-1">
          <p className="leading-snug">
            <ActionLabel action={event.action} />
            {event.target?.name && (
              <span className="ml-2 font-mono text-xs text-[var(--color-ink)] break-all">{event.target.name}</span>
            )}
          </p>
          <p className="mt-1 font-mono text-[11px] text-[var(--color-ink-muted)]">
            {event.actor.email ?? `[${event.actor.type}]`}
            {event.target?.type && <> · target: <span className="text-[var(--color-ink)]">{event.target.type}</span></>}
          </p>
          {event.detail && Object.keys(event.detail).length > 0 && (
            <DetailRow detail={event.detail} />
          )}
        </div>
        <time className="shrink-0 font-mono text-[11px] text-[var(--color-ink-muted)] tabular-nums" title={event.occurredAt}>
          {formatTime(event.occurredAt)}
        </time>
      </div>
    </li>
  )
}

function ActionLabel({ action }: { action: string }) {
  const tone = toneFor(action)
  return (
    <span className={`font-mono text-[11px] uppercase tracking-[0.18em] ${tone}`}>
      {action}
    </span>
  )
}

function toneFor(action: string): string {
  if (action.endsWith('.failed') || action.endsWith('.revoked') || action.endsWith('.deleted')) return 'text-[var(--color-oxblood)]'
  if (action.endsWith('.succeeded') || action.endsWith('.created') || action.endsWith('.published')) return 'text-[var(--color-gold-deep)]'
  return 'text-[var(--color-ink-muted)]'
}

function DetailRow({ detail }: { detail: Record<string, string> }) {
  const [open, setOpen] = useState(false)
  const keys = Object.keys(detail)
  return (
    <details className="mt-2" onToggle={(e) => setOpen((e.currentTarget as HTMLDetailsElement).open)}>
      <summary className="cursor-pointer list-none flex items-center gap-2 text-[11px] font-mono text-[var(--color-ink-muted)] hover:text-[var(--color-ink)]">
        <span className={`transition-transform ${open ? 'rotate-90' : ''} text-[var(--color-gold)]`}>›</span>
        <span>{keys.length} detail{keys.length === 1 ? '' : 's'}</span>
      </summary>
      <dl className="mt-2 ml-4 pl-3 border-l border-[var(--color-rule)] text-[11px] font-mono space-y-1">
        {keys.map(k => (
          <div key={k} className="flex gap-2">
            <dt className="text-[var(--color-ink-muted)] min-w-[10ch]">{k}:</dt>
            <dd className="text-[var(--color-ink)] break-all">{detail[k]}</dd>
          </div>
        ))}
      </dl>
    </details>
  )
}

function formatTime(iso: string): string {
  const date = new Date(iso)
  const now = new Date()
  const ageMs = now.getTime() - date.getTime()
  const sec = Math.floor(ageMs / 1000)
  if (sec < 60)  return `${sec}s ago`
  const min = Math.floor(sec / 60)
  if (min < 60)  return `${min}m ago`
  const hr = Math.floor(min / 60)
  if (hr < 24)   return `${hr}h ago`
  const day = Math.floor(hr / 24)
  if (day < 7)   return `${day}d ago`
  return date.toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' })
}
