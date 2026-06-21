import { useState, useEffect, useRef } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { characterKeys } from '../hooks/characterQueryKeys'
import type { PendingRegistrationResponse } from '../schemas/characterSchemas'

interface Props {
  registration: PendingRegistrationResponse
}

function formatTimeRemaining(expiresAt: string): string {
  const remaining = new Date(expiresAt).getTime() - Date.now()
  if (remaining <= 0) return 'Expired'
  const totalSeconds = Math.floor(remaining / 1000)
  const minutes = Math.floor(totalSeconds / 60)
  const seconds = totalSeconds % 60
  return `${minutes}:${String(seconds).padStart(2, '0')}`
}

export function RegistrationTokenCard({ registration }: Props) {
  const queryClient = useQueryClient()
  const [timeRemaining, setTimeRemaining] = useState(() => formatTimeRemaining(registration.expiresAt))
  const [copied, setCopied] = useState(false)
  const copiedTimer = useRef<ReturnType<typeof setTimeout> | null>(null)

  const isExpired = timeRemaining === 'Expired'

  useEffect(() => {
    if (isExpired) {
      // Token has lapsed client-side — drop the cached registration so the
      // section reverts to the "Register Character" call to action.
      queryClient.setQueryData(characterKeys.registration(), null)
      return
    }
    const interval = setInterval(() => {
      setTimeRemaining(formatTimeRemaining(registration.expiresAt))
    }, 1000)
    return () => clearInterval(interval)
  }, [registration.expiresAt, isExpired, queryClient])

  // Clear any pending "Copied!" reset on unmount.
  useEffect(() => () => {
    if (copiedTimer.current) clearTimeout(copiedTimer.current)
  }, [])

  const handleCopy = async () => {
    if (!navigator.clipboard) return
    try {
      await navigator.clipboard.writeText(registration.token)
      setCopied(true)
      if (copiedTimer.current) clearTimeout(copiedTimer.current)
      copiedTimer.current = setTimeout(() => setCopied(false), 2000)
    } catch {
      // Write rejected (denied permission) — leave the button in its idle state.
      setCopied(false)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base flex items-center gap-2">
          Verification Token
          {isExpired ? (
            <Badge className="border-transparent bg-destructive text-destructive-foreground">Expired</Badge>
          ) : (
            <Badge variant="secondary">{timeRemaining}</Badge>
          )}
        </CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <p className="text-sm text-muted-foreground">
          Paste this token into your RSI bio at{' '}
          <span className="font-mono text-xs">robertsspaceindustries.com</span>, then submit your handle below.
        </p>
        <div className="flex items-center gap-2">
          <code className="flex-1 rounded bg-muted px-3 py-2 text-sm font-mono break-all">
            {registration.token}
          </code>
          <Button
            variant="outline"
            size="sm"
            onClick={handleCopy}
            disabled={isExpired}
            aria-label="Copy token to clipboard"
          >
            {copied ? 'Copied!' : 'Copy'}
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}
