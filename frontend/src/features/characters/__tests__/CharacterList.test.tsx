import { render, screen, waitFor } from '@testing-library/react'
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/tests/server'
import { createWrapper } from '@/tests/testUtils'
import { CharacterList } from '../components/CharacterList'

const mockCharacters = [
  { id: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11', name: 'G8trdone', handle: 'g8r', createdAt: new Date().toISOString() },
  { id: 'b1ffcd00-0d1c-4fa9-bc7e-7cc0ce491b22', name: 'AlphaPilot', handle: 'alphapilot', createdAt: new Date().toISOString() },
]

describe('CharacterList', () => {
  it('renders each character with name and handle', async () => {
    server.use(
      http.get('/api/characters/', () =>
        HttpResponse.json({ characters: mockCharacters })
      )
    )

    render(<CharacterList />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText('G8trdone')).toBeDefined()
      expect(screen.getByText('g8r')).toBeDefined()
      expect(screen.getByText('AlphaPilot')).toBeDefined()
      expect(screen.getByText('alphapilot')).toBeDefined()
    })
  })

  it('renders empty state when character list is empty', async () => {
    server.use(
      http.get('/api/characters/', () =>
        HttpResponse.json({ characters: [] })
      )
    )

    render(<CharacterList />, { wrapper: createWrapper() })

    await waitFor(() => {
      expect(screen.getByText(/no characters registered/i)).toBeDefined()
    })
  })
})
