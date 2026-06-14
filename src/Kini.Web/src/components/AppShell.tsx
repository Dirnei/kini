import { useEffect } from 'react'
import { Outlet, useNavigate } from 'react-router'
import { AppHeader } from './AppHeader'
import { useCurrentUser, type MeData } from '../lib/auth'

export type AppOutletContext = { me: MeData }

/**
 * Shell for the authenticated /app/* routes: enforces a session, renders
 * the shared header, and exposes the resolved current user to children via
 * React Router's Outlet context (`useOutletContext<AppOutletContext>()`).
 */
export function AppShell() {
  const { data: me, isLoading } = useCurrentUser()
  const navigate = useNavigate()

  useEffect(() => {
    if (!isLoading && me === null) navigate('/sign-in')
  }, [isLoading, me, navigate])

  if (isLoading || !me) {
    return (
      <div className="min-h-screen flex items-center justify-center font-mono text-sm text-[var(--color-ink-muted)]">
        resolving session…
      </div>
    )
  }

  const ctx: AppOutletContext = { me }
  return (
    <div className="min-h-screen flex flex-col">
      <AppHeader me={me} />
      <Outlet context={ctx} />
    </div>
  )
}
