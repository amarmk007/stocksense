import { useState, useCallback } from 'react'
import { AnimatePresence, motion } from 'framer-motion'
import { useNavigate } from 'react-router-dom'
import { apiClient } from '../api/client'
import { useAuth } from '../hooks/useAuth'
import { useRecommendationsStatus } from '../hooks/useRecommendations'

const variants = {
  enter: (dir: number) => ({ x: dir > 0 ? '100%' : '-100%', opacity: 0 }),
  center: { x: 0, opacity: 1 },
  exit: (dir: number) => ({ x: dir < 0 ? '100%' : '-100%', opacity: 0 }),
}

const transition = { type: 'tween', duration: 0.3, ease: 'easeInOut' as const }

function formatCurrency(v: number) {
  return v >= 1_000_000 ? '$1M' : `$${(v / 1000).toFixed(0)}k`
}

export default function OnboardingFlow() {
  const navigate = useNavigate()
  const { setIsOnboarded } = useAuth()

  const [step, setStep] = useState(0)
  const [dir, setDir] = useState(1)
  const [loading, setLoading] = useState(false)
  const [polling, setPolling] = useState(false)

  const [amount, setAmount] = useState(50_000)
  const [timeline, setTimeline] = useState(5)
  const [returnPct, setReturnPct] = useState(20)
  const [experience, setExperience] = useState<'Novice' | 'Experienced' | null>(null)

  const allAnswered = experience !== null

  const onReady = useCallback(() => {
    setIsOnboarded(true)
    navigate('/dashboard')
  }, [navigate, setIsOnboarded])

  useRecommendationsStatus(polling, onReady)

  async function handleSubmit() {
    setLoading(true)
    try {
      await apiClient.post('/profile', {
        investmentAmount: amount,
        timelineYears: timeline,
        expectedReturnPct: returnPct,
        experienceLevel: experience,
      })
      await apiClient.post('/recommendations/generate', {})
      setIsOnboarded(true)
      setLoading(false)
      setPolling(true)
    } catch {
      setLoading(false)
    }
  }

  function go(next: number) {
    setDir(next > step ? 1 : -1)
    setStep(next)
  }

  if (loading || polling) {
    return (
      <div className="min-h-screen bg-gray-950 flex flex-col items-center justify-center gap-4">
        <div className="w-10 h-10 border-4 border-indigo-500 border-t-transparent rounded-full animate-spin" />
        <p className="text-white text-lg font-medium">Analyzing markets for you...</p>
        <p className="text-gray-500 text-sm">This takes about 20–30 seconds</p>
      </div>
    )
  }

  const screens = [
    <Screen key="amount" title="How much are you investing?">
      <SliderField
        value={amount}
        min={1_000}
        max={1_000_000}
        step={10_000}
        display={formatCurrency(amount)}
        onChange={setAmount}
      />
    </Screen>,

    <Screen key="timeline" title="What's your investment timeline?">
      <SliderField
        value={timeline}
        min={1}
        max={10}
        step={1}
        display={`${timeline} yr${timeline > 1 ? 's' : ''}`}
        onChange={setTimeline}
      />
    </Screen>,

    <Screen key="return" title="What return are you targeting?">
      <SliderField
        value={returnPct}
        min={5}
        max={100}
        step={5}
        display={`${returnPct}%`}
        onChange={setReturnPct}
      />
    </Screen>,

    <Screen key="experience" title="How experienced are you?">
      <div className="flex gap-4 mt-6">
        {(['Novice', 'Experienced'] as const).map(opt => (
          <button
            key={opt}
            onClick={() => setExperience(opt)}
            className={`flex-1 py-4 rounded-xl font-semibold text-lg transition ${
              experience === opt
                ? 'bg-indigo-600 text-white'
                : 'bg-gray-800 text-gray-300 hover:bg-gray-700'
            }`}
          >
            {opt}
          </button>
        ))}
      </div>
      {experience === 'Novice' && (
        <p className="text-gray-400 text-sm mt-3 text-center">
          Plain-English explanations — no jargon.
        </p>
      )}
      {experience === 'Experienced' && (
        <p className="text-gray-400 text-sm mt-3 text-center">
          Data-dense analysis with technical signals.
        </p>
      )}
    </Screen>,
  ]

  return (
    <div className="min-h-screen bg-gray-950 flex flex-col items-center justify-center px-6">
      <div className="w-full max-w-sm">
        {/* Progress dots */}
        <div className="flex justify-center gap-2 mb-10">
          {screens.map((_, i) => (
            <div
              key={i}
              className={`h-1.5 rounded-full transition-all ${
                i === step ? 'bg-indigo-500 w-6' : i < step ? 'bg-indigo-800 w-3' : 'bg-gray-700 w-3'
              }`}
            />
          ))}
        </div>

        {/* Slide area */}
        <div className="relative overflow-hidden" style={{ minHeight: 200 }}>
          <AnimatePresence custom={dir} mode="popLayout">
            <motion.div
              key={step}
              custom={dir}
              variants={variants}
              initial="enter"
              animate="center"
              exit="exit"
              transition={transition}
            >
              {screens[step]}
            </motion.div>
          </AnimatePresence>
        </div>

        {/* Nav */}
        <div className="flex gap-3 mt-10">
          {step > 0 && (
            <button
              onClick={() => go(step - 1)}
              className="flex-1 py-3 rounded-xl bg-gray-800 text-gray-300 font-semibold hover:bg-gray-700 transition"
            >
              Back
            </button>
          )}
          {step < screens.length - 1 ? (
            <button
              onClick={() => go(step + 1)}
              className="flex-1 py-3 rounded-xl bg-indigo-600 text-white font-semibold hover:bg-indigo-500 transition"
            >
              Next
            </button>
          ) : (
            <button
              onClick={handleSubmit}
              disabled={!allAnswered}
              className={`flex-1 py-3 rounded-xl font-semibold transition ${
                allAnswered
                  ? 'bg-indigo-600 text-white hover:bg-indigo-500'
                  : 'bg-gray-800 text-gray-600 cursor-not-allowed'
              }`}
            >
              Get Recommendations
            </button>
          )}
        </div>
      </div>
    </div>
  )
}

function Screen({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div>
      <h2 className="text-2xl font-bold text-white text-center">{title}</h2>
      {children}
    </div>
  )
}

function SliderField({
  value, min, max, step, display, onChange,
}: {
  value: number; min: number; max: number; step: number
  display: string; onChange: (v: number) => void
}) {
  return (
    <div className="mt-8">
      <p className="text-center text-4xl font-bold text-indigo-400 mb-6">{display}</p>
      <input
        type="range"
        min={min}
        max={max}
        step={step}
        value={value}
        onChange={e => onChange(Number(e.target.value))}
        className="w-full accent-indigo-500 cursor-pointer"
      />
      <div className="flex justify-between text-gray-500 text-sm mt-2">
        <span>{min >= 1000 ? formatCurrencyRaw(min) : `${min}${typeof display === 'string' && display.endsWith('%') ? '%' : ''}`}</span>
        <span>{max >= 1000 ? formatCurrencyRaw(max) : `${max}${typeof display === 'string' && display.endsWith('%') ? '%' : ''}`}</span>
      </div>
    </div>
  )
}

function formatCurrencyRaw(v: number) {
  return v >= 1_000_000 ? '$1M' : v >= 1000 ? `$${v / 1000}k` : `$${v}`
}
