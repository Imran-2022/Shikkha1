// ── API Response Types ────────────────────────────────────────────────────────

export interface SchoolClass {
  id: string
  name: string
  totalSeats: number
  heldSeats: number
  confirmedSeats: number
  availableSeats: number
}

export interface ClassCreatedResponse {
  id: string
  name: string
  totalSeats: number
}

export type HoldStatus = 'HELD' | 'CONFIRMED' | 'EXPIRED' | 'CANCELLED'

export interface PlaceHoldResponse {
  holdId: string
  classId: string
  studentId: string
  status: HoldStatus
  expiresAt: string
}

export interface HoldStatusResponse {
  holdId: string
  classId: string
  status: HoldStatus
  expiresAt: string
}

export interface ConfirmHoldResponse {
  holdId: string
  status: HoldStatus
}

export interface StudentHoldResponse {
  holdId: string
  classId: string
  className: string
  status: HoldStatus
  expiresAt: string
}

// ── Request Types ─────────────────────────────────────────────────────────────

export interface CreateClassRequest {
  name: string
  totalSeats: number
}

// ── API Error ─────────────────────────────────────────────────────────────────

export interface ApiError {
  title: string
  detail: string
  status: number
  errors?: Record<string, string[]>
}
