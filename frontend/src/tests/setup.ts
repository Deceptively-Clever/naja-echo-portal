import '@testing-library/jest-dom'
import { afterEach, beforeAll, afterAll, vi } from 'vitest'
import { cleanup } from '@testing-library/react'
import { server } from './server'

beforeAll(() => server.listen({ onUnhandledRequest: 'warn' }))
afterEach(() => {
  cleanup()
  server.resetHandlers()
})
afterAll(() => server.close())

// Node.js v22+ experimental localStorage shadows jsdom's; provide a proper Map-backed mock.
const localStorageStore = new Map<string, string>()
const localStorageMock: Storage = {
  getItem: (key) => localStorageStore.get(key) ?? null,
  setItem: (key, value) => { localStorageStore.set(key, value) },
  removeItem: (key) => { localStorageStore.delete(key) },
  clear: () => { localStorageStore.clear() },
  get length() { return localStorageStore.size },
  key: (index) => [...localStorageStore.keys()][index] ?? null,
}
vi.stubGlobal('localStorage', localStorageMock)
afterEach(() => { localStorageStore.clear() })

// jsdom does not implement IntersectionObserver; provide a no-op mock.
// Tests that need to assert scroll behavior override this in their own beforeEach.
if (!window.IntersectionObserver) {
  class NoopIntersectionObserver {
    observe = vi.fn()
    disconnect = vi.fn()
    unobserve = vi.fn()
  }
  vi.stubGlobal('IntersectionObserver', NoopIntersectionObserver)
}

// jsdom does not implement matchMedia; provide a default mock (matches: false).
// Tests that need a specific system preference can override window.matchMedia locally.
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation((query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
})
