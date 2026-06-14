import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router'
import { api, getToken, setToken } from './api'

export type MeData = {
  identity: {
    id: string
    username: string
    email: string
    displayName: string | null
    orgId: string
  }
  organization: { id: string; name: string; primaryDomain: string | null }
  session: { id: string; expiresAt: string }
}

export function useCurrentUser() {
  return useQuery<MeData | null>({
    queryKey: ['auth', 'me'],
    queryFn: async () => {
      if (!getToken()) return null
      const res = await fetch('/v1/auth/me', {
        headers: { Authorization: `Bearer ${getToken()}` },
      })
      if (res.status === 401) {
        setToken(null)
        return null
      }
      if (!res.ok) throw new Error(`auth/me ${res.status}`)
      return (await res.json()) as MeData
    },
    staleTime: 60_000,
    retry: false,
  })
}

export function useSignOut() {
  const navigate = useNavigate()
  const qc = useQueryClient()
  return async () => {
    try {
      await api.POST('/v1/auth/sign-out')
    } catch {
      /* ignore network failures on sign-out */
    }
    setToken(null)
    qc.removeQueries({ queryKey: ['auth'] })
    navigate('/sign-in')
  }
}
