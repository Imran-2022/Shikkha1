import { useState } from 'react'
import { useClasses, usePlaceHold } from '@/hooks/useClasses'
import { useStudentHolds } from '@/hooks/useHolds'
import { useStudentId } from '@/store/studentStore'
import { formatError, isHoldActive, statusLabel } from '@/utils'
import { Alert } from '@/components/ui/Alert'
import { Button } from '@/components/ui/Button'
import { StatusBadge } from '@/components/ui/Badge'
import { Card, CardHeader, CardBody } from '@/components/ui/Card'
import { SeatBar } from '@/components/ui/SeatBar'
import { Spinner } from '@/components/ui/Spinner'
import { HoldModal } from './components/HoldModal'
import type { PlaceHoldResponse, StudentHoldResponse } from '@/types'

export function ClassListPage() {
  const { studentId, isValid } = useStudentId()
  const { data: classes, isLoading, error } = useClasses()
  const studentHolds = useStudentHolds(studentId)
  const [activeHold, setActiveHold] = useState<PlaceHoldResponse | null>(null)

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
        {isValid && studentHolds.isError && !studentHolds.data && (
          <Alert
            variant="error"
            title="Could not check your applications"
            message={`${formatError(studentHolds.error)} New applications are disabled until your status can be checked.`}
          />
        )}

        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {classes.map((cls) => (
            <ClassCard
              key={cls.id}
              cls={cls}
              studentId={studentId}
              isValidStudent={isValid}
              existingHold={findClassApplication(studentHolds.data, cls.id)}
              isCheckingApplication={isValid && !studentHolds.data && studentHolds.isLoading}
              applicationStatusUnavailable={isValid && !studentHolds.data && studentHolds.isError}
              onHoldPlaced={setActiveHold}
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
  existingHold: StudentHoldResponse | undefined
  isCheckingApplication: boolean
  applicationStatusUnavailable: boolean
  onHoldPlaced: (hold: PlaceHoldResponse) => void
}

function ClassCard({
  cls,
  studentId,
  isValidStudent,
  existingHold,
  isCheckingApplication,
  applicationStatusUnavailable,
  onHoldPlaced,
}: ClassCardProps) {
  const placeHold = usePlaceHold(cls.id)
  const [localError, setLocalError] = useState<string | null>(null)

  const handleApply = async () => {
    setLocalError(null)
    try {
      const hold = await placeHold.mutateAsync(studentId)
      onHoldPlaced(hold)
    } catch (err) {
      setLocalError(formatError(err))
    }
  }

  const isFull = cls.availableSeats === 0
  const holdStatus = existingHold
    ? statusLabel(existingHold.status, existingHold.expiresAt)
    : undefined
  const hasActiveApplication =
    existingHold?.status === 'CONFIRMED' ||
    (existingHold !== undefined && isHoldActive(existingHold.status, existingHold.expiresAt))

  return (
    <Card>
      <CardHeader>
        <h2 className="font-semibold text-gray-900">{cls.name}</h2>
      </CardHeader>
      <CardBody className="space-y-4">
        <SeatBar cls={cls} />

        {localError && <Alert variant="error" message={localError} />}
        {existingHold && !hasActiveApplication && holdStatus && (
          <StatusBadge status={holdStatus} expiresAt={existingHold.expiresAt} />
        )}

        {hasActiveApplication && existingHold && holdStatus === 'CONFIRMED' ? (
          <div className="space-y-2">
            <StatusBadge status={holdStatus} />
            <p className="text-sm text-green-700">Your admission is confirmed.</p>
          </div>
        ) : hasActiveApplication && existingHold ? (
          <div className="space-y-2">
            <StatusBadge
              status={statusLabel(existingHold.status, existingHold.expiresAt)}
              expiresAt={existingHold.expiresAt}
            />
            <p className="text-sm text-amber-700">
              Your seat is on hold. Complete payment from My Applications.
            </p>
          </div>
        ) : isCheckingApplication ? (
          <Button disabled className="w-full">Checking application…</Button>
        ) : applicationStatusUnavailable ? (
          <Button disabled className="w-full">Application status unavailable</Button>
        ) : isFull ? (
          <span className="inline-block rounded-full bg-red-100 px-3 py-1 text-xs font-semibold text-red-700">
            No seats available
          </span>
        ) : (
          <Button
            onClick={handleApply}
            loading={placeHold.isPending}
            disabled={!isValidStudent}
            className="w-full"
            title={!isValidStudent ? 'Enter your student ID first' : undefined}
          >
            {existingHold ? 'Apply again' : 'Apply Now'}
          </Button>
        )}
      </CardBody>
    </Card>
  )
}

function findClassApplication(
  holds: StudentHoldResponse[] | undefined,
  classId: string
): StudentHoldResponse | undefined {
  const classHolds = holds?.filter((hold) => hold.classId === classId) ?? []

  return (
    classHolds.find((hold) => hold.status === 'CONFIRMED') ??
    classHolds.find((hold) => isHoldActive(hold.status, hold.expiresAt)) ??
    classHolds[0]
  )
}
