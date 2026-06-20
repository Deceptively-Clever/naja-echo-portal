import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { useCharacters } from '../hooks/useCharacters'

export function CharacterList() {
  const { data, isLoading } = useCharacters()
  const characters = data?.characters ?? []

  if (isLoading) {
    return <p className="text-sm text-muted-foreground">Loading characters…</p>
  }

  if (characters.length === 0) {
    return (
      <Card>
        <CardContent className="py-6">
          <p className="text-sm text-muted-foreground text-center">
            No characters registered yet. Verify your first RSI handle to get started.
          </p>
        </CardContent>
      </Card>
    )
  }

  return (
    <div className="flex flex-col gap-2">
      {characters.map((character) => (
        <Card key={character.id}>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm flex items-center gap-2">
              {character.name}
              <Badge variant="secondary">{character.handle}</Badge>
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-xs text-muted-foreground">
              Registered {new Date(character.createdAt).toLocaleDateString()}
            </p>
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
