import { useState } from 'react'
import { useClasses, usePlaceHold } from '@/hooks/useClasses'
import { useStudentId } from '@/store/studentStore'
import { formatError } from '@/utils'
import { Alert } from '@/components/ui/Alert'
import { Button } from '@/components/ui/Button'
import { Card, CardHeader, CardBody } from '@/components/ui/Card'
import { SeatBar } from '@/components/ui/SeatBar'
import { Spinner } from '@/components/ui/Spinner'
import { HoldModal } from './components/HoldModal'
import type { PlaceHoldResponse } from '@/types'

export function ClassListPage() {
  const { studentId, isValid } = useStudentId()
  const { data: classes, isLoading, error } = useClasses()
  const [activeHold, setActiveHold] = useState<PlaceHoldResponse | null>(null)
  const [applyingForClass, setApplyingForClass] = useState<string | null>(null)

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner size="lg" label="Loading classes…" />
      </div>
    )
  }

  if (error) {
    return <Alert variant="error" title="Failed to load classes" message={formatError(error)} />
  }

  if (!classes || classes.length === 0) {
    return (
      <div className="rounded-lg border-2 border-dashed border-gray-200 py-16 text-center">
        <p className="text-gray-500">No classes available yet.</p>
        <p className="mt-1 text-sm text-gray-400">An admin can create classes from the Admin tab.</p>
      </div>
    )
  }

  return (
    <>
      <div className="space-y-4">
        {!isValid && (
          <Alert
            variant="info"
            message="Enter your 17-digit birth registration number at the top of the page to apply for admission."
          />
        )}

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {classes.map((cls) => (
            <ClassCard
              key={cls.id}
              cls={cls}
              studentId={studentId}
              isValidStudent={isValid}
              isApplying={applyingForClass === cls.id}
              onApply={() => {
                setApplyingForClass(cls.id)
              }}
              onHoldPlaced={(hold) => {
                setApplyingForClass(null)
                setActiveHold(hold)
              }}
              onApplyCancel={() => setApplyingForClass(null)}
            />
          ))}
        </div>
      </div>

      {activeHold && (
        <HoldModal
          hold={activeHold}
          studentId={studentId}
          onClose={() => setActiveHold(null)}
        />
      )}
    </>
  )
}

// ── ClassCard ─────────────────────────────────────────────────────────────────

interface ClassCardProps {
  cls: import('@/types').SchoolClass
  studentId: string
  isValidStudent: boolean
  isApplying: boolean
  onApply: () => void
  onHoldPlaced: (hold: PlaceHoldResponse) => void
  onApplyCancel: () => void
}

function ClassCard({
  cls,
  studentId,
  isValidStudent,
  isApplying,
  onApply,
  onHoldPlaced,
  onApplyCancel,
}: ClassCardProps) {
  const placeHold = usePlaceHold(cls.id)
  const [localError, setLocalError] = useState<string | null>(null)

  const handleApply = async () => {
    if (!isApplying) {
      onApply()
      return
    }
    setLocalError(null)
    try {
      const hold = await placeHold.mutateAsync(studentId)
      onHoldPlaced(hold)
    } catch (err) {
      setLocalError(formatError(err))
      onApplyCancel()
    }
  }

  const isFull = cls.availableSeats === 0

  return (
    <Card>
      <CardHeader>
        <h2 className="font-semibold text-gray-900">{cls.name}</h2>
      </CardHeader>
      <CardBody className="space-y-4">
        <SeatBar cls={cls} />

        {localError && <Alert variant="error" message={localError} />}

        {isFull ? (
          <span className="inline-block rounded-full bg-red-100 px-3 py-1 text-xs font-semibold text-red-700">
            No seats available
          </span>
        ) : (
          <Button
            onClick={handleApply}
            loading={placeHold.isPending}
            disabled={!isValidStudent || isFull}
            className="w-full"
            title={!isValidStudent ? 'Enter your student ID first' : undefined}
          >
            Apply Now
          </Button>
        )}
      </CardBody>
    </Card>
  )
}
