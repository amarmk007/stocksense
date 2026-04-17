import { createContext, useContext, useState, useEffect, ReactNode, createElement } from 'react'
import { setAuthToken } from '../api/client'

interface AuthContextType {
  token: string | null
  isOnboarded: boolean
  setToken: (token: string | null) => void
  setIsOnboarded: (v: boolean) => void
}

const AuthContext = createContext<AuthContextType | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setTokenState] = useState<string | null>(null)
  const [isOnboarded, setIsOnboarded] = useState(false)

  useEffect(() => {
    const hash = window.location.hash
    if (hash.startsWith('#token=')) {
      const t = hash.slice(7)
      setToken(t)
      window.history.replaceState(null, '', window.location.pathname)
    } else if (!token) {
      window.location.href = '/api/auth/google'
    }
  }, [])

  function setToken(t: string | null) {
    setTokenState(t)
    setAuthToken(t)
    if (t) {
      try {
        const payload = JSON.parse(atob(t.split('.')[1]))
        setIsOnboarded(payload.IsOnboarded === 'true' || payload.IsOnboarded === true)
      } catch {
        // ignore malformed token
      }
    }
  }

  return createElement(AuthContext.Provider, { value: { token, isOnboarded, setToken, setIsOnboarded } }, children)
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
