import { create } from 'zustand'

export const useAuthStore = create((set) => ({
  accessToken: null,
  parent:      null,   // { parentId, displayName }
  child:       null,   // { studentId, studentName, className, admissionNo }

  // Called after OTP verify — parent authenticated, child not yet selected
  login: (accessToken, parent) => set({ accessToken, parent, child: null }),

  // Called after select-child — new token carries child context
  setChild: (accessToken, child) => set({ accessToken, child }),

  logout:   () => set({ accessToken: null, parent: null, child: null }),
  setToken: (accessToken) => set({ accessToken }),
}))
