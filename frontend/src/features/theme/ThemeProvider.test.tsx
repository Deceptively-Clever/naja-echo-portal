import { describe, it, expect, beforeEach } from 'vitest'
import { render, screen, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ThemeProvider } from './ThemeProvider'
import { useTheme } from './useTheme'
import { STORAGE_KEY } from './themeStorage'

function ThemeReadout() {
  const { theme, toggleTheme, setTheme } = useTheme()
  return (
    <div>
      <span data-testid="theme">{theme}</span>
      <button onClick={toggleTheme}>toggle</button>
      <button onClick={() => setTheme('light')}>set-light</button>
      <button onClick={() => setTheme('dark')}>set-dark</button>
    </div>
  )
}

describe('ThemeProvider', () => {
  beforeEach(() => {
    localStorage.clear()
    document.documentElement.classList.remove('dark')
  })

  it('initializes theme from stored preference', () => {
    localStorage.setItem(STORAGE_KEY, 'light')
    render(
      <ThemeProvider>
        <ThemeReadout />
      </ThemeProvider>
    )
    expect(screen.getByTestId('theme').textContent).toBe('light')
  })

  it('initializes to flagship dark when no stored preference', () => {
    render(
      <ThemeProvider>
        <ThemeReadout />
      </ThemeProvider>
    )
    expect(screen.getByTestId('theme').textContent).toBe('dark')
  })

  it('does not flash: dark class is present on init when theme is dark', () => {
    render(
      <ThemeProvider>
        <ThemeReadout />
      </ThemeProvider>
    )
    expect(document.documentElement.classList.contains('dark')).toBe(true)
  })

  it('does not flash: dark class is absent on init when stored theme is light', () => {
    localStorage.setItem(STORAGE_KEY, 'light')
    render(
      <ThemeProvider>
        <ThemeReadout />
      </ThemeProvider>
    )
    expect(document.documentElement.classList.contains('dark')).toBe(false)
  })

  it('toggleTheme flips dark → light', async () => {
    const user = userEvent.setup()
    render(
      <ThemeProvider>
        <ThemeReadout />
      </ThemeProvider>
    )
    await user.click(screen.getByText('toggle'))
    expect(screen.getByTestId('theme').textContent).toBe('light')
  })

  it('toggleTheme flips light → dark', async () => {
    localStorage.setItem(STORAGE_KEY, 'light')
    const user = userEvent.setup()
    render(
      <ThemeProvider>
        <ThemeReadout />
      </ThemeProvider>
    )
    await user.click(screen.getByText('toggle'))
    expect(screen.getByTestId('theme').textContent).toBe('dark')
  })

  it('toggleTheme applies dark class side effect', async () => {
    localStorage.setItem(STORAGE_KEY, 'light')
    const user = userEvent.setup()
    render(
      <ThemeProvider>
        <ThemeReadout />
      </ThemeProvider>
    )
    await user.click(screen.getByText('toggle'))
    expect(document.documentElement.classList.contains('dark')).toBe(true)
  })

  it('toggleTheme removes dark class side effect', async () => {
    const user = userEvent.setup()
    render(
      <ThemeProvider>
        <ThemeReadout />
      </ThemeProvider>
    )
    await user.click(screen.getByText('toggle'))
    expect(document.documentElement.classList.contains('dark')).toBe(false)
  })

  it('toggleTheme persists to localStorage', async () => {
    const user = userEvent.setup()
    render(
      <ThemeProvider>
        <ThemeReadout />
      </ThemeProvider>
    )
    await user.click(screen.getByText('toggle'))
    expect(localStorage.getItem(STORAGE_KEY)).toBe('light')
  })

  it('setTheme to "light" updates state, class, and storage', async () => {
    const user = userEvent.setup()
    render(
      <ThemeProvider>
        <ThemeReadout />
      </ThemeProvider>
    )
    await user.click(screen.getByText('set-light'))
    expect(screen.getByTestId('theme').textContent).toBe('light')
    expect(document.documentElement.classList.contains('dark')).toBe(false)
    expect(localStorage.getItem(STORAGE_KEY)).toBe('light')
  })

  it('setTheme to "dark" updates state, class, and storage', async () => {
    localStorage.setItem(STORAGE_KEY, 'light')
    const user = userEvent.setup()
    render(
      <ThemeProvider>
        <ThemeReadout />
      </ThemeProvider>
    )
    await user.click(screen.getByText('set-dark'))
    expect(screen.getByTestId('theme').textContent).toBe('dark')
    expect(document.documentElement.classList.contains('dark')).toBe(true)
    expect(localStorage.getItem(STORAGE_KEY)).toBe('dark')
  })
})

describe('ThemeProvider persistence (US2)', () => {
  beforeEach(() => {
    localStorage.clear()
    document.documentElement.classList.remove('dark')
  })

  it('mounts with dark class absent when stored theme is light (no reload flash)', () => {
    localStorage.setItem(STORAGE_KEY, 'light')
    render(
      <ThemeProvider>
        <ThemeReadout />
      </ThemeProvider>
    )
    expect(document.documentElement.classList.contains('dark')).toBe(false)
  })

  it('mounts with dark class present when stored theme is dark', () => {
    localStorage.setItem(STORAGE_KEY, 'dark')
    render(
      <ThemeProvider>
        <ThemeReadout />
      </ThemeProvider>
    )
    expect(document.documentElement.classList.contains('dark')).toBe(true)
  })

  it('persists preference: toggling writes to localStorage so next mount restores it', async () => {
    const user = userEvent.setup()
    const { unmount } = render(
      <ThemeProvider>
        <ThemeReadout />
      </ThemeProvider>
    )
    await user.click(screen.getByText('toggle')) // dark → light
    expect(localStorage.getItem(STORAGE_KEY)).toBe('light')
    unmount()

    // Simulate new mount (new session reading stored preference)
    render(
      <ThemeProvider>
        <ThemeReadout />
      </ThemeProvider>
    )
    expect(screen.getByTestId('theme').textContent).toBe('light')
    expect(document.documentElement.classList.contains('dark')).toBe(false)
  })
})

describe('useTheme outside ThemeProvider', () => {
  it('throws a descriptive error', () => {
    function BrokenComponent() {
      useTheme()
      return null
    }
    expect(() => {
      act(() => {
        render(<BrokenComponent />)
      })
    }).toThrow()
  })
})
