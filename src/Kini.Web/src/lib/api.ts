import createClient from 'openapi-fetch'
import type { paths } from './api-types'

// Single typed client used everywhere. baseUrl is '' so requests are relative,
// which lets Vite's dev proxy and Kestrel's prod hosting both serve us.
export const api = createClient<paths>({ baseUrl: '' })
