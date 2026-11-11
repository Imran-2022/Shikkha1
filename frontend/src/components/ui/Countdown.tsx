import { useState, useEffect } from 'react'
import { formatTimeRemaining } from '@/utils'

interface CountdownProps {
  expiresAt: string
  onExpire?: () => void
}

export function Countdown({ expiresAt, onExpire }: CountdownProps) {
  const [remaining, setRemaining] = useState(() => formatTimeRemaining(expiresAt))
  const [expired, setExpired] = useState(() => new Date(expiresAt).getTime() <= Date.now())

  useEffect(() => {
    if (expired) return

    const tick = () => {
      const diff = new Date(expiresAt).getTime() - Date.now()
      if (diff <= 0) {
        setRemaining('Expired')
        setExpired(true)
        onExpire?.()
      } else {
        setRemaining(formatTimeRemaining(expiresAt))
      }
    }

    tick()
    const id = setInterval(tick, 1000)
    return () => clearInterval(id)
  }, [expiresAt, expired, onExpire])

  if (expired) {
    return <span className="text-sm font-medium text-red-600">Expired</span>
  }

  return (
    <span className="font-mono text-sm font-semibold text-amber-700">
      {remaining}
    </span>
  )
}
