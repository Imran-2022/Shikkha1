import { describe, it, expect, vi, afterEach } from 'vitest'
import {
  formatTimeRemaining,
  isHoldActive,
  statusLabel,
  validateStudentId,
  formatError,
} from '@/utils'

describe('validateStudentId', () => {
  it('returns null for valid 17-digit ID', () => {
    expect(validateStudentId('12345678901234567')).toBeNull()
  })

  it('returns error for empty string', () => {
    expect(validateStudentId('')).not.toBeNull()
  })

  it('returns error for 16 digits', () => {
    expect(validateStudentId('1234567890123456')).not.toBeNull()
  })

  it('returns error for 18 digits', () => {
    expect(validateStudentId('123456789012345678')).not.toBeNull()
  })

  it('returns error for letters', () => {
    expect(validateStudentId('1234567890123456a')).not.toBeNull()
  })
})

describe('formatTimeRemaining', () => {
  afterEach(() => vi.useRealTimers())

  it('returns "Expired" for past date', () => {
    const past = new Date(Date.now() - 1000).toISOString()
    expect(formatTimeRemaining(past)).toBe('Expired')
  })

  it('formats minutes and seconds correctly', () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2024-01-01T00:00:00Z'))
    const future = new Date('2024-01-01T00:05:30Z').toISOString()
    expect(formatTimeRemaining(future)).toBe('5m 30s')
  })
})

describe('isHoldActive', () => {
  it('returns true for HELD status and future expiry', () => {
    const future = new Date(Date.now() + 60_000).toISOString()
    expect(isHoldActive('HELD', future)).toBe(true)
  })

  it('returns false for HELD with past expiry', () => {
    const past = new Date(Date.now() - 1000).toISOString()
    expect(isHoldActive('HELD', past)).toBe(false)
  })

  it('returns false for CONFIRMED status', () => {
    const future = new Date(Date.now() + 60_000).toISOString()
    expect(isHoldActive('CONFIRMED', future)).toBe(false)
  })
})

describe('statusLabel', () => {
  it('returns EXPIRED for HELD with past expiry', () => {
    const past = new Date(Date.now() - 1000).toISOString()
    expect(statusLabel('HELD', past)).toBe('EXPIRED')
  })

  it('returns HELD for HELD with future expiry', () => {
    const future = new Date(Date.now() + 60_000).toISOString()
    expect(statusLabel('HELD', future)).toBe('HELD')
  })

  it('passes through CONFIRMED unchanged', () => {
    const future = new Date(Date.now() + 60_000).toISOString()
    expect(statusLabel('CONFIRMED', future)).toBe('CONFIRMED')
  })
})

describe('formatError', () => {
  it('extracts detail from ApiError', () => {
    const err = { title: 'T', detail: 'No seats available', status: 409 }
    expect(formatError(err)).toBe('No seats available')
  })

  it('falls back to title when no detail', () => {
    const err = { title: 'Not Found', status: 404 }
    expect(formatError(err)).toBe('Not Found')
  })

  it('handles plain string', () => {
    expect(formatError('oops')).toBe('oops')
  })

  it('handles null/undefined', () => {
    expect(formatError(null)).toBe('An unexpected error occurred.')
    expect(formatError(undefined)).toBe('An unexpected error occurred.')
  })
})
