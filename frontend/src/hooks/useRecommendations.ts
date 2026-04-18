import { useState, useEffect, useRef } from 'react'
import { apiClient } from '../api/client'

export interface RecommendationData {
  isStale: boolean
  generatedAt: string
  recommendations: {
    ticker: string
    name: string
    upsideEstimate: string
    reasoning: string
    signals: { analyst: string[]; macro: string[]; market: string[] }
    sources: { title: string; url: string }[]
  }[]
}

export function useRecommendationsStatus(enabled: boolean, onReady: () => void) {
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null)
  const onReadyRef = useRef(onReady)
  onReadyRef.current = onReady

  useEffect(() => {
    if (!enabled) return
    intervalRef.current = setInterval(async () => {
      try {
        const { data } = await apiClient.get<{ status: string }>('/recommendations/status')
        if (data.status === 'ready') {
          clearInterval(intervalRef.current!)
          onReadyRef.current()
        }
      } catch {
        // keep polling
      }
    }, 3000)
    return () => clearInterval(intervalRef.current!)
  }, [enabled])
}

export function useRecommendations() {
  const [data, setData] = useState<RecommendationData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    apiClient.get<RecommendationData>('/recommendations')
      .then(r => setData(r.data))
      .catch(() => setError('Unable to load recommendations.'))
      .finally(() => setLoading(false))
  }, [])

  return { data, loading, error }
}
