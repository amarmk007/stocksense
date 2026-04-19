import { Link } from 'react-router-dom'
import { useRecommendations } from '../hooks/useRecommendations'
import RecommendationCard from '../components/RecommendationCard'
import StaleBanner from '../components/StaleBanner'

export default function Dashboard() {
  const { data, loading, error } = useRecommendations()

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-950 flex flex-col items-center justify-center gap-4">
        <div className="w-10 h-10 border-4 border-indigo-500 border-t-transparent rounded-full animate-spin" />
        <p className="text-white text-lg font-medium">Analyzing markets for you...</p>
        <p className="text-gray-500 text-sm">This takes about 20–30 seconds</p>
      </div>
    )
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gray-950 flex items-center justify-center px-6">
        <div className="text-center">
          <p className="text-red-400 text-lg">Something went wrong loading your recommendations.</p>
          <p className="text-gray-500 text-sm mt-2">Try refreshing the page.</p>
        </div>
      </div>
    )
  }

  if (!data) return null

  return (
    <div className="min-h-screen bg-gray-950">
      <header className="border-b border-gray-800 px-6 py-4 flex items-center justify-between">
        <h1 className="text-white font-bold text-xl">StockSense</h1>
        <Link to="/profile" className="text-gray-400 hover:text-white text-sm transition">
          Profile
        </Link>
      </header>

      <main className="max-w-2xl mx-auto px-4 py-6 space-y-4">
        {data.isStale && <StaleBanner />}

        <div className="flex items-center justify-between mb-2">
          <h2 className="text-white font-semibold text-lg">
            Today's Picks
          </h2>
          <span className="text-gray-500 text-xs">
            {new Date(data.generatedAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
          </span>
        </div>

        {data.recommendations.map((item: any, i: number) => (
          <RecommendationCard key={item.ticker ?? i} item={item} />
        ))}
      </main>
    </div>
  )
}
