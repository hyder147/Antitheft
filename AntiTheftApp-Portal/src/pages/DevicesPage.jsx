import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { getDevices, triggerCommand } from '../api.js'
import Sonar from '../components/Sonar.jsx'
import Logo from '../components/Logo.jsx'
import { timeAgo } from '../lib/timeAgo.js'

export default function DevicesPage() {
  const [devices, setDevices] = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [statusByDevice, setStatusByDevice] = useState({})
  const navigate = useNavigate()
  const token = localStorage.getItem('antitheftapp_token')

  useEffect(() => {
    if (!token) {
      navigate('/')
      return
    }
    loadDevices()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  async function loadDevices() {
    setLoading(true)
    setError('')
    try {
      const result = await getDevices(token)
      setDevices(result)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  async function handleTrigger(deviceId, type) {
    setStatusByDevice((prev) => ({ ...prev, [deviceId]: { kind: 'pending', text: 'Sending…', type } }))

    const { ok, status, body } = await triggerCommand(token, deviceId, type)

    if (ok) {
      const verb = type === 'SoundAlarm' ? 'Alarm command sent' : 'Stop command sent'
      setStatusByDevice((prev) => ({
        ...prev,
        [deviceId]: { kind: 'success', text: `${verb} — ${body.id?.slice(0, 8)}…`, type },
      }))
    } else if (status === 502) {
      setStatusByDevice((prev) => ({
        ...prev,
        [deviceId]: { kind: 'error', text: `Delivery failed: ${body.detail || body.message}`, type },
      }))
    } else {
      setStatusByDevice((prev) => ({
        ...prev,
        [deviceId]: { kind: 'error', text: body.message || `Request failed (${status})`, type },
      }))
    }
  }

  function handleLogout() {
    localStorage.removeItem('antitheftapp_token')
    navigate('/')
  }

  function sonarState(device, status) {
    if (status?.kind === 'pending' && status.type === 'SoundAlarm') return 'alarming'
    if (status?.kind === 'success' && status.type === 'SoundAlarm') return 'alarming'
    if (!device.isProtectionEnabled) return 'off'
    return 'protected'
  }

  return (
    <div className="shell">
      <div className="topbar">
        <div className="brand">
          <Logo size={26} />
          <div className="brand-word">
            AntiTheftApp
            <small>SECURITY CONSOLE</small>
          </div>
        </div>
        <button className="btn btn-ghost" style={{ width: 'auto', padding: '8px 16px' }} onClick={handleLogout}>
          Log out
        </button>
      </div>

      <div className="page">
        <div className="page-head">
          <div className="eyebrow">
            <span className="dot" />
            {loading ? 'Checking in…' : `${devices.length} device${devices.length === 1 ? '' : 's'} registered`}
          </div>
          <h1>Your devices</h1>
          <p className="subtitle">Sound the alarm on a lost device, or stand it down once it's found.</p>
        </div>

        {error && <div className="banner error">{error}</div>}

        {loading && (
          <>
            <div className="skeleton-row" />
            <div className="skeleton-row" />
          </>
        )}

        {!loading && devices.length === 0 && !error && (
          <div className="empty-state">
            <Sonar state="off" />
            <p>No devices registered yet. Install the mobile app and sign in there to bring a device online here.</p>
          </div>
        )}

        {devices.map((device) => {
          const status = statusByDevice[device.id]
          return (
            <div className="device-card" key={device.id}>
              <Sonar state={sonarState(device, status)} />

              <div className="device-main">
                <div className="device-top">
                  <div className="device-name">{device.deviceName}</div>
                  <span className={`status-pill ${device.isProtectionEnabled ? 'on' : 'off'}`}>
                    {device.isProtectionEnabled ? 'Protected' : 'Protection off'}
                  </span>
                </div>

                <div className="device-meta">
                  {device.platform}
                  {device.lastSeenAt && (
                    <>
                      <span className="sep">·</span>
                      last seen {timeAgo(device.lastSeenAt)}
                    </>
                  )}
                </div>

                {device.lastSecurityEvent && (
                  <div className="security-flag">
                    ⚠ {device.lastSecurityEvent}
                    {device.lastSecurityEventAt && ` — ${timeAgo(device.lastSecurityEventAt)}`}
                  </div>
                )}

                <div className="btn-row">
                  <button className="btn btn-alarm" onClick={() => handleTrigger(device.id, 'SoundAlarm')}>
                    Sound alarm
                  </button>
                  <button className="btn btn-ghost" onClick={() => handleTrigger(device.id, 'StopAlarm')}>
                    Stop alarm
                  </button>
                </div>

                {status && <div className={`status-msg ${status.kind}`}>{status.text}</div>}
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
