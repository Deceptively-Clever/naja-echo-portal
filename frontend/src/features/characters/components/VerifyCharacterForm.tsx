import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import { ApiError } from '@/lib/apiClient'
import { useVerifyCharacter } from '../hooks/useVerifyCharacter'

interface FormValues {
  handle: string
}

function getVerifyErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    // message contains the parsed problem title from the server
    if (error.message && !error.message.startsWith('Request failed:')) {
      return error.message
    }
    switch (error.status) {
      case 422: return 'Token not found on your RSI profile'
      case 404: return 'RSI citizen profile not found for that handle'
      case 502: return 'Could not reach RSI — please try again shortly'
    }
  }
  return 'An unexpected error occurred'
}

export function VerifyCharacterForm() {
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } = useForm<FormValues>()
  const { mutateAsync } = useVerifyCharacter()
  const [serverError, setServerError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)

  const onSubmit = async (values: FormValues) => {
    setServerError(null)
    setSuccess(false)
    try {
      await mutateAsync(values.handle.trim())
      setSuccess(true)
      reset()
    } catch (error) {
      setServerError(getVerifyErrorMessage(error))
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-3">
      <div className="flex flex-col gap-1">
        <label htmlFor="handle" className="text-sm font-medium">RSI Handle</label>
        <div className="flex gap-2">
          <input
            id="handle"
            {...register('handle', {
              required: 'Handle is required',
              maxLength: { value: 100, message: 'Handle must be 100 characters or fewer' },
            })}
            placeholder="e.g. g8r"
            className="flex-1 rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
            disabled={isSubmitting}
          />
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Verifying…' : 'Verify'}
          </Button>
        </div>
        {errors.handle && (
          <p className="text-sm text-destructive">{errors.handle.message}</p>
        )}
      </div>
      {serverError && (
        <p className="text-sm text-destructive" role="alert">{serverError}</p>
      )}
      {success && (
        <p className="text-sm text-green-600" role="status">Character verified and registered successfully!</p>
      )}
    </form>
  )
}
