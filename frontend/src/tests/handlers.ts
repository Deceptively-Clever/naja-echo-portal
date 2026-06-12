import { http, HttpResponse } from 'msw'

export const authenticatedSession = {
  authenticated: true as const,
  user: {
    id: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
    displayName: 'Test User',
    discordUsername: 'testuser',
  },
}

export const anonymousSession = {
  authenticated: false as const,
}

export const handlers = [
  http.get('/api/auth/me', () => HttpResponse.json(authenticatedSession)),
  http.post('/api/auth/signout', () => new HttpResponse(null, { status: 204 })),
  http.get('/api/auth/discord/login', () =>
    new HttpResponse(null, {
      status: 302,
      headers: { Location: 'https://discord.com/oauth2/authorize' },
    })
  ),
  http.get('/api/health', () => HttpResponse.json({ status: 'ok' })),
]
