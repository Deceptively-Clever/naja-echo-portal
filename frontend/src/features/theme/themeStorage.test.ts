import { describe, it, expect, beforeEach, vi } from 'vitest'
import {
  getStoredTheme,
  setStoredTheme,
  resolveInitialTheme,
  applyTheme,
  STORAGE_KEY,
} from './themeStorage'

describe('themeStorage', () => {
  beforeEach(() => {
    localStorage.clear()
    document.documentElement.classList.remove('dark')
  })

  describe('getStoredTheme', () => {
    it('returns null when key is absent', () => {
      expect(getStoredTheme()).toBeNull()
    })

    it('returns "dark" when stored value is "dark"', () => {
      localStorage.setItem(STORAGE_KEY, 'dark')
      expect(getStoredTheme()).toBe('dark')
    })

    it('returns "light" when stored value is "light"', () => {
      localStorage.setItem(STORAGE_KEY, 'light')
      expect(getStoredTheme()).toBe('light')
    })

    it('returns null for invalid stored value', () => {
      localStorage.setItem(STORAGE_KEY, 'purple')
      expect(getStoredTheme()).toBeNull()
    })

    it('returns null for empty string', () => {
      localStorage.setItem(STORAGE_KEY, '')
      expect(getStoredTheme()).toBeNull()
    })

    it('returns null for "system" (not a valid stored value)', () => {
      localStorage.setItem(STORAGE_KEY, 'system')
      expect(getStoredTheme()).toBeNull()
    })
  })

  describe('setStoredTheme', () => {
    it('writes "dark" to localStorage', () => {
      setStoredTheme('dark')
      expect(localStorage.getItem(STORAGE_KEY)).toBe('dark')
    })

    it('writes "light" to localStorage', () => {
      setStoredTheme('light')
      expect(localStorage.getItem(STORAGE_KEY)).toBe('light')
    })

    it('round-trips: set then get returns same value', () => {
      setStoredTheme('light')
      expect(getStoredTheme()).toBe('light')

      setStoredTheme('dark')
      expect(getStoredTheme()).toBe('dark')
    })
  })

  describe('resolveInitialTheme', () => {
    beforeEach(() => {
      // Reset to the default mock (matches: false) after any per-test override
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

    it('returns stored "light" when present', () => {
      localStorage.setItem(STORAGE_KEY, 'light')
      expect(resolveInitialTheme()).toBe('light')
    })

    it('returns stored "dark" when present', () => {
      localStorage.setItem(STORAGE_KEY, 'dark')
      expect(resolveInitialTheme()).toBe('dark')
    })

    it('returns "light" from system preference when no stored value', () => {
      vi.mocked(window.matchMedia).mockImplementation((query: string) => ({
        matches: query === '(prefers-color-scheme: light)',
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      }))
      expect(resolveInitialTheme()).toBe('light')
    })

    it('returns flagship "dark" default when no stored value and no system preference', () => {
      // matchMedia mock defaults to matches: false (from setup.ts)
      expect(resolveInitialTheme()).toBe('dark')
    })

    it('ignores corrupted stored value and falls through to system/default', () => {
      localStorage.setItem(STORAGE_KEY, 'corrupted')
      // matchMedia default: matches: false → flagship dark
      expect(resolveInitialTheme()).toBe('dark')
    })

    it('ignores corrupted stored value and uses system light preference', () => {
      localStorage.setItem(STORAGE_KEY, 'invalid')
      vi.mocked(window.matchMedia).mockImplementation((query: string) => ({
        matches: query === '(prefers-color-scheme: light)',
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      }))
      expect(resolveInitialTheme()).toBe('light')
    })

    it('stored value takes precedence over system preference', () => {
      localStorage.setItem(STORAGE_KEY, 'light')
      vi.mocked(window.matchMedia).mockImplementation((query: string) => ({
        matches: query !== '(prefers-color-scheme: light)', // system says dark
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      }))
      expect(resolveInitialTheme()).toBe('light')
    })
  })

  describe('applyTheme', () => {
    it('adds "dark" class to documentElement when theme is "dark"', () => {
      applyTheme('dark')
      expect(document.documentElement.classList.contains('dark')).toBe(true)
    })

    it('removes "dark" class from documentElement when theme is "light"', () => {
      document.documentElement.classList.add('dark')
      applyTheme('light')
      expect(document.documentElement.classList.contains('dark')).toBe(false)
    })

    it('is idempotent: applying "dark" twice leaves class present once', () => {
      applyTheme('dark')
      applyTheme('dark')
      expect(document.documentElement.classList.contains('dark')).toBe(true)
    })

    it('is idempotent: applying "light" when already light leaves no class', () => {
      applyTheme('light')
      applyTheme('light')
      expect(document.documentElement.classList.contains('dark')).toBe(false)
    })
  })
})
