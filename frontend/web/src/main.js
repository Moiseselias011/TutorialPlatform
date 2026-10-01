import { createApp } from 'vue'
import { createPinia } from 'pinia'
import './style.css'
import App from './App.vue'
import router from './router'
import { tokenStore } from './api/client'
import { useAuthStore } from './stores/auth'

const app = createApp(App)

app.use(createPinia())
app.use(router)

// Si el interceptor detecta un 401, volvemos al login sin recargar
window.__onUnauthorized = () => {
  const auth = useAuthStore()
  auth.logout()
  if (router.currentRoute.value.meta.requiresAuth) {
    router.push({ name: 'login', query: { redirect: router.currentRoute.value.fullPath } })
  }
}

// Evita quedarse con un token muerto
if (!tokenStore.get()) {
  const auth = useAuthStore()
  auth.token = null
  auth.user = null
}

app.mount('#app')
