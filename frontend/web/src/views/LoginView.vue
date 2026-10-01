<script setup>
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { errorMessage } from '../api/client'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

const form = ref({ usernameOrEmail: '', password: '' })
const error = ref('')

async function submit() {
  error.value = ''
  if (!form.value.usernameOrEmail.trim() || !form.value.password) {
    error.value = 'Completa todos los campos.'
    return
  }
  try {
    await auth.login({
      usernameOrEmail: form.value.usernameOrEmail.trim(),
      password: form.value.password,
    })
    router.push(route.query.redirect || '/')
  } catch (e) {
    error.value = errorMessage(e, 'No se pudo iniciar sesión.')
  }
}
</script>

<template>
  <div class="card auth-card">
    <h1 class="section-title">Iniciar sesión</h1>
    <p class="section-sub">Accede para guardar tutoriales en tu biblioteca.</p>

    <div v-if="error" class="alert alert-error">{{ error }}</div>

    <form class="form mt-2" @submit.prevent="submit">
      <div class="field">
        <label for="u">Usuario o correo</label>
        <input
          id="u"
          v-model="form.usernameOrEmail"
          type="text"
          autocomplete="username"
          placeholder="maria"
          required
        />
      </div>

      <div class="field">
        <label for="p">Contraseña</label>
        <input
          id="p"
          v-model="form.password"
          type="password"
          autocomplete="current-password"
          placeholder="••••••••"
          required
        />
      </div>

      <button class="btn btn-primary" type="submit" :disabled="auth.loading">
        <span v-if="auth.loading" class="spinner" />
        {{ auth.loading ? 'Entrando…' : 'Entrar' }}
      </button>
    </form>

    <p class="text-center mt-2 muted">
      ¿No tienes cuenta?
      <router-link class="link" to="/register">Regístrate</router-link>
    </p>

    <div class="hint mt-2">
      Cuentas de prueba: <strong>admin/admin123</strong> ·
      <strong>maria/maria123</strong> · <strong>carlos/carlos123</strong>
    </div>
  </div>
</template>
