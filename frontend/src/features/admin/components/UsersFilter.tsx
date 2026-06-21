import { Input } from '@/components/ui/input'

interface UsersFilterProps {
  value: string
  onChange: (value: string) => void
}

export function UsersFilter({ value, onChange }: UsersFilterProps) {
  return (
    <Input
      className="max-w-sm"
      placeholder="Filter by name, character, or role…"
      value={value}
      onChange={(e) => onChange(e.target.value)}
      aria-label="Filter users"
    />
  )
}
