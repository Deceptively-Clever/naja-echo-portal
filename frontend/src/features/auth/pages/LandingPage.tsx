import { SignInButton } from '../components/SignInButton'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export function LandingPage() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-background px-4">
      <Card className="w-full max-w-md text-center">
        <CardHeader>
          <CardTitle className="text-3xl">Welcome to Naja Echo!</CardTitle>
          <p className="text-muted-foreground text-sm mt-1">
            Org utilities for the Naja Echo organisation in Star Citizen.
          </p>
        </CardHeader>
        <CardContent>
          <SignInButton />
        </CardContent>
      </Card>
    </main>
  )
}
