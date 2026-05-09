import { create } from 'zustand'

/**
 * Auth store — school app.
 * Access token stored in MEMORY only (never localStorage/sessionStorage — XSS protection).
 * Refresh token lives in HttpOnly cookie managed by the browser/API.
 */
export const useAuthStore = create((set, get) => ({
  accessToken: null,
  user: null,        // { userId, groupId, schoolId, fullName, permissions[] }
  isAuthenticated: false,

  setAccessToken: (token) => set({ accessToken: token, isAuthenticated: !!token }),

  setUser: (user) => set({ user }),

  login: (accessToken, user) => set({ accessToken, user, isAuthenticated: true }),

  logout: () => set({ accessToken: null, user: null, isAuthenticated: false }),

  hasPermission: (permCode) => {
    const perms = get().user?.permissions
    return Array.isArray(perms) && perms.includes(permCode)
  },
}))
