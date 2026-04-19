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
  const [generating, setGenerating] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    async function load() {
      try {
        const r = await apiClient.get<RecommendationData>('/recommendations')
        if (!cancelled) setData(r.data)
      } catch (err: any) {
        if (cancelled) return
        if (err?.response?.status === 404) {
          // No recommendations yet — trigger generation and poll
          setGenerating(true)
          try {
            await apiClient.post('/recommendations/generate', {})
          } catch {
            // ignore if already generating
          }
          const interval = setInterval(async () => {
            try {
              const { data: status } = await apiClient.get<{ status: string }>('/recommendations/status')
              if (status.status === 'ready') {
                clearInterval(interval)
                const r = await apiClient.get<RecommendationData>('/recommendations')
                if (!cancelled) {
                  setData(r.data)
                  setGenerating(false)
                }
              }
            } catch {
              // keep polling
            }
          }, 3000)
          return
        }
        setError('Unable to load recommendations.')
      } finally {
        if (!cancelled && !generating) setLoading(false)
      }
    }

    load()
    return () => { cancelled = true }
  }, [])

  return { data, loading: loading || generating, error }
}
