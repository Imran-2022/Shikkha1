import { useState } from 'react'
import { useStudentHolds, useConfirmHold, useCancelHold } from '@/hooks/useHolds'
import { useStudentId } from '@/store/studentStore'
import { formatError, statusLabel } from '@/utils'
import { Alert } from '@/components/ui/Alert'
import { Button } from '@/components/ui/Button'
import { Card, CardHeader, CardBody } from '@/components/ui/Card'
import { StatusBadge } from '@/components/ui/Badge'
import { Countdown } from '@/components/ui/Countdown'
import { Spinner } from '@/components/ui/Spinner'
import type { StudentHoldResponse } from '@/types'

export function MyApplicationsPage() {
  const { studentId, isValid } = useStudentId()
  const { data: holds, isLoading, error, refetch } = useStudentHolds(studentId)

  if (!isValid) {
    return (
      <Alert
        variant="info"
        title="Student ID required"
        message="Please enter your 17-digit birth registration number at the top of the page to view your applications."
      />
    )
  }

  if (isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner size="lg" label="Loading your applications…" />
      </div>
    )
  }

  if (error) {
    return <Alert variant="error" title="Failed to load applications" message={formatError(error)} />
  }

  if (!holds || holds.length === 0) {
    return (
      <div className="rounded-lg border-2 border-dashed border-gray-200 py-16 text-center">
        <p className="text-gray-500">You have no applications yet.</p>
        <p className="mt-1 text-sm text-gray-400">Go to the Classes tab to apply for admission.</p>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-gray-500">
        Showing all applications for student <span className="font-mono font-semibold text-gray-800">{studentId}</span>
      </p>

      <div className="space-y-3">
        {holds.map((hold) => (
          <HoldCard
            key={hold.holdId}
            hold={hold}
            studentId={studentId}
            onActionComplete={() => refetch()}
          />
        ))}
      </div>
    </div>
  )
}

// ── HoldCard ──────────────────────────────────────────────────────────────────

interface HoldCardProps {
  hold: StudentHoldResponse
  studentId: string
  onActionComplete: () => void
}

function HoldCard({ hold, studentId, onActionComplete }: HoldCardProps) {
  const confirm = useConfirmHold()
  const cancel = useCancelHold()
  const [localStatus, setLocalStatus] = useState(hold.status)
  const [error, setError] = useState<string | null>(null)
  const [expired, setExpired] = useState(false)

  const resolvedStatus = expired ? 'EXPIRED' : statusLabel(localStatus, hold.expiresAt)
  const isHeld = localStatus === 'HELD' && !expired && new Date(hold.expiresAt).getTime() > Date.now()

  const handleConfirm = async () => {
    setError(null)
    try {
      const res = await confirm.mutateAsync({ holdId: hold.holdId, studentId })
      setLocalStatus(res.status)
      onActionComplete()
    } catch (err) {
      setError(formatError(err))
    }
  }

  const handleCancel = async () => {
    setError(null)
    try {
      await cancel.mutateAsync({ holdId: hold.holdId, studentId })
      setLocalStatus('CANCELLED')
      onActionComplete()
    } catch (err) {
      setError(formatError(err))
    }
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between gap-2 flex-wrap">
          <h3 className="font-semibold text-gray-900">{hold.className}</h3>
          <StatusBadge status={resolvedStatus} />
        </div>
      </CardHeader>
      <CardBody className="space-y-3">
        {error && <Alert variant="error" message={error} />}

        <dl className="grid grid-cols-2 gap-x-4 gap-y-1 text-sm">
          <dt className="text-gray-500">Hold ID</dt>
          <dd className="font-mono text-xs text-gray-800 truncate">{hold.holdId}</dd>

          <dt className="text-gray-500">Status</dt>
          <dd className="text-gray-800">{resolvedStatus}</dd>

          {isHeld && (
            <>
              <dt className="text-gray-500">Time remaining</dt>
              <dd>
                <Countdown
                  expiresAt={hold.expiresAt}
                  onExpire={() => {
                    setExpired(true)
                    onActionComplete()
                  }}
                />
              </dd>
            </>
          )}
        </dl>

        {isHeld && (
          <div className="flex gap-2 pt-1">
            <Button
              variant="success"
              size="sm"
              onClick={handleConfirm}
              loading={confirm.isPending}
              disabled={cancel.isPending}
            >
              Pay Admission Fee
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={handleCancel}
              loading={cancel.isPending}
              disabled={confirm.isPending}
            >
              Cancel
            </Button>
          </div>
        )}

        {resolvedStatus === 'CONFIRMED' && (
          <Alert variant="success" message="Your admission is confirmed." />
        )}

        {(resolvedStatus === 'EXPIRED') && (
          <Alert variant="error" message="This hold has expired. Apply again from the Classes tab." />
        )}
      </CardBody>
    </Card>
  )
}
