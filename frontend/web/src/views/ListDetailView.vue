<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api, { errorMessage } from '../api/client'
import TutorialCard from '../components/TutorialCard.vue'

const route = useRoute()
const router = useRouter()

const list = ref(null)
const loading = ref(true)
const error = ref('')
const notice = ref('')
const busy = ref(false)

// Búsqueda y paginación DENTRO de la lista personal.
// La API devuelve la lista completa en una sola petición, así que se filtra
// y pagina en cliente: respuesta inmediata mientras se escribe y sin
// llamadas adicionales al servidor.
const search = ref('')
const page = ref(1)
const pageSize = 6

const filtered = computed(() => {
  const all = list.value?.tutorials ?? []
  const q = search.value.trim().toLowerCase()
  if (!q) return all
  return all.filter(
    (t) =>
      (t.title || '').toLowerCase().includes(q) ||
      (t.description || '').toLowerCase().includes(q) ||
      (t.technology?.name || '').toLowerCase().includes(q) ||
      (t.author?.username || '').toLowerCase().includes(q),
  )
})

const totalPages = computed(() => Math.max(1, Math.ceil(filtered.value.length / pageSize)))

const pageItems = computed(() => {
  const start = (page.value - 1) * pageSize
  return filtered.value.slice(start, start + pageSize)
})

// Escribir una búsqueda vuelve siempre a la primera página
watch(search, () => {
  page.value = 1
})

// Si la página actual deja de existir (menos resultados, o se quitó un
// tutorial), se recorta al último valor válido en lugar de dejar la vista en blanco.
watch(totalPages, (n) => {
  if (page.value > n) page.value = n
})

async function load() {
  loading.value = true
  error.value = ''
  search.value = ''
  page.value = 1
  try {
    const { data } = await api.get(`/lists/${route.params.id}`)
    list.value = data
  } catch (e) {
    error.value = errorMessage(e, 'No se pudo cargar la lista.')
  } finally {
    loading.value = false
  }
}

onMounted(load)
watch(() => route.params.id, load)

async function remove(tutorial) {
  if (!confirm(`¿Quitar "${tutorial.title}" de esta lista? El tutorial sigue en la plataforma.`)) return
  busy.value = true
  try {
    await api.delete(`/lists/${list.value.id}/save/${tutorial.id}`)
    list.value.tutorials = list.value.tutorials.filter((t) => t.id !== tutorial.id)
    notice.value = 'Quitado de la lista.'
  } catch (e) {
    error.value = errorMessage(e, 'No se pudo quitar.')
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <div>
    <button class="btn btn-outline btn-sm" @click="router.push('/my-lists')">← Mis listas</button>

    <div v-if="loading" class="empty">
      <span class="spinner dark" style="width: 30px; height: 30px" />
      <p>Cargando lista…</p>
    </div>

    <div v-else-if="error" class="empty">
      <span class="empty-icon">⚠️</span>
      <p>{{ error }}</p>
      <button class="btn btn-outline" @click="load">Reintentar</button>
    </div>

    <template v-else-if="list">
      <div class="row mt-2" style="margin-bottom: 20px">
        <div>
          <h1 class="section-title">📁 {{ list.name }}</h1>
          <p class="section-sub" style="margin: 0">
            {{ list.tutorials.length }} tutoriales guardados ·
            creada el {{ new Date(list.createdAt).toLocaleDateString() }}
          </p>
        </div>
      </div>

      <div v-if="notice" class="alert alert-success" style="margin-bottom: 16px">{{ notice }}</div>

      <!-- Lista totalmente vacía -->
      <div v-if="list.tutorials.length === 0" class="empty">
        <span class="empty-icon">📭</span>
        <p>Esta lista está vacía.</p>
        <p class="hint">Usa el botón «Guardar» en cualquier tutorial para añadirlo aquí.</p>
        <router-link class="btn btn-primary mt-2" to="/">Explorar tutoriales</router-link>
      </div>

      <template v-else>
        <!-- Búsqueda dentro de la lista personal -->
        <div class="toolbar">
          <input
            v-model="search"
            class="toolbar-search"
            type="search"
            placeholder="🔍 Buscar dentro de esta lista…"
            aria-label="Buscar dentro de esta lista"
          />
          <span class="hint" style="align-self: center; white-space: nowrap">
            {{ filtered.length }} de {{ list.tutorials.length }}
          </span>
        </div>

        <!-- Tiene tutoriales pero ninguno coincide con la búsqueda -->
        <div v-if="filtered.length === 0" class="empty">
          <span class="empty-icon">🔍</span>
          <p>No hay tutoriales que coincidan con «{{ search }}».</p>
          <button class="btn btn-outline" @click="search = ''">Limpiar búsqueda</button>
        </div>

        <div v-else class="grid">
          <div v-for="t in pageItems" :key="t.id" class="wrapper">
            <TutorialCard :tutorial="t" />
            <button class="btn btn-outline btn-sm unsave" :disabled="busy" @click="remove(t)">
              ✕ Quitar de esta lista
            </button>
          </div>
        </div>

        <!-- Paginación -->
        <nav v-if="totalPages > 1" class="pagination" aria-label="Paginación de la lista">
          <button class="btn btn-outline btn-sm" :disabled="page <= 1" @click="page--">
            ← Anterior
          </button>
          <span class="page-info">Página {{ page }} de {{ totalPages }}</span>
          <button class="btn btn-outline btn-sm" :disabled="page >= totalPages" @click="page++">
            Siguiente →
          </button>
        </nav>
      </template>
    </template>
  </div>
</template>

<style scoped>
.grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 16px;
}
.wrapper { display: flex; flex-direction: column; gap: 8px; }
.unsave { width: 100%; }
</style>
