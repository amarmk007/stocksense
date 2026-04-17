export default function Auth() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-950">
      <div className="text-center">
        <h1 className="text-3xl font-bold text-white mb-2">StockSense</h1>
        <p className="text-gray-400 mb-8">AI-powered stock research, finally in plain English.</p>
        <a
          href="/api/auth/google"
          className="inline-block bg-white text-gray-900 font-semibold px-6 py-3 rounded-lg hover:bg-gray-100 transition"
        >
          Sign in with Google
        </a>
      </div>
    </div>
  )
}
