import { create } from 'zustand'

const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:62845'

/** Relative paths (/Uploads/...) are served by the API server — prefix them.
 * Without this the browser resolves them against the parent app's own domain
 * (where the file doesn't exist) and the logo/favicon/login-bg never load. */
const toAbsolute = (path) =>
  path && path.startsWith('/') ? `${API_BASE}${path}` : path

export const useBrandingStore = create((set) => ({
  branding: {
    displayName:    'Fee Portal',
    primaryColor:   '#1677ff',
    headerBgColor:  '#001529',
    navTextColor:   '#ffffff',
    logoPath:       null,
    faviconPath:    null,
    tagline:        null,
    loginBgPath:    null,
  },
  setBranding: (data) => set((state) => {
    const nonNull = Object.fromEntries(
      Object.entries(data || {}).filter(([, v]) => v != null && v !== '')
    )
    return {
      branding: {
        ...state.branding,
        ...nonNull,
        logoPath:    toAbsolute(data?.logoPath),
        faviconPath: toAbsolute(data?.faviconPath),
        loginBgPath: toAbsolute(data?.loginBgPath),
      },
    }
  }),
}))
