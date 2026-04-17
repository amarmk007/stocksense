import { useState, useEffect, useRef } from 'react'
import { apiClient } from '../api/client'

export function useRecommendationsStatus(enabled: boolean, onReady: () => void) {
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null)

  useEffect(() => {
    if (!enabled) return
    intervalRef.current = setInterval(async () => {
      try {
        const { data } = await apiClient.get<{ status: string }>('/recommendations/status')
        if (data.status === 'ready') {
          clearInterval(intervalRef.current!)
          onReady()
        }
      } catch {
        // keep polling
      }
    }, 3000)
    return () => clearInterval(intervalRef.current!)
  }, [enabled, onReady])
}

export function useRecommendations() {
  const [data, setData] = useState<any>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    apiClient.get('/recommendations')
      .then(r => setData(r.data))
      .catch(() => setError('Unable to load recommendations. Please try again.'))
      .finally(() => setLoading(false))
  }, [])

  return { data, loading, error }
}
