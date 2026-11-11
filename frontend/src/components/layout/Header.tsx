import { useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import clsx from 'clsx'
import { useStudentId } from '@/store/studentStore'
import { validateStudentId } from '@/utils'

export function Header() {
  const { studentId, setStudentId, isValid } = useStudentId()
  const [inputValue, setInputValue] = useState(studentId)
  const [error, setError] = useState<string | null>(null)
  const location = useLocation()

  const handleChange = (value: string) => {
    // Allow only digits, max 17
    const digits = value.replace(/\D/g, '').slice(0, 17)
    setInputValue(digits)
    const err = validateStudentId(digits)
    setError(err)
    if (!err) setStudentId(digits)
    else if (!digits) setStudentId('')
  }

  const navLinks = [
    { to: '/', label: 'Classes' },
    { to: '/my-applications', label: 'My Applications' },
    { to: '/admin', label: 'Admin' },
  ]

  return (
    <header className="sticky top-0 z-30 border-b border-gray-200 bg-white shadow-sm">
      <div className="mx-auto max-w-6xl px-4">
        {/* Top bar */}
        <div className="flex items-center justify-between gap-4 py-3">
          <div className="flex items-center gap-3">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-blue-600 text-white font-bold text-sm">
              SA
            </div>
            <span className="font-semibold text-gray-900 text-lg">School Admission</span>
          </div>

          {/* Student ID input */}
          <div className="flex items-center gap-2">
            <label htmlFor="student-id-global" className="shrink-0 text-sm font-medium text-gray-600">
              Birth Reg. No:
            </label>
            <div className="relative">
              <input
                id="student-id-global"
                type="text"
                inputMode="numeric"
                value={inputValue}
                onChange={(e) => handleChange(e.target.value)}
                placeholder="17-digit number"
                maxLength={17}
                className={clsx(
                  'w-44 rounded-md border px-3 py-1.5 text-sm font-mono',
                  'focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent',
                  error
                    ? 'border-red-400 bg-red-50'
                    : isValid
                      ? 'border-green-400 bg-green-50'
                      : 'border-gray-300 bg-white'
                )}
                aria-describedby={error ? 'student-id-error' : undefined}
              />
              {isValid && (
                <span className="absolute right-2 top-1/2 -translate-y-1/2 text-green-500 text-sm">✓</span>
              )}
            </div>
            {error && (
              <span id="student-id-error" className="text-xs text-red-600">{error}</span>
            )}
          </div>
        </div>

        {/* Navigation */}
        <nav className="flex gap-1 pb-0">
          {navLinks.map(({ to, label }) => (
            <Link
              key={to}
              to={to}
              className={clsx(
                'px-4 py-2 text-sm font-medium rounded-t-md border-b-2 transition-colors',
                location.pathname === to
                  ? 'border-blue-600 text-blue-700 bg-blue-50'
                  : 'border-transparent text-gray-600 hover:text-gray-900 hover:border-gray-300'
              )}
            >
              {label}
            </Link>
          ))}
        </nav>
      </div>
    </header>
  )
}
