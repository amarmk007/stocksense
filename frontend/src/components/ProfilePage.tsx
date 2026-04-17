import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { apiClient } from '../api/client'

interface Profile {
  investmentAmount: number
  timelineYears: number
  expectedReturnPct: number
  experienceLevel: 'Novice' | 'Experienced'
}

function formatCurrency(v: number) {
  return v >= 1_000_000 ? '$1M' : `$${(v / 1000).toFixed(0)}k`
}

export default function ProfilePage() {
  const [profile, setProfile] = useState<Profile | null>(null)
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    apiClient.get<Profile>('/profile')
      .then(r => setProfile(r.data))
      .catch(() => setError('Failed to load profile.'))
  }, [])

  async function handleSave() {
    if (!profile) return
    setSaving(true)
    setSaved(false)
    try {
      await apiClient.patch('/profile', profile)
      setSaved(true)
    } catch {
      setError('Failed to save changes.')
    } finally {
      setSaving(false)
    }
  }

  if (!profile && !error) {
    return (
      <div className="min-h-screen bg-gray-950 flex items-center justify-center">
        <div className="w-8 h-8 border-4 border-indigo-500 border-t-transparent rounded-full animate-spin" />
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-950">
      <header className="border-b border-gray-800 px-6 py-4 flex items-center justify-between">
        <Link to="/dashboard" className="text-gray-400 hover:text-white text-sm transition flex items-center gap-1">
          ← Dashboard
        </Link>
        <h1 className="text-white font-bold">Profile</h1>
        <div className="w-20" />
      </header>

      <main className="max-w-sm mx-auto px-6 py-8 space-y-8">
        {error && <p className="text-red-400 text-sm">{error}</p>}

        {profile && (
          <>
            {/* Investment Amount */}
            <div>
              <label className="block text-gray-400 text-sm mb-3">Investment Amount</label>
              <p className="text-center text-3xl font-bold text-indigo-400 mb-4">
                {formatCurrency(profile.investmentAmount)}
              </p>
              <input
                type="range" min={1000} max={1_000_000} step={10_000}
                value={profile.investmentAmount}
                onChange={e => setProfile({ ...profile, investmentAmount: Number(e.target.value) })}
                className="w-full accent-indigo-500"
              />
            </div>

            {/* Timeline */}
            <div>
              <label className="block text-gray-400 text-sm mb-3">Timeline</label>
              <p className="text-center text-3xl font-bold text-indigo-400 mb-4">
                {profile.timelineYears} yr{profile.timelineYears > 1 ? 's' : ''}
              </p>
              <input
                type="range" min={1} max={10} step={1}
                value={profile.timelineYears}
                onChange={e => setProfile({ ...profile, timelineYears: Number(e.target.value) })}
                className="w-full accent-indigo-500"
              />
            </div>

            {/* Expected Return */}
            <div>
              <label className="block text-gray-400 text-sm mb-3">Target Return</label>
              <p className="text-center text-3xl font-bold text-indigo-400 mb-4">
                {profile.expectedReturnPct}%
              </p>
              <input
                type="range" min={5} max={100} step={5}
                value={profile.expectedReturnPct}
                onChange={e => setProfile({ ...profile, expectedReturnPct: Number(e.target.value) })}
                className="w-full accent-indigo-500"
              />
            </div>

            {/* Experience Level */}
            <div>
              <label className="block text-gray-400 text-sm mb-3">Experience Level</label>
              <div className="flex gap-3">
                {(['Novice', 'Experienced'] as const).map(opt => (
                  <button
                    key={opt}
                    onClick={() => setProfile({ ...profile, experienceLevel: opt })}
                    className={`flex-1 py-3 rounded-xl font-semibold transition ${
                      profile.experienceLevel === opt
                        ? 'bg-indigo-600 text-white'
                        : 'bg-gray-800 text-gray-300 hover:bg-gray-700'
                    }`}
                  >
                    {opt}
                  </button>
                ))}
              </div>
            </div>

            {/* Save */}
            <button
              onClick={handleSave}
              disabled={saving}
              className="w-full py-3 rounded-xl bg-indigo-600 text-white font-semibold hover:bg-indigo-500 disabled:opacity-50 transition"
            >
              {saving ? 'Saving...' : 'Save Changes'}
            </button>

            {saved && (
              <p className="text-center text-sm text-gray-400">
                Changes will apply at tomorrow's refresh.
              </p>
            )}
          </>
        )}
      </main>
    </div>
  )
}
