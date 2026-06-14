import { useEffect, useState } from 'react'

type Props = {
  email: string
  size?: number          // px (CSS); actual image fetched at 2× for HiDPI
  fallbackLabel?: string // first char shown until image resolves / if it 404s
  className?: string
}

/**
 * Gravatar-backed avatar. Computes the SHA-256 of the lowercased email and
 * resolves https://www.gravatar.com/avatar/<sha256>. If the user has no
 * Gravatar, the `d=identicon` fallback paints a unique geometric mark.
 *
 * Privacy note: rendering this sends a hash of the user's email to Gravatar
 * (Automattic). Pre-image attacks against random emails are infeasible, but
 * a dictionary attack against a known target email succeeds trivially.
 * If that matters for your customers, expose an org-level opt-out later.
 */
export function Avatar({ email, size = 32, fallbackLabel, className = '' }: Props) {
  const [hash, setHash] = useState<string | null>(null)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    let cancelled = false
    sha256Hex(email.trim().toLowerCase()).then((h) => {
      if (!cancelled) setHash(h)
    })
    return () => {
      cancelled = true
    }
  }, [email])

  const initial = ((fallbackLabel ?? email)[0] ?? '?').toUpperCase()
  const sharedStyle = { width: size, height: size }

  if (!hash || failed) {
    return (
      <span
        className={`rounded-full bg-[var(--color-oxblood)] text-[var(--color-parchment)] flex items-center justify-center font-display italic select-none ${className}`}
        style={{ ...sharedStyle, fontSize: size * 0.45 }}
        aria-label={initial}
      >
        {initial}
      </span>
    )
  }

  const url = `https://www.gravatar.com/avatar/${hash}?s=${size * 2}&d=identicon`
  return (
    <img
      src={url}
      alt={initial}
      width={size}
      height={size}
      className={`rounded-full ${className}`}
      style={sharedStyle}
      loading="lazy"
      referrerPolicy="no-referrer"
      onError={() => setFailed(true)}
    />
  )
}

async function sha256Hex(text: string): Promise<string> {
  const data = new TextEncoder().encode(text)
  const buf = await crypto.subtle.digest('SHA-256', data)
  return Array.from(new Uint8Array(buf))
    .map((b) => b.toString(16).padStart(2, '0'))
    .join('')
}
