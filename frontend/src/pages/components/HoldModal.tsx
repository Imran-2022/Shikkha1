import { useState, useCallback } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { Button } from '@/components/ui/Button'
import { Alert } from '@/components/ui/Alert'
import { Countdown } from '@/components/ui/Countdown'
import { StatusBadge } from '@/components/ui/Badge'
import { useConfirmHold, useCancelHold, holdKeys } from '@/hooks/useHolds'
import { classKeys } from '@/hooks/useClasses'
import { formatError } from '@/utils'
import type { PlaceHoldResponse } from '@/types'

interface HoldModalProps {
  hold: PlaceHoldResponse
  studentId: string
  onClose: () => void
}

export function HoldModal({ hold, studentId, onClose }: HoldModalProps) {
  const qc = useQueryClient()
  const confirm = useConfirmHold()
  const cancel = useCancelHold()
  const [status, setStatus] = useState(hold.status)
  const [error, setError] = useState<string | null>(null)
  const [expired, setExpired] = useState(false)

  const refresh = useCallback(async () => {
    qc.invalidateQueries({ queryKey: holdKeys.byId(hold.holdId) })
    qc.invalidateQueries({ queryKey: classKeys.all })
  }, [hold.holdId, qc])

  const handleExpire = useCallback(() => {
    setExpired(true)
    setStatus('EXPIRED')
    refresh()
  }, [refresh])

  const handleConfirm = async () => {
    setError(null)
    try {
      const res = await confirm.mutateAsync({ holdId: hold.holdId, studentId })
      setStatus(res.status)
    } catch (err) {
      setError(formatError(err))
    }
  }

  const handleCancel = async () => {
    setError(null)
    try {
      await cancel.mutateAsync({ holdId: hold.holdId, studentId })
      setStatus('CANCELLED')
      setTimeout(onClose, 1200)
    } catch (err) {
      setError(formatError(err))
    }
  }

  const isHeld = status === 'HELD' && !expired
  const isConfirmed = status === 'CONFIRMED'

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4" role="dialog" aria-modal="true" aria-labelledby="hold-modal-title">
      <div className="w-full max-w-md rounded-xl bg-white shadow-xl">
        {/* Header */}
        <div className="border-b border-gray-100 px-6 py-4 flex items-center justify-between">
          <h2 id="hold-modal-title" className="text-lg font-semibold text-gray-900">
            {isConfirmed ? '🎉 Admission Confirmed!' : 'Your Seat Hold'}
          </h2>
          <StatusBadge status={status} expiresAt={hold.expiresAt} />
        </div>

        {/* Body */}
        <div className="px-6 py-5 space-y-4">
          {error && <Alert variant="error" message={error} />}

          <dl className="space-y-2 text-sm">
            <div className="flex justify-between">
              <dt className="text-gray-500">Hold ID</dt>
              <dd className="font-mono text-gray-900 text-xs">{hold.holdId}</dd>
            </div>
            <div className="flex justify-between">
              <dt className="text-gray-500">Student ID</dt>
              <dd className="font-mono text-gray-900">{hold.studentId}</dd>
            </div>
            {isHeld && (
              <div className="flex justify-between">
                <dt className="text-gray-500">Time remaining</dt>
                <dd>
                  <Countdown expiresAt={hold.expiresAt} onExpire={handleExpire} />
                </dd>
              </div>
            )}
          </dl>

          {isHeld && (
            <Alert
              variant="warning"
              message="Pay the admission fee before the timer expires. Your seat will be released otherwise."
            />
          )}

          {isConfirmed && (
            <Alert
              variant="success"
              message="Your admission has been confirmed successfully. No further action needed."
            />
          )}

          {(expired || status === 'EXPIRED') && (
            <Alert
              variant="error"
              title="Hold expired"
              message="Your seat hold has expired. Please go back and apply again."
            />
          )}
        </div>

        {/* Footer */}
        <div className="border-t border-gray-100 px-6 py-4 flex gap-3 justify-end">
          {isHeld && (
            <>
              <Button
                variant="ghost"
                onClick={handleCancel}
                loading={cancel.isPending}
                disabled={confirm.isPending}
              >
                Cancel Application
              </Button>
              <Button
                variant="success"
                onClick={handleConfirm}
                loading={confirm.isPending}
                disabled={cancel.isPending}
              >
                Pay Admission Fee
              </Button>
            </>
          )}
          <Button variant="ghost" onClick={onClose}>
            {isConfirmed || expired || status === 'CANCELLED' ? 'Close' : 'Back to list'}
          </Button>
        </div>
      </div>
    </div>
  )
}
