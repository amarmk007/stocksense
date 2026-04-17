import { useState } from 'react'
import { AnimatePresence, motion } from 'framer-motion'

interface Signals {
  analyst: string[]
  macro: string[]
  market: string[]
}

interface Source {
  title: string
  url: string
}

interface RecommendationItem {
  ticker: string
  name: string
  upsideEstimate: string
  reasoning: string
  signals: Signals
  sources: Source[]
}

function UpsideBadge({ value }: { value: string }) {
  const positive = !value.startsWith('-')
  return (
    <span className={`text-sm font-bold px-2 py-0.5 rounded-full ${
      positive ? 'bg-green-900/60 text-green-400' : 'bg-red-900/60 text-red-400'
    }`}>
      {value}
    </span>
  )
}

function SignalSection({ label, items }: { label: string; items: string[] }) {
  if (!items?.length) return null
  return (
    <div>
      <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1.5">{label}</p>
      <ul className="space-y-1">
        {items.map((item, i) => (
          <li key={i} className="text-sm text-gray-300 flex gap-2">
            <span className="text-indigo-500 mt-0.5 shrink-0">·</span>
            <span>{item}</span>
          </li>
        ))}
      </ul>
    </div>
  )
}

export default function RecommendationCard({ item }: { item: RecommendationItem }) {
  const [open, setOpen] = useState(false)

  return (
    <div
      className="bg-gray-900 rounded-2xl border border-gray-800 overflow-hidden cursor-pointer hover:border-gray-700 transition"
      onClick={() => setOpen(o => !o)}
    >
      {/* Collapsed header — always visible */}
      <div className="flex items-center justify-between px-5 py-4">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-indigo-900/50 flex items-center justify-center">
            <span className="text-indigo-300 font-bold text-xs">{item.ticker.slice(0, 4)}</span>
          </div>
          <div>
            <p className="text-white font-semibold leading-tight">{item.ticker}</p>
            <p className="text-gray-500 text-xs">{item.name}</p>
          </div>
        </div>
        <div className="flex items-center gap-3">
          <UpsideBadge value={item.upsideEstimate} />
          <svg
            className={`w-4 h-4 text-gray-500 transition-transform ${open ? 'rotate-180' : ''}`}
            fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}
          >
            <path strokeLinecap="round" strokeLinejoin="round" d="M19 9l-7 7-7-7" />
          </svg>
        </div>
      </div>

      {/* Expanded content */}
      <AnimatePresence initial={false}>
        {open && (
          <motion.div
            key="content"
            initial={{ height: 0, opacity: 0 }}
            animate={{ height: 'auto', opacity: 1 }}
            exit={{ height: 0, opacity: 0 }}
            transition={{ duration: 0.25, ease: 'easeInOut' }}
          >
            <div className="px-5 pb-5 pt-1 space-y-5 border-t border-gray-800">
              {/* Reasoning */}
              <div>
                <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1.5">Analysis</p>
                <p className="text-sm text-gray-300 leading-relaxed">{item.reasoning}</p>
              </div>

              {/* Signals */}
              <div className="space-y-4">
                <SignalSection label="Analyst" items={item.signals?.analyst} />
                <SignalSection label="Macro" items={item.signals?.macro} />
                <SignalSection label="Market" items={item.signals?.market} />
              </div>

              {/* Sources */}
              {item.sources?.length > 0 && (
                <div>
                  <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider mb-1.5">Sources</p>
                  <div className="space-y-1">
                    {item.sources.map((s, i) => (
                      <a
                        key={i}
                        href={s.url}
                        target="_blank"
                        rel="noopener noreferrer"
                        onClick={e => e.stopPropagation()}
                        className="block text-xs text-indigo-400 hover:text-indigo-300 underline truncate transition"
                      >
                        {s.title}
                      </a>
                    ))}
                  </div>
                </div>
              )}
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
