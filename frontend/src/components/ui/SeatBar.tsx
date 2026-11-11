import type { SchoolClass } from '@/types'

interface SeatBarProps {
  cls: SchoolClass
}

export function SeatBar({ cls }: SeatBarProps) {
  const confirmedPct = cls.totalSeats > 0 ? (cls.confirmedSeats / cls.totalSeats) * 100 : 0
  const heldPct = cls.totalSeats > 0 ? (cls.heldSeats / cls.totalSeats) * 100 : 0

  return (
    <div className="space-y-2">
      {/* Progress bar */}
      <div className="h-2.5 overflow-hidden rounded-full bg-gray-100">
        <div className="flex h-full">
          <div
            className="bg-green-500 transition-all duration-300"
            style={{ width: `${confirmedPct}%` }}
            title={`Confirmed: ${cls.confirmedSeats}`}
          />
          <div
            className="bg-amber-400 transition-all duration-300"
            style={{ width: `${heldPct}%` }}
            title={`Held: ${cls.heldSeats}`}
          />
        </div>
      </div>

      {/* Seat counts */}
      <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-gray-600">
        <span>
          <span className="inline-block h-2 w-2 rounded-full bg-gray-100 border border-gray-300 mr-1" />
          Available: <strong>{cls.availableSeats}</strong>
        </span>
        <span>
          <span className="inline-block h-2 w-2 rounded-full bg-amber-400 mr-1" />
          Held: <strong>{cls.heldSeats}</strong>
        </span>
        <span>
          <span className="inline-block h-2 w-2 rounded-full bg-green-500 mr-1" />
          Confirmed: <strong>{cls.confirmedSeats}</strong>
        </span>
        <span className="ml-auto font-medium">
          Total: {cls.totalSeats}
        </span>
      </div>
    </div>
  )
}
