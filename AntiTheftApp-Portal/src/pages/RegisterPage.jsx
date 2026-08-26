import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { register } from '../api.js'
import Logo from '../components/Logo.jsx'

export default function RegisterPage() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [pin, setPin] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()

  async function handleSubmit(e) {
    e.preventDefault()
    setError('')

    if (password.length < 8) {
      setError('Password needs to be at least 8 characters.')
      return
    }
    if (password !== confirmPassword) {
      setError("Passwords don't match.")
      return
    }
    if (pin.length < 4 || pin.length > 8) {
      setError('PIN needs to be 4–8 digits.')
      return
    }

    setLoading(true)
    try {
      const result = await register(email, password, pin)
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
        <h1>Create your console</h1>
        <p className="subtitle">
          Set up your account here first, then sign in with the same email and password on the mobile app to
          bring a device online.
        </p>

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
              placeholder="At least 8 characters"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="new-password"
              minLength={8}
              required
            />
          </div>
          <div className="field">
            <label htmlFor="confirmPassword">Confirm password</label>
            <input
              id="confirmPassword"
              type="password"
              placeholder="Type it again"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              autoComplete="new-password"
              required
            />
          </div>
          <div className="field">
            <label htmlFor="pin">Device PIN</label>
            <input
              id="pin"
              type="password"
              inputMode="numeric"
              placeholder="4–8 digits"
              value={pin}
              onChange={(e) => setPin(e.target.value.replace(/\D/g, ''))}
              minLength={4}
              maxLength={8}
              required
            />
          </div>
          <p className="subtitle" style={{ marginTop: -6, marginBottom: 16, fontSize: 12.5 }}>
            This PIN is typed on the phone itself to silence the alarm — keep it separate from your password.
          </p>

          {error && <div className="banner error">{error}</div>}

          <button type="submit" className="btn btn-primary" disabled={loading}>
            {loading ? 'Creating account…' : 'Create account'}
          </button>
        </form>

        <div className="logout-row" style={{ marginTop: 20 }}>
          <Link to="/" className="btn-ghost btn" style={{ textAlign: 'center', textDecoration: 'none' }}>
            Already have an account? Sign in
          </Link>
        </div>
      </div>
    </div>
  )
}