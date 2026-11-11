import type { ApiError, HoldStatus } from '@/types'

export function formatError(error: unknown): string {
  if (!error) return 'An unexpected error occurred.'
  if (typeof error === 'string') return error
  const e = error as ApiError
  if (e.detail) return e.detail
  if (e.title) return e.title
  return 'An unexpected error occurred.'
}

export function formatValidationErrors(error: unknown): string {
  const e = error as ApiError
  if (!e.errors) return formatError(error)
  const messages = Object.values(e.errors).flat()
  return messages.join(' ')
}

export function formatTimeRemaining(expiresAt: string): string {
  const diff = new Date(expiresAt).getTime() - Date.now()
  if (diff <= 0) return 'Expired'
  const totalSeconds = Math.floor(diff / 1000)
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${minutes}m ${seconds.toString().padStart(2, '0')}s`
}

export function isHoldActive(status: HoldStatus, expiresAt: string): boolean {
  if (status !== 'HELD') return false
  return new Date(expiresAt).getTime() > Date.now()
}

export function statusLabel(status: HoldStatus, expiresAt: string): HoldStatus {
  if (status === 'HELD' && new Date(expiresAt).getTime() <= Date.now()) {
    return 'EXPIRED'
  }
  return status
}

export function statusColor(status: HoldStatus | 'EXPIRED'): string {
  switch (status) {
    case 'HELD': return 'text-amber-700 bg-amber-50 border-amber-200'
    case 'CONFIRMED': return 'text-green-700 bg-green-50 border-green-200'
    case 'EXPIRED': return 'text-gray-500 bg-gray-50 border-gray-200'
    case 'CANCELLED': return 'text-red-600 bg-red-50 border-red-200'
    default: return 'text-gray-600 bg-gray-50 border-gray-200'
  }
}

export function validateStudentId(id: string): string | null {
  if (!id) return 'Birth registration number is required.'
  if (!/^\d{17}$/.test(id)) return 'Must be exactly 17 digits.'
  return null
}
