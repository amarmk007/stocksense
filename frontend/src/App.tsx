import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider, useAuth } from './hooks/useAuth'
import Auth from './pages/Auth'
import Dashboard from './pages/Dashboard'
import OnboardingFlow from './components/OnboardingFlow'
import ProfilePage from './components/ProfilePage'

function AppRoutes() {
  const { token, isOnboarded } = useAuth()

  if (!token) return <Navigate to="/auth" replace />
  if (!isOnboarded) return <Navigate to="/onboarding" replace />
  return <Navigate to="/dashboard" replace />
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/auth" element={<Auth />} />
          <Route path="/onboarding" element={<OnboardingFlow />} />
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/profile" element={<ProfilePage />} />
          <Route path="*" element={<AppRoutes />} />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  )
}
