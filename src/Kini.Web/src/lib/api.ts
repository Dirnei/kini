import createClient, { type Middleware } from 'openapi-fetch'
import type { paths } from './api-types'

const TOKEN_STORAGE_KEY = 'kini.token'

export function setToken(token: string | null): void {
  if (token) localStorage.setItem(TOKEN_STORAGE_KEY, token)
  else localStorage.removeItem(TOKEN_STORAGE_KEY)
}

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_STORAGE_KEY)
}

// Attach the bearer token automatically to every request that has one.
const bearerMiddleware: Middleware = {
  async onRequest({ request }) {
    const token = getToken()
    if (token) request.headers.set('Authorization', `Bearer ${token}`)
    return request
  },
  async onResponse({ response }) {
    // 401 means our token is dead. Clear it so the UI re-prompts.
    if (response.status === 401) setToken(null)
    return response
  },
}

export const api = createClient<paths>({ baseUrl: '' })
api.use(bearerMiddleware)
