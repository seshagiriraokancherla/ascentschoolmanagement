import { create } from 'zustand'

const useAuthStore = create((set) => ({
  user:            null,   // { userId, fullName, role }
  accessToken:     null,
  isAuthenticated: false,

  login: (userData, token) => {
    sessionStorage.setItem('controlAccessToken', token)
    set({ user: userData, accessToken: token, isAuthenticated: true })
  },

  logout: () => {
    sessionStorage.removeItem('controlAccessToken')
    set({ user: null, accessToken: null, isAuthenticated: false })
  },

  setAccessToken: (token) => {
    sessionStorage.setItem('controlAccessToken', token)
    set({ accessToken: token })
  },

  hasRole: (role) => {
    const state = useAuthStore.getState()
    return state.user?.role === role
  },
}))

export default useAuthStore
