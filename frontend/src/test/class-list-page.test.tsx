import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ClassListPage } from '@/pages/ClassListPage'
import type { PlaceHoldResponse, SchoolClass, StudentHoldResponse } from '@/types'

const mocks = vi.hoisted(() => ({
  placeHold: vi.fn(),
  useClasses: vi.fn(),
  usePlaceHold: vi.fn(),
  useStudentHolds: vi.fn(),
  useStudentId: vi.fn(),
}))

vi.mock('@/hooks/useClasses', () => ({
  useClasses: mocks.useClasses,
  usePlaceHold: mocks.usePlaceHold,
}))

vi.mock('@/hooks/useHolds', () => ({
  useStudentHolds: mocks.useStudentHolds,
}))

vi.mock('@/store/studentStore', () => ({
  useStudentId: mocks.useStudentId,
}))

vi.mock('@/pages/components/HoldModal', () => ({
  HoldModal: ({ hold }: { hold: PlaceHoldResponse }) => <div>{hold.holdId}</div>,
}))

const schoolClass: SchoolClass = {
  id: 'class-1',
  name: 'Class 6 - Morning',
  totalSeats: 10,
  heldSeats: 0,
  confirmedSeats: 0,
  availableSeats: 10,
}

const studentId = '12345678901234567'

describe('ClassListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mocks.useClasses.mockReturnValue({
      data: [schoolClass],
      isLoading: false,
      error: null,
    })
    mocks.useStudentId.mockReturnValue({ studentId, isValid: true })
    mocks.useStudentHolds.mockReturnValue({
      data: [],
      isLoading: false,
      isError: false,
    })
    mocks.usePlaceHold.mockReturnValue({
      mutateAsync: mocks.placeHold,
      isPending: false,
    })
  })

  it('places a hold with one click', async () => {
    const user = userEvent.setup()
    const hold: PlaceHoldResponse = {
      holdId: 'hold-1',
      classId: schoolClass.id,
      studentId,
      status: 'HELD',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
    }
    mocks.placeHold.mockResolvedValue(hold)

    render(<ClassListPage />)
    await user.click(screen.getByRole('button', { name: 'Apply Now' }))

    expect(mocks.placeHold).toHaveBeenCalledTimes(1)
    expect(mocks.placeHold).toHaveBeenCalledWith(studentId)
    expect(await screen.findByText('hold-1')).toBeInTheDocument()
  })

  it.each([
    {
      status: 'HELD' as const,
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      message: 'Your seat is on hold. Complete payment from My Applications.',
    },
    {
      status: 'CONFIRMED' as const,
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
      message: 'Your admission is confirmed.',
    },
  ])('shows the existing $status application instead of Apply Now', ({ status, expiresAt, message }) => {
    const hold: StudentHoldResponse = {
      holdId: 'existing-hold',
      classId: schoolClass.id,
      className: schoolClass.name,
      status,
      expiresAt,
    }
    mocks.useStudentHolds.mockReturnValue({
      data: [hold],
      isLoading: false,
      isError: false,
    })

    render(<ClassListPage />)

    expect(screen.queryByRole('button', { name: 'Apply Now' })).not.toBeInTheDocument()
    expect(screen.getByText(message)).toBeInTheDocument()
    expect(screen.getByText(status)).toBeInTheDocument()
  })
})
