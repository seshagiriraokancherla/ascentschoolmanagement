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

// ── Error text extraction ─────────────────────────────────────────────────
// Two different body shapes come back from the API:
//   • handled errors  → our envelope:  { success:false, message: "Admission number already exists." }
//   • unhandled crash → Web API HttpError: { message: "An error has occurred.",
//                                            exceptionMessage: "<the real reason>", stackTrace: … }
// The envelope's `message` is the useful one; HttpError's is a fixed placeholder
// and the truth sits in `exceptionMessage` (WebApiConfig sets
// IncludeErrorDetailPolicy.Always, so it IS sent in production). Prefer the
// exception text whenever the top-level message is that placeholder, otherwise
// nothing above this layer can tell a validation failure from a server crash.
const GENERIC_HTTP_ERROR = 'an error has occurred'

function errorTextFrom(body, status) {
  const msg = body?.message
  const ex  = body?.exceptionMessage || body?.ExceptionMessage
  if (ex && (!msg || msg.trim().toLowerCase().replace(/\.$/, '') === GENERIC_HTTP_ERROR)) return ex
  return msg || body?.Message || `Request failed: ${status}`
}

/**
 * Message to show for a caught error: the server's reason when there is one,
 * otherwise the caller's own wording.
 *
 * An Ant Design `validateFields()` rejection carries `errorFields` and a useless
 * message, and the form already highlights the offending inputs — so those fall
 * back to the caller's text rather than leaking form internals into a toast.
 * (Callers that want no toast at all for validation still guard on
 * `err?.errorFields` themselves, as StudentFormPage does.)
 */
export function apiError(err, fallback = 'Something went wrong.') {
  if (err?.errorFields) return fallback
  return err?.message || fallback
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
      // Clear auth state only. RequireAuth (subscribed to isAuthenticated) then
      // redirects to /login via React Router — a client-side navigation. The old
      // `window.location.href = '/login'` did a full page load, which returned the
      // host's 404 page when SPA fallback wasn't configured (see public/.htaccess
      // and public/web.config).
      useAuthStore.getState().logout()
      throw err
    } finally {
      refreshing = false
    }
  }

  // ── Non-2xx → throw with same shape as before ─────────────────────────
  if (!res.ok) {
    const errBody = await res.json().catch(() => ({}))
    const err     = new Error(errorTextFrom(errBody, res.status))
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
