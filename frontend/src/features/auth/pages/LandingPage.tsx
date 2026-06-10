import { SignInButton } from '../components/SignInButton'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export function LandingPage() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-gray-50 px-4">
      <Card className="w-full max-w-md text-center">
        <CardHeader>
          <CardTitle className="text-3xl">NajaEchoPortal</CardTitle>
          <p className="text-gray-500 text-sm mt-1">
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
