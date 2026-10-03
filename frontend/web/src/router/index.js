import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const routes = [
  { path: '/', name: 'home', component: () => import('../views/HomeView.vue'), meta: { domain: 'programacion' } },
  {
    path: '/dibujo',
    name: 'dibujo',
    component: () => import('../views/HomeView.vue'),
    meta: { domain: 'dibujo' },
  },
  {
    path: '/marketing',
    name: 'marketing',
    component: () => import('../views/HomeView.vue'),
    meta: { domain: 'marketing' },
  },
  { path: '/login', name: 'login', component: () => import('../views/LoginView.vue'), meta: { guest: true } },
  { path: '/register', name: 'register', component: () => import('../views/RegisterView.vue'), meta: { guest: true } },
  { path: '/tutorials/:id', name: 'tutorial', component: () => import('../views/TutorialDetailView.vue') },
  {
    path: '/tutorials/:id/edit',
    name: 'tutorial-edit',
    component: () => import('../views/TutorialFormView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/new-tutorial',
    name: 'new-tutorial',
    component: () => import('../views/TutorialFormView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/profile',
    name: 'profile',
    component: () => import('../views/ProfileView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/my-lists',
    name: 'my-lists',
    component: () => import('../views/MyListsView.vue'),
    meta: { requiresAuth: true },
  },
  {
    path: '/my-lists/:id',
    name: 'list-detail',
    component: () => import('../views/ListDetailView.vue'),
    meta: { requiresAuth: true },
  },
  { path: '/:pathMatch(.*)*', name: 'not-found', component: () => import('../views/NotFoundView.vue') },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: () => ({ top: 0 }),
})

router.beforeEach(async (to) => {
  const auth = useAuthStore()

  // Asegurar que el usuario esté cargado antes de evaluar guardas
  if (!auth.initialized) await auth.fetchMe()

  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  // El login/registro no tiene sentido con sesión abierta
  if (to.meta.guest && auth.isAuthenticated) {
    return { name: 'home' }
  }

  return true
})

export default router
