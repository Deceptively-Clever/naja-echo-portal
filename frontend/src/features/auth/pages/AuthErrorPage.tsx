import { Link } from 'react-router-dom'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { buttonVariants } from '@/components/ui/button'
import { cn } from '@/lib/utils'

export function AuthErrorPage() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-gray-50 px-4">
      <div className="w-full max-w-md space-y-4">
        <Alert variant="destructive">
          <AlertTitle>Sign-in could not be completed</AlertTitle>
          <AlertDescription>
            Something went wrong during authorization. Your account has not been affected.
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
