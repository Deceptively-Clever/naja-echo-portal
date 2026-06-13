import { useState, useCallback, type ReactNode } from 'react'
import { resolveInitialTheme, applyTheme, setStoredTheme, type Theme } from './themeStorage'
import { ThemeContext } from './ThemeContext'

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<Theme>(() => {
    const initial = resolveInitialTheme()
    applyTheme(initial)
    return initial
  })

  const setTheme = useCallback((next: Theme) => {
    setThemeState(next)
    applyTheme(next)
    setStoredTheme(next)
  }, [])

  const toggleTheme = useCallback(() => {
    setTheme(theme === 'dark' ? 'light' : 'dark')
  }, [theme, setTheme])

  return (
    <ThemeContext.Provider value={{ theme, toggleTheme, setTheme }}>
      {children}
    </ThemeContext.Provider>
  )
}
