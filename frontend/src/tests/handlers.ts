import { http, HttpResponse } from 'msw'

export const authenticatedUser = {
  id: 'a0eebc99-9c0b-4ef8-bb6d-6bb9bd380a11',
  displayName: 'Test User',
  avatarUrl: 'https://cdn.discordapp.com/avatars/123/abc.png',
}

export const handlers = [
  http.get('/api/auth/me', () => HttpResponse.json(authenticatedUser)),
  http.post('/api/auth/signout', () => new HttpResponse(null, { status: 204 })),
  http.get('/api/auth/discord/login', () =>
    new HttpResponse(null, {
      status: 302,
      headers: { Location: 'https://discord.com/oauth2/authorize' },
    })
  ),
  http.get('/api/health', () => HttpResponse.json({ status: 'ok' })),
]
