import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from '@/lib/queryClient'
import { AppRouter } from '@/routes/AppRouter'
import { ThemeProvider } from '@/features/theme/ThemeProvider'

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider>
        <AppRouter />
      </ThemeProvider>
    </QueryClientProvider>
  )
}

export default App
