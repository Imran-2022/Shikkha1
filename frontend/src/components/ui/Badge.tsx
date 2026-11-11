import clsx from 'clsx'
import type { HoldStatus } from '@/types'
import { statusColor } from '@/utils'

interface BadgeProps {
  status: HoldStatus | string
  expiresAt?: string
}

export function StatusBadge({ status, expiresAt }: BadgeProps) {
  const resolved: HoldStatus =
    status === 'HELD' && expiresAt && new Date(expiresAt).getTime() <= Date.now()
      ? 'EXPIRED'
      : (status as HoldStatus)

  return (
    <span
      className={clsx(
        'inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold',
        statusColor(resolved)
      )}
    >
      {resolved}
    </span>
  )
}
