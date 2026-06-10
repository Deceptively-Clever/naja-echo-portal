import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from '@/lib/queryClient'
import { AppRouter } from '@/routes/AppRouter'

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AppRouter />
    </QueryClientProvider>
  )
}

export default App
