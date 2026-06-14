/**
 * Kini wax-seal logomark. SVG drawn from scratch — concentric rings, a band of
 * Latin micro-typography (directory of keys), a Fraunces italic K at center,
 * a small bottom flourish. Inherits color via currentColor.
 */

type SealProps = {
  size?: number
  className?: string
  ariaLabel?: string
}

export function Seal({ size = 280, className = '', ariaLabel = 'Kini seal' }: SealProps) {
  const tickCount = 60
  const ticks = Array.from({ length: tickCount }, (_, i) => i)

  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 280 280"
      xmlns="http://www.w3.org/2000/svg"
      role="img"
      aria-label={ariaLabel}
      className={className}
    >
      <defs>
        {/* Path for the circular text along the inner ring. */}
        <path
          id="seal-text-path"
          d="M 140,140 m -98,0 a 98,98 0 1,1 196,0 a 98,98 0 1,1 -196,0"
          fill="none"
        />
      </defs>

      {/* Outermost ring — thin hairline. */}
      <circle cx="140" cy="140" r="135" fill="none" stroke="currentColor" strokeWidth="0.6" opacity="0.55" />

      {/* Bold inner ring — the body of the seal. */}
      <circle cx="140" cy="140" r="124" fill="none" stroke="currentColor" strokeWidth="1.8" />

      {/* Decorative tick marks between rings. */}
      <g>
        {ticks.map((i) => {
          const angle = (i / tickCount) * Math.PI * 2 - Math.PI / 2
          const r1 = 128
          const r2 = i % 5 === 0 ? 132 : 130
          const x1 = 140 + Math.cos(angle) * r1
          const y1 = 140 + Math.sin(angle) * r1
          const x2 = 140 + Math.cos(angle) * r2
          const y2 = 140 + Math.sin(angle) * r2
          return (
            <line
              key={i}
              x1={x1}
              y1={y1}
              x2={x2}
              y2={y2}
              stroke="currentColor"
              strokeWidth={i % 5 === 0 ? 1 : 0.5}
              opacity={i % 5 === 0 ? 0.85 : 0.4}
            />
          )
        })}
      </g>

      {/* Inner hairline circle to frame the band of text. */}
      <circle cx="140" cy="140" r="114" fill="none" stroke="currentColor" strokeWidth="0.4" opacity="0.55" />
      <circle cx="140" cy="140" r="82" fill="none" stroke="currentColor" strokeWidth="0.4" opacity="0.55" />

      {/* The circular text — Latin for "directory of keys". */}
      <text
        fontFamily="var(--font-mono, JetBrains Mono, monospace)"
        fontSize="8.5"
        letterSpacing="6"
        fill="currentColor"
        opacity="0.85"
      >
        <textPath href="#seal-text-path" startOffset="0%">
          {'· DIRECTORIVM · CLAVIVM · KINI · MMXXVI · DIRECTORIVM · CLAVIVM · KINI · MMXXVI '}
        </textPath>
      </text>

      {/* Center K — Fraunces italic, the heart of the mark. */}
      <text
        x="140"
        y="140"
        fontFamily="var(--font-display, Fraunces, serif)"
        fontWeight="500"
        fontStyle="italic"
        fontSize="96"
        textAnchor="middle"
        dominantBaseline="central"
        fill="currentColor"
        style={{ fontVariationSettings: '"opsz" 144' }}
      >
        K
      </text>

      {/* Bottom flourish — a small chevron of three dots. */}
      <g transform="translate(140 218)" fill="currentColor" opacity="0.7">
        <circle cx="-10" cy="0" r="1.5" />
        <circle cx="0" cy="3" r="2" />
        <circle cx="10" cy="0" r="1.5" />
      </g>

      {/* Small star markers at compass points to break up the band. */}
      {[0, 90, 180, 270].map((deg) => {
        const angle = (deg * Math.PI) / 180 - Math.PI / 2
        const x = 140 + Math.cos(angle) * 98
        const y = 140 + Math.sin(angle) * 98
        return (
          <g key={deg} transform={`translate(${x} ${y})`}>
            <path
              d="M 0,-3 L 0.8,-0.8 L 3,0 L 0.8,0.8 L 0,3 L -0.8,0.8 L -3,0 L -0.8,-0.8 Z"
              fill="currentColor"
              opacity="0.7"
            />
          </g>
        )
      })}
    </svg>
  )
}
