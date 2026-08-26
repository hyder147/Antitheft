import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { login } from '../api.js'
import Logo from '../components/Logo.jsx'

export default function LoginPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()

  async function handleSubmit(e) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const result = await login(email, password)
      localStorage.setItem('antitheftapp_token', result.token)
      navigate('/devices')
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-wrap">
      <div className="auth-card">
        <div className="auth-logo">
          <Logo size={40} />
        </div>
        <div className="eyebrow" style={{ justifyContent: 'center', width: '100%', marginBottom: 4 }}>
          <span className="dot" />
          SECURITY CONSOLE
        </div>
        <h1>Sign in to your console</h1>
        <p className="subtitle">Locate, sound, or silence the alarm on any device you protect.</p>

        <form onSubmit={handleSubmit}>
          <div className="field">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              placeholder="you@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              autoComplete="email"
              required
            />
          </div>
          <div className="field">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              placeholder="••••••••"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="current-password"
              required
            />
          </div>

          {error && <div className="banner error">{error}</div>}

          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? 'Signing in…' : 'Access console'}
          </button>
        </form>

        <div className="logout-row" style={{ marginTop: 20 }}>
          <Link to="/register" className="btn-ghost btn" style={{ textAlign: 'center', textDecoration: 'none' }}>
            New here? Create an account
          </Link>
        </div>
      </div>
    </div>
  )
}