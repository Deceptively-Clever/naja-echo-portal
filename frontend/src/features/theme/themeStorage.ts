export type Theme = 'dark' | 'light'

export const STORAGE_KEY = 'theme'
const DARK_CLASS = 'dark'

export function getStoredTheme(): Theme | null {
  const value = localStorage.getItem(STORAGE_KEY)
  if (value === 'dark' || value === 'light') return value
  return null
}

export function setStoredTheme(theme: Theme): void {
  localStorage.setItem(STORAGE_KEY, theme)
}

export function resolveInitialTheme(): Theme {
  const stored = getStoredTheme()
  if (stored) return stored

  try {
    if (window.matchMedia('(prefers-color-scheme: light)').matches) return 'light'
  } catch {
    // matchMedia unavailable (e.g. SSR/test environments without mock)
  }

  return 'dark'
}

export function applyTheme(theme: Theme): void {
  if (theme === 'dark') {
    document.documentElement.classList.add(DARK_CLASS)
  } else {
    document.documentElement.classList.remove(DARK_CLASS)
  }
}
