import { defineStore } from 'pinia'
import api, { tokenStore } from '../api/client'

/**
 * Estado de autenticación.
 * El usuario SOLO puede ver su propio perfil (no hay perfiles públicos).
 */
export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: tokenStore.get(),
    user: null,
    loading: false,
    initialized: false,
  }),

  getters: {
    isAuthenticated: (s) => Boolean(s.token && s.user),
    isAdmin: (s) => s.user?.role === 'ADMIN',
    username: (s) => s.user?.username ?? '',
  },

  actions: {
    setSession(token, user) {
      this.token = token
      this.user = user
      tokenStore.set(token)
    },

    async register(payload) {
      this.loading = true
      try {
        const { data } = await api.post('/auth/register', payload)
        this.setSession(data.token, data.user)
        return data
      } finally {
        this.loading = false
      }
    },

    async login(payload) {
      this.loading = true
      try {
        const { data } = await api.post('/auth/login', payload)
        this.setSession(data.token, data.user)
        return data
      } finally {
        this.loading = false
      }
    },

    /** Carga (o recarga) el perfil propio desde el token guardado. */
    async fetchMe() {
      if (!this.token) {
        this.initialized = true
        return null
      }
      try {
        const { data } = await api.get('/auth/me')
        this.user = data
        return data
      } catch {
        this.logout()
        return null
      } finally {
        this.initialized = true
      }
    },

    logout() {
      this.token = null
      this.user = null
      tokenStore.clear()
    },
  },
})
