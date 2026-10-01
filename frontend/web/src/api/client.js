import axios from 'axios'

/**
 * Cliente HTTP centralizado.
 * - Adjunta el JWT en cada petición.
 * - En 401 cierra la sesión (token expirado / inválido).
 */
const api = axios.create({
  baseURL: '/api',
  timeout: 15000,
})

const TOKEN_KEY = 'tp_token'

export const tokenStore = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (t) => localStorage.setItem(TOKEN_KEY, t),
  clear: () => localStorage.removeItem(TOKEN_KEY),
}

api.interceptors.request.use((config) => {
  const token = tokenStore.get()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (res) => res,
  (error) => {
    // 401: el token ya no sirve → limpiar sesión
    if (error.response?.status === 401 && tokenStore.get()) {
      tokenStore.clear()
      if (window.__onUnauthorized) window.__onUnauthorized()
    }
    return Promise.reject(error)
  },
)

/**
 * Extrae un mensaje legible de una respuesta de error del backend.
 */
export function errorMessage(error, fallback = 'Ocurrió un error inesperado.') {
  const data = error?.response?.data
  if (!data) return error?.message || fallback
  if (typeof data === 'string') return data
  if (data.message) return data.message

  // Errores de validación de DataAnnotations: { errors: { Campo: ["msg"] } }
  if (data.errors) {
    const first = Object.values(data.errors)[0]
    if (Array.isArray(first) && first.length) return first[0]
    if (typeof first === 'string') return first
  }
  return fallback
}

export default api
