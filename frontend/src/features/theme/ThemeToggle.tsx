import { Sun, Moon } from 'lucide-react'
import { Toggle } from '@/components/ui/toggle'
import { useTheme } from './useTheme'

export function ThemeToggle({ className }: { className?: string }) {
  const { theme, setTheme } = useTheme()
  const isDark = theme === 'dark'

  return (
    <Toggle
      size="icon"
      pressed={isDark}
      onPressedChange={(on) => setTheme(on ? 'dark' : 'light')}
      aria-label={isDark ? 'Switch to light theme' : 'Switch to dark theme'}
      className={className}
    >
      {isDark ? (
        <Sun className="h-5 w-5" aria-hidden="true" />
      ) : (
        <Moon className="h-5 w-5" aria-hidden="true" />
      )}
    </Toggle>
  )
}
