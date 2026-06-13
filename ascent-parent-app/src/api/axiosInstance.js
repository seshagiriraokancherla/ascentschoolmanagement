const API_BASE   = import.meta.env.VITE_API_BASE_URL || 'http://localhost:62845'
const SCHOOL_CODE = import.meta.env.VITE_SCHOOL_CODE || ''

let _getToken = () => null
let _setToken = () => {}

export function connectAuthStore(getToken, setToken) {
  _getToken = getToken
  _setToken = setToken
}

function baseHeaders() {
  const h = { 'Content-Type': 'application/json' }
  if (SCHOOL_CODE) {
    h['X-School-Code'] = SCHOOL_CODE
    h['X-Subdomain']   = SCHOOL_CODE  // branding endpoint uses X-Subdomain
  }
  const token = _getToken()
  if (token) h['Authorization'] = `Bearer ${token}`
  return h
}

async function request(method, path, body) {
  const res = await fetch(`${API_BASE}${path}`, {
    method,
    credentials: 'include',
    headers:     baseHeaders(),
    body:        body != null ? JSON.stringify(body) : undefined,
  })

  // Pre-authentication endpoints (no token exists yet) must surface the real
  // backend message rather than triggering the refresh-retry "Session expired" path.
  const NO_RETRY_PATHS = [
    '/mobile/auth/parent/request-otp',
    '/mobile/auth/parent/verify-otp',
    '/mobile/auth/parent/refresh',
    '/mobile/auth/parent/login',
    '/mobile/auth/parent/register',
  ]
  const skipRetry = NO_RETRY_PATHS.includes(path)

  if (res.status === 401 && !skipRetry) {
    const rr = await fetch(`${API_BASE}/mobile/auth/parent/refresh`, {
      method:      'POST',
      credentials: 'include',
      headers: {
        'Content-Type': 'application/json',
        'X-School-Code': SCHOOL_CODE,
        'X-Subdomain':   SCHOOL_CODE,
      },
    })
    if (rr.ok) {
      const rb = await rr.json()
      const newToken = rb.data?.accessToken
      _setToken(newToken)
      const retryRes = await fetch(`${API_BASE}${path}`, {
        method,
        credentials: 'include',
        headers:     { ...baseHeaders(), Authorization: `Bearer ${newToken}` },
        body:        body != null ? JSON.stringify(body) : undefined,
      })
      const retryData = await retryRes.json()
      return { data: retryData }
    }
    _setToken(null)
    throw Object.assign(new Error('Session expired'), { status: 401 })
  }

  const parsed = await res.json()
  if (!res.ok) throw Object.assign(new Error(parsed?.message || 'Request failed'), { status: res.status })
  return { data: parsed }
}

const api = {
  get:    (path)       => request('GET',    path),
  post:   (path, body) => request('POST',   path, body),
  put:    (path, body) => request('PUT',    path, body),
  delete: (path)       => request('DELETE', path),
}

export default api
