const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:62845'

// ── Silent-refresh state ──────────────────────────────────────────────────
let refreshing = false
let queue      = []

const processQueue = (error) => {
  queue.forEach((p) => (error ? p.reject(error) : p.resolve()))
  queue = []
}

async function doRefresh() {
  const res = await fetch(`${API_BASE}/control/auth/refresh`, {
    method:      'POST',
    credentials: 'include',
  })
  if (!res.ok) throw new Error('Refresh failed')
  const body = await res.json()
  return body.data.accessToken
}

// ── Core request function ─────────────────────────────────────────────────
async function request(method, path, body, retry = false) {
  const token   = sessionStorage.getItem('controlAccessToken')
  const headers = {}

  if (token) headers['Authorization'] = `Bearer ${token}`

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
      sessionStorage.setItem('controlAccessToken', newToken)
      processQueue(null)
      return request(method, path, body, true)
    } catch (err) {
      processQueue(err)
      sessionStorage.removeItem('controlAccessToken')
      window.location.href = '/login'
      throw err
    } finally {
      refreshing = false
    }
  }

  // ── Non-2xx → throw with same shape as axios errors ───────────────────
  if (!res.ok) {
    const errBody = await res.json().catch(() => ({}))
    const err     = new Error(errBody.message || `Request failed: ${res.status}`)
    err.response  = { status: res.status, data: errBody }
    throw err
  }

  const data = await res.json()
  return { data }   // matches axios res.data shape used throughout the app
}

// ── Public API — same interface as the previous axios instance ────────────
const api = {
  get:    (path)       => request('GET',    path),
  post:   (path, body) => request('POST',   path, body ?? {}),
  put:    (path, body) => request('PUT',    path, body ?? {}),
  delete: (path)       => request('DELETE', path),
}

export default api
