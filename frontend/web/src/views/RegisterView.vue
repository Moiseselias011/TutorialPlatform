<script setup>
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { errorMessage } from '../api/client'

const auth = useAuthStore()
const router = useRouter()

const form = ref({ username: '', email: '', password: '', confirm: '' })
const error = ref('')

// Validación en cliente (espejo de la del servidor)
const usernameError = computed(() => {
  const v = form.value.username.trim()
  if (!v) return ''
  if (v.length < 3) return 'Mínimo 3 caracteres.'
  if (!/^[a-zA-Z0-9_.-]+$/.test(v)) return 'Solo letras, números, punto, guion y guion bajo.'
  return ''
})

const emailError = computed(() => {
  const v = form.value.email.trim()
  if (!v) return ''
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v) ? '' : 'Correo no válido.'
})

const passwordError = computed(() => {
  const v = form.value.password
  if (!v) return ''
  return v.length >= 6 ? '' : 'Mínimo 6 caracteres.'
})

const confirmError = computed(() => {
  if (!form.value.confirm) return ''
  return form.value.confirm === form.value.password ? '' : 'Las contraseñas no coinciden.'
})

const isValid = computed(
  () =>
    form.value.username.trim().length >= 3 &&
    !usernameError.value &&
    !emailError.value &&
    !passwordError.value &&
    !confirmError.value,
)

async function submit() {
  error.value = ''
  if (!isValid.value) {
    error.value = 'Revisa los campos marcados.'
    return
  }
  try {
    await auth.register({
      username: form.value.username.trim(),
      email: form.value.email.trim(),
      password: form.value.password,
    })
    router.push('/')
  } catch (e) {
    error.value = errorMessage(e, 'No se pudo crear la cuenta.')
  }
}
</script>

<template>
  <div class="card auth-card">
    <h1 class="section-title">Crear cuenta</h1>
    <p class="section-sub">
      Únete para comentar, dar Me gusta y armar tu biblioteca personal.
    </p>

    <div v-if="error" class="alert alert-error">{{ error }}</div>

    <form class="form mt-2" @submit.prevent="submit">
      <div class="field">
        <label for="username">Usuario</label>
        <input
          id="username"
          v-model="form.username"
          type="text"
          autocomplete="username"
          placeholder="tu_usuario"
          required
        />
        <span v-if="usernameError" class="hint" style="color: var(--danger)">{{ usernameError }}</span>
      </div>

      <div class="field">
        <label for="email">Correo</label>
        <input
          id="email"
          v-model="form.email"
          type="email"
          autocomplete="email"
          placeholder="tu@correo.com"
          required
        />
        <span v-if="emailError" class="hint" style="color: var(--danger)">{{ emailError }}</span>
      </div>

      <div class="field">
        <label for="password">Contraseña</label>
        <input
          id="password"
          v-model="form.password"
          type="password"
          autocomplete="new-password"
          placeholder="Mínimo 6 caracteres"
          required
        />
        <span v-if="passwordError" class="hint" style="color: var(--danger)">{{ passwordError }}</span>
      </div>

      <div class="field">
        <label for="confirm">Confirmar contraseña</label>
        <input
          id="confirm"
          v-model="form.confirm"
          type="password"
          autocomplete="new-password"
          placeholder="Repite la contraseña"
          required
        />
        <span v-if="confirmError" class="hint" style="color: var(--danger)">{{ confirmError }}</span>
      </div>

      <button class="btn btn-primary" type="submit" :disabled="auth.loading || !isValid">
        <span v-if="auth.loading" class="spinner" />
        {{ auth.loading ? 'Creando…' : 'Crear cuenta' }}
      </button>
    </form>

    <p class="text-center mt-2 muted">
      ¿Ya tienes cuenta?
      <router-link class="link" to="/login">Inicia sesión</router-link>
    </p>
  </div>
</template>
