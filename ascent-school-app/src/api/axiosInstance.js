import { useAuthStore } from '../store/authStore'

const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:62845'

// Extracts subdomain from hostname (e.g. srividya.edu-care.in → srividya)
// In dev, set VITE_SUBDOMAIN=srividya in .env.local
const getSubdomain = () => {
  if (import.meta.env.VITE_SUBDOMAIN) return import.meta.env.VITE_SUBDOMAIN
  const parts = window.location.hostname.split('.')
  return parts.length >= 3 ? parts[0] : null
}

// ── Silent-refresh state ──────────────────────────────────────────────────
let refreshing = false
let queue      = []

const processQueue = (error) => {
  queue.forEach((p) => (error ? p.reject(error) : p.resolve()))
  queue = []
}

async function doRefresh() {
  const schoolId = localStorage.getItem('schoolId')
  const params   = schoolId ? `?schoolId=${schoolId}` : ''
  const subdomain = getSubdomain()

  const res = await fetch(`${API_BASE}/school/auth/refresh${params}`, {
    method:      'POST',
    credentials: 'include',
    headers:     subdomain ? { 'X-Subdomain': subdomain } : {},
  })
  if (!res.ok) throw new Error('Refresh failed')
  const body = await res.json()
  return body.data.accessToken
}

// ── Core request function ─────────────────────────────────────────────────
async function request(method, path, body, retry = false) {
  const token     = useAuthStore.getState().accessToken
  const subdomain = getSubdomain()
  const headers   = {}

  if (token)     headers['Authorization'] = `Bearer ${token}`
  if (subdomain) headers['X-Subdomain']   = subdomain

  const options = { method, credentials: 'include', headers }

  if (body !== undefined && body !== null) {
    headers['Content-Type'] = 'application/json'
    options.body            = JSON.stringify(body)
  }

  const res = await fetch(`${API_BASE}${path}`, options)

  // ── 401 → silent refresh + retry (once) ──────────────────────────────
  if (res.status === 401 && !retry) {
    if (refreshing) {
      await new Promise((resolve, reject) => queue.push({ resolve, reject }))
      return request(method, path, body, true)
    }

    refreshing = true
    try {
      const newToken = await doRefresh()
      useAuthStore.getState().setAccessToken(newToken)
      processQueue(null)
      return request(method, path, body, true)
    } catch (err) {
      processQueue(err)
      useAuthStore.getState().logout()
      window.location.href = '/login'
      throw err
    } finally {
      refreshing = false
    }
  }

  // ── Non-2xx → throw with same shape as before ─────────────────────────
  if (!res.ok) {
    const errBody = await res.json().catch(() => ({}))
    const err     = new Error(errBody.message || `Request failed: ${res.status}`)
    err.response  = { status: res.status, data: errBody }
    throw err
  }

  const data = await res.json()
  return { data }   // matches the res.data shape used throughout the app
}

// ── Public API ────────────────────────────────────────────────────────────
const api = {
  get:    (path)       => request('GET',    path),
  post:   (path, body) => request('POST',   path, body ?? {}),
  put:    (path, body) => request('PUT',    path, body ?? {}),
  delete: (path)       => request('DELETE', path),
}

export default api
