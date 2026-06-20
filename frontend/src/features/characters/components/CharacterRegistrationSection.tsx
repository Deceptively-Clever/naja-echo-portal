import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { useRegistration } from '../hooks/useRegistration'
import { useStartRegistration } from '../hooks/useStartRegistration'
import { RegistrationTokenCard } from './RegistrationTokenCard'
import { VerifyCharacterForm } from './VerifyCharacterForm'
import { CharacterList } from './CharacterList'

export function CharacterRegistrationSection() {
  const { data: registration, isLoading: loadingRegistration } = useRegistration()
  const { mutate: startRegistration, isPending } = useStartRegistration()

  const hasActiveToken = registration !== null && registration !== undefined

  return (
    <div className="flex flex-col gap-4">
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Characters</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <CharacterList />

          {!loadingRegistration && !hasActiveToken && (
            <Button
              onClick={() => startRegistration()}
              disabled={isPending}
            >
              {isPending ? 'Starting…' : 'Register Character'}
            </Button>
          )}

          {hasActiveToken && (
            <>
              <RegistrationTokenCard registration={registration} />
              <VerifyCharacterForm />
            </>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
