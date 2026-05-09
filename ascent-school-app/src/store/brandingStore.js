import { create } from 'zustand'

const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:62845'

/** Relative paths (/Uploads/...) are served by the API server — prefix them. */
const toAbsolute = (path) =>
  path && path.startsWith('/') ? `${API_BASE}${path}` : path

/**
 * Branding store — loaded from public GET /branding before login.
 * Consumed by App.jsx to configure AntD theme and CSS variables.
 */
export const useBrandingStore = create((set) => ({
  branding: {
    displayName:       'Ascent Schools',
    tagline:           '',
    logoPath:          null,
    faviconPath:       null,
    primaryColor:      '#1677ff',   // AntD default blue
    secondaryColor:    '#69b1ff',
    headerBgColor:     '#001529',
    navTextColor:      '#ffffff',
    loginBgPath:       null,
    receiptFooterText: '',
  },
  isLoaded: false,

  setBranding: (data) => set((state) => {
    // Only overwrite defaults with non-null values from the API.
    // If a school has no branding configured the API returns nulls;
    // preserving defaults avoids black-on-black rendering.
    const nonNull = Object.fromEntries(
      Object.entries(data || {}).filter(([, v]) => v != null && v !== '')
    )
    return {
      branding: {
        ...state.branding,
        ...nonNull,
        logoPath:    toAbsolute(data.logoPath),
        faviconPath: toAbsolute(data.faviconPath),
        loginBgPath: toAbsolute(data.loginBgPath),
      },
      isLoaded: true,
    }
  }),
}))
