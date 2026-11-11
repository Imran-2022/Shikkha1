import clsx from 'clsx'

type AlertVariant = 'error' | 'success' | 'warning' | 'info'

interface AlertProps {
  variant?: AlertVariant
  title?: string
  message: string
  className?: string
}

const styles: Record<AlertVariant, { wrapper: string; icon: string }> = {
  error:   { wrapper: 'bg-red-50 border-red-300 text-red-800',    icon: '✕' },
  success: { wrapper: 'bg-green-50 border-green-300 text-green-800', icon: '✓' },
  warning: { wrapper: 'bg-amber-50 border-amber-300 text-amber-800', icon: '!' },
  info:    { wrapper: 'bg-blue-50 border-blue-300 text-blue-800',  icon: 'i' },
}

export function Alert({ variant = 'error', title, message, className }: AlertProps) {
  const { wrapper, icon } = styles[variant]
  return (
    <div className={clsx('flex gap-3 rounded-lg border px-4 py-3', wrapper, className)} role="alert">
      <span className="mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full border border-current text-xs font-bold">
        {icon}
      </span>
      <div className="min-w-0">
        {title && <p className="font-semibold">{title}</p>}
        <p className="text-sm">{message}</p>
      </div>
    </div>
  )
}
