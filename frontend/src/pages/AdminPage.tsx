import { useState, type FormEvent } from 'react'
import { useClasses, useCreateClass } from '@/hooks/useClasses'
import { formatError, formatValidationErrors } from '@/utils'
import { Alert } from '@/components/ui/Alert'
import { Button } from '@/components/ui/Button'
import { Card, CardHeader, CardBody } from '@/components/ui/Card'
import { Input } from '@/components/ui/Input'
import { SeatBar } from '@/components/ui/SeatBar'
import { Spinner } from '@/components/ui/Spinner'

export function AdminPage() {
  return (
    <div className="space-y-8">
      <CreateClassForm />
      <ClassManagementTable />
    </div>
  )
}

// ── Create Class Form ─────────────────────────────────────────────────────────

function CreateClassForm() {
  const createClass = useCreateClass()
  const [name, setName] = useState('')
  const [totalSeats, setTotalSeats] = useState('')
  const [fieldErrors, setFieldErrors] = useState<{ name?: string; totalSeats?: string }>({})
  const [successMsg, setSuccessMsg] = useState<string | null>(null)
  const [apiError, setApiError] = useState<string | null>(null)

  const validate = () => {
    const errors: { name?: string; totalSeats?: string } = {}
    if (!name.trim()) errors.name = 'Class name is required.'
    if (!totalSeats || isNaN(Number(totalSeats)) || Number(totalSeats) < 1)
      errors.totalSeats = 'Must be a positive number.'
    setFieldErrors(errors)
    return Object.keys(errors).length === 0
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setApiError(null)
    setSuccessMsg(null)
    if (!validate()) return

    try {
      const result = await createClass.mutateAsync({
        name: name.trim(),
        totalSeats: Number(totalSeats),
      })
      setSuccessMsg(`Class "${result.name}" created with ${result.totalSeats} seats.`)
      setName('')
      setTotalSeats('')
      setFieldErrors({})
    } catch (err) {
      setApiError(formatValidationErrors(err) || formatError(err))
    }
  }

  return (
    <Card>
      <CardHeader>
        <h2 className="text-lg font-semibold text-gray-900">Create New Class</h2>
        <p className="mt-0.5 text-sm text-gray-500">
          Each shift of a class is a separate entry (e.g. "Class 6 – Morning Shift").
        </p>
      </CardHeader>
      <CardBody>
        <form onSubmit={handleSubmit} noValidate className="space-y-4">
          {apiError && <Alert variant="error" message={apiError} />}
          {successMsg && <Alert variant="success" message={successMsg} />}

          <div className="grid gap-4 sm:grid-cols-2">
            <Input
              label="Class Name"
              placeholder='e.g. Class 6 – Morning Shift'
              value={name}
              onChange={(e) => setName(e.target.value)}
              error={fieldErrors.name}
              required
            />
            <Input
              label="Total Seats"
              type="number"
              min={1}
              placeholder='e.g. 120'
              value={totalSeats}
              onChange={(e) => setTotalSeats(e.target.value)}
              error={fieldErrors.totalSeats}
              required
            />
          </div>

          <div className="flex justify-end">
            <Button
              type="submit"
              loading={createClass.isPending}
              disabled={createClass.isPending}
            >
              Create Class
            </Button>
          </div>
        </form>
      </CardBody>
    </Card>
  )
}

// ── Class Management Table ─────────────────────────────────────────────────────

function ClassManagementTable() {
  const { data: classes, isLoading, error } = useClasses()

  if (isLoading) {
    return (
      <div className="flex justify-center py-8">
        <Spinner label="Loading classes…" />
      </div>
    )
  }

  if (error) {
    return <Alert variant="error" title="Failed to load classes" message={formatError(error)} />
  }

  if (!classes || classes.length === 0) {
    return (
      <div className="rounded-lg border-2 border-dashed border-gray-200 py-10 text-center text-gray-500">
        No classes yet. Create one above.
      </div>
    )
  }

  return (
    <Card>
      <CardHeader>
        <h2 className="text-lg font-semibold text-gray-900">All Classes</h2>
      </CardHeader>
      <div className="divide-y divide-gray-100">
        {classes.map((cls) => (
          <div key={cls.id} className="px-5 py-4 space-y-3">
            <div className="flex items-center justify-between gap-4 flex-wrap">
              <h3 className="font-medium text-gray-900">{cls.name}</h3>
              <span className="font-mono text-xs text-gray-400">{cls.id}</span>
            </div>
            <SeatBar cls={cls} />
          </div>
        ))}
      </div>
    </Card>
  )
}
