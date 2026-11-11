import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Alert } from '@/components/ui/Alert'
import { Button } from '@/components/ui/Button'
import { StatusBadge } from '@/components/ui/Badge'
import { Countdown } from '@/components/ui/Countdown'

describe('Alert', () => {
  it('renders error message', () => {
    render(<Alert variant="error" message="Something went wrong" />)
    expect(screen.getByRole('alert')).toBeInTheDocument()
    expect(screen.getByText('Something went wrong')).toBeInTheDocument()
  })

  it('renders title when provided', () => {
    render(<Alert variant="info" title="Heads up" message="Info message" />)
    expect(screen.getByText('Heads up')).toBeInTheDocument()
  })
})

describe('Button', () => {
  it('renders children', () => {
    render(<Button>Click me</Button>)
    expect(screen.getByRole('button', { name: 'Click me' })).toBeInTheDocument()
  })

  it('is disabled when loading', () => {
    render(<Button loading>Click me</Button>)
    expect(screen.getByRole('button')).toBeDisabled()
  })

  it('calls onClick when clicked', async () => {
    const user = userEvent.setup()
    let clicked = false
    render(<Button onClick={() => { clicked = true }}>Click</Button>)
    await user.click(screen.getByRole('button'))
    expect(clicked).toBe(true)
  })

  it('does not call onClick when disabled', async () => {
    const user = userEvent.setup()
    let clicked = false
    render(<Button disabled onClick={() => { clicked = true }}>Click</Button>)
    await user.click(screen.getByRole('button'))
    expect(clicked).toBe(false)
  })
})

describe('StatusBadge', () => {
  it('shows HELD for active hold', () => {
    const future = new Date(Date.now() + 60_000).toISOString()
    render(<StatusBadge status="HELD" expiresAt={future} />)
    expect(screen.getByText('HELD')).toBeInTheDocument()
  })

  it('shows EXPIRED for past hold', () => {
    const past = new Date(Date.now() - 1000).toISOString()
    render(<StatusBadge status="HELD" expiresAt={past} />)
    expect(screen.getByText('EXPIRED')).toBeInTheDocument()
  })

  it('shows CONFIRMED', () => {
    render(<StatusBadge status="CONFIRMED" />)
    expect(screen.getByText('CONFIRMED')).toBeInTheDocument()
  })
})

describe('Countdown', () => {
  it('renders "Expired" for past date', () => {
    const past = new Date(Date.now() - 1000).toISOString()
    render(<Countdown expiresAt={past} />)
    expect(screen.getByText('Expired')).toBeInTheDocument()
  })

  it('renders time remaining for future date', () => {
    const future = new Date(Date.now() + 120_000).toISOString()
    render(<Countdown expiresAt={future} />)
    // Should show something like "2m 00s"
    expect(screen.getByText(/\dm \d{2}s/)).toBeInTheDocument()
  })
})
