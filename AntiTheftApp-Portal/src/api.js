// In dev this falls back to your local API. For a real deploy, set
// VITE_API_BASE_URL in a .env file (see .env.example) to your hosted
// API's URL — a hardcoded localhost address will not work once this
// is built and served from anywhere but your own machine.
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'

async function handleAuthResponse(response) {
  if (!response.ok) {
    const body = await response.json().catch(() => ({}))
    throw new Error(body.message || `Request failed (${response.status})`)
  }
  return response.json()
}

export async function login(email, password) {
  const response = await fetch(`${API_BASE_URL}/api/Auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  })
  return handleAuthResponse(response)
}

export async function getDevices(token) {
  const response = await fetch(`${API_BASE_URL}/api/Devices`, {
    headers: { Authorization: `Bearer ${token}` },
  })
  if (!response.ok) throw new Error(`Could not load devices (${response.status})`)
  return response.json()
}

export async function triggerCommand(token, deviceId, type) {
  const response = await fetch(`${API_BASE_URL}/api/Devices/${deviceId}/commands`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({ type }),
  })

  const body = await response.json().catch(() => ({}))
  return { ok: response.ok, status: response.status, body }
}

export async function register(email, password, pin) {
  const response = await fetch(`${API_BASE_URL}/api/Auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password, pin }),
  })
  return handleAuthResponse(response)
}