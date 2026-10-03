<script setup>
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()

// Menú desplegable de «Tutoriales» (Programación / Dibujo)
const menuOpen = ref(false)
// «Tutoriales» está activo en cualquiera de sus índices
const enTutoriales = computed(() => ['/', '/dibujo', '/marketing'].includes(route.path))

// Se cierra al pulsar fuera del menú o con Esc; el propio botón no llega
// aquí porque detiene la propagación.
function onDocClick(e) {
  if (e.target instanceof Element && !e.target.closest('.nav-dropdown')) menuOpen.value = false
}
function onEsc(e) {
  if (e.key === 'Escape') menuOpen.value = false
}

onMounted(() => {
  document.addEventListener('click', onDocClick)
  document.addEventListener('keydown', onEsc)
})
onUnmounted(() => {
  document.removeEventListener('click', onDocClick)
  document.removeEventListener('keydown', onEsc)
})

const initials = computed(() => (auth.username || '?').slice(0, 2).toUpperCase())

function logout() {
  auth.logout()
  router.push({ name: 'home' })
}
</script>

<template>
  <div class="app-shell">
    <header class="navbar">
      <router-link to="/" class="brand" aria-label="Vía Alexandria: conocimiento en forma de tutoriales">
        <img class="brand-logo" src="/logo.png" alt="Vía Alexandria: conocimiento en forma de tutoriales" />
      </router-link>

      <nav class="nav-links">
        <!-- «Tutoriales» dejó de ser un enlace: abre un menú con los índices -->
        <div class="nav-dropdown">
          <button
            type="button"
            class="nav-dropdown-toggle"
            :class="{ active: enTutoriales }"
            :aria-expanded="menuOpen"
            aria-haspopup="menu"
            @click.stop="menuOpen = !menuOpen"
          >
            Tutoriales <span class="nav-caret" aria-hidden="true">▾</span>
          </button>

          <div v-show="menuOpen" class="nav-dropdown-menu" role="menu">
            <router-link to="/" role="menuitem" @click="menuOpen = false">
              Programación
            </router-link>
            <router-link to="/dibujo" role="menuitem" @click="menuOpen = false">
              Dibujo
            </router-link>
            <router-link to="/marketing" role="menuitem" @click="menuOpen = false">
              Marketing
            </router-link>
          </div>
        </div>

        <template v-if="auth.isAuthenticated">
          <router-link to="/my-lists">Mis listas</router-link>
          <router-link to="/profile">Mi perfil</router-link>
        </template>
      </nav>

      <div class="nav-auth">
        <template v-if="auth.isAuthenticated">
          <span class="badge" :class="{ admin: auth.isAdmin }">
            {{ auth.isAdmin ? 'ADMIN' : 'USER' }}
          </span>
          <span class="avatar" :title="auth.username">
            <img v-if="auth.user?.photoUrl" :src="auth.user.photoUrl" :alt="auth.username" />
            <template v-else>{{ initials }}</template>
          </span>
          <button class="btn btn-ghost" @click="logout">Cerrar sesión</button>
        </template>

        <template v-else>
          <router-link class="btn btn-ghost" to="/login">Iniciar sesión</router-link>
          <router-link class="btn btn-primary" to="/register">Registrarse</router-link>
        </template>
      </div>
    </header>

    <main class="page">
      <router-view />
    </main>

    <footer class="footer">
      Vía Alexandria: conocimiento en forma de tutoriales
    </footer>
  </div>
</template>
