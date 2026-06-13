import { describe, it, expect, beforeEach, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ThemeProvider } from './ThemeProvider'
import { ThemeToggle } from './ThemeToggle'
import { STORAGE_KEY } from './themeStorage'

function renderWithTheme(initialTheme?: 'dark' | 'light') {
  if (initialTheme) localStorage.setItem(STORAGE_KEY, initialTheme)
  return render(
    <ThemeProvider>
      <ThemeToggle />
    </ThemeProvider>
  )
}

describe('ThemeToggle', () => {
  beforeEach(() => {
    localStorage.clear()
    document.documentElement.classList.remove('dark')
    vi.mocked(window.matchMedia).mockImplementation((query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    }))
  })

  it('renders a single button', () => {
    renderWithTheme('dark')
    expect(screen.getByRole('button')).toBeInTheDocument()
  })

  it('shows Moon icon in light theme (action = switch to dark)', () => {
    renderWithTheme('light')
    // Moon icon: lucide renders an svg; find it via aria-label on the button
    const btn = screen.getByRole('button')
    expect(btn).toHaveAttribute('aria-label', 'Switch to dark theme')
  })

  it('shows Sun icon in dark theme (action = switch to light)', () => {
    renderWithTheme('dark')
    const btn = screen.getByRole('button')
    expect(btn).toHaveAttribute('aria-label', 'Switch to light theme')
  })

  it('aria-pressed is true when dark theme is active', () => {
    renderWithTheme('dark')
    expect(screen.getByRole('button')).toHaveAttribute('aria-pressed', 'true')
  })

  it('aria-pressed is false when light theme is active', () => {
    renderWithTheme('light')
    expect(screen.getByRole('button')).toHaveAttribute('aria-pressed', 'false')
  })

  it('click toggles from dark to light: class removed and storage updated', async () => {
    const user = userEvent.setup()
    renderWithTheme('dark')
    await user.click(screen.getByRole('button'))
    expect(document.documentElement.classList.contains('dark')).toBe(false)
    expect(localStorage.getItem(STORAGE_KEY)).toBe('light')
  })

  it('click toggles from light to dark: class added and storage updated', async () => {
    const user = userEvent.setup()
    renderWithTheme('light')
    await user.click(screen.getByRole('button'))
    expect(document.documentElement.classList.contains('dark')).toBe(true)
    expect(localStorage.getItem(STORAGE_KEY)).toBe('dark')
  })

  it('aria-label updates after toggle', async () => {
    const user = userEvent.setup()
    renderWithTheme('dark')
    await user.click(screen.getByRole('button'))
    expect(screen.getByRole('button')).toHaveAttribute('aria-label', 'Switch to dark theme')
  })

  it('aria-pressed updates after toggle', async () => {
    const user = userEvent.setup()
    renderWithTheme('dark')
    await user.click(screen.getByRole('button'))
    expect(screen.getByRole('button')).toHaveAttribute('aria-pressed', 'false')
  })

  it('keyboard Enter activates toggle', async () => {
    const user = userEvent.setup()
    renderWithTheme('dark')
    screen.getByRole('button').focus()
    await user.keyboard('{Enter}')
    expect(document.documentElement.classList.contains('dark')).toBe(false)
  })

  it('keyboard Space activates toggle', async () => {
    const user = userEvent.setup()
    renderWithTheme('dark')
    screen.getByRole('button').focus()
    await user.keyboard(' ')
    expect(document.documentElement.classList.contains('dark')).toBe(false)
  })

  it('button is in tab order (not tabIndex -1)', () => {
    renderWithTheme('dark')
    const btn = screen.getByRole('button')
    expect(btn).not.toHaveAttribute('tabindex', '-1')
  })

  it('icon is decorative (aria-hidden)', () => {
    renderWithTheme('dark')
    const icons = document.querySelectorAll('svg')
    icons.forEach((icon) => {
      expect(icon).toHaveAttribute('aria-hidden', 'true')
    })
  })
})
