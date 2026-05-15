import { create } from 'zustand'

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
  setBranding: (data) => set({ branding: { ...data } }),
}))
