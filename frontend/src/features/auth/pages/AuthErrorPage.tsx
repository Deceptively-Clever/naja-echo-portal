import { Link, useSearchParams } from 'react-router-dom'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { buttonVariants } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const reasonMessages: Record<string, string> = {
  oauth_error: 'The sign-in request was denied or an error occurred.',
  state_mismatch: 'The sign-in session expired or was tampered with. Please try again.',
  server_error: 'An unexpected error occurred on our end.',
}

export function AuthErrorPage() {
  const [params] = useSearchParams()
  const reason = params.get('reason') ?? 'server_error'
  const detail = reasonMessages[reason] ?? reasonMessages['server_error']

  return (
    <main className="flex min-h-screen items-center justify-center bg-gray-50 px-4">
      <div className="w-full max-w-md space-y-4">
        <Alert variant="destructive">
          <AlertTitle>Sign-in could not be completed</AlertTitle>
          <AlertDescription>
            {detail} Your account has not been affected.
          </AlertDescription>
        </Alert>
        <div className="text-center">
          <Link
            to="/"
            className={cn(buttonVariants())}
            aria-label="Try again"
          >
            Try again
          </Link>
        </div>
      </div>
    </main>
  )
}
