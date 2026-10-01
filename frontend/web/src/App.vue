<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'

const auth = useAuthStore()
const router = useRouter()

const initials = computed(() => (auth.username || '?').slice(0, 2).toUpperCase())

function logout() {
  auth.logout()
  router.push({ name: 'home' })
}
</script>

<template>
  <div class="app-shell">
    <header class="navbar">
      <router-link to="/" class="brand">
        <span class="brand-mark">📚</span>
        <span>Tutorial<span class="accent">Hub</span></span>
      </router-link>

      <nav class="nav-links">
        <router-link to="/">Tutoriales</router-link>

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
      TutorialHub · Plataforma colaborativa de tutoriales de programación
    </footer>
  </div>
</template>
