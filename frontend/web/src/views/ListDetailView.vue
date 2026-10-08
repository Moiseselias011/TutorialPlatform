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

// Búsqueda y paginación DENTRO de la lista personal, resueltas en el SERVIDOR
// con el mismo patrón que HomeView: una lista no tiene tope de tutoriales, así
// que no se descarga entera para cortarla en el navegador.
const search = ref('')
const page = ref(1)
const pageSize = 6

const items = computed(() => list.value?.tutorials?.items ?? [])
const totalCount = computed(() => list.value?.tutorials?.totalCount ?? 0)
const totalPages = computed(() => list.value?.tutorials?.totalPages ?? 1)

let timer = null

// Guarda contra respuestas fuera de orden: load() se dispara desde el debounce
// del buscador y desde la paginación. Si dos peticiones vuelan a la vez y la
// antigua llega la última, sobrescribiría los datos frescos con los obsoletos.
let requestSeq = 0

async function load() {
  const seq = ++requestSeq
  loading.value = true
  error.value = ''
  try {
    const params = { page: page.value, pageSize }
    if (search.value.trim()) params.search = search.value.trim()

    const { data } = await api.get(`/lists/${route.params.id}`, { params })
    if (seq !== requestSeq) return // respuesta obsoleta
    list.value = data

    // Si la página quedó fuera de rango (p. ej. al quitar el último tutorial
    // de la última página) se recorta al último valor válido y el watch de
    // `page` recarga. totalPages nunca baja de 1, así que esto termina siempre.
    if (page.value > data.tutorials.totalPages) page.value = data.tutorials.totalPages
  } catch (e) {
    if (seq !== requestSeq) return // fallo de una petición ya superada
    error.value = errorMessage(e, 'No se pudo cargar la lista.')
  } finally {
    if (seq === requestSeq) loading.value = false
  }
}

// Debounce del buscador
watch(search, () => {
  clearTimeout(timer)
  timer = setTimeout(() => {
    page.value = 1
    load()
  }, 320)
})

watch(page, load)

// Al cambiar de lista se vacía la búsqueda (si no, seguiría aplicada la de la
// anterior) y se recarga desde la primera página.
function reload() {
  search.value = ''
  page.value = 1
  clearTimeout(timer)
  load()
}

onMounted(reload)
watch(() => route.params.id, reload)

async function remove(tutorial) {
  if (!confirm(`¿Quitar "${tutorial.title}" de esta lista? El tutorial sigue en la plataforma.`)) return
  busy.value = true
  try {
    await api.delete(`/lists/${list.value.id}/save/${tutorial.id}`)
    notice.value = 'Quitado de la lista.'
    // Recarga desde el servidor: si se quitó el último de la última página,
    // load() recorta la página en lugar de dejar la vista en blanco.
    await load()
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
            {{ list.tutorialCount }} tutoriales guardados ·
            creada el {{ new Date(list.createdAt).toLocaleDateString() }}
          </p>
        </div>
      </div>

      <div v-if="notice" class="alert alert-success" style="margin-bottom: 16px">{{ notice }}</div>

      <!-- Lista totalmente vacía -->
      <div v-if="list.tutorialCount === 0" class="empty">
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
            {{ totalCount }} de {{ list.tutorialCount }}
          </span>
        </div>

        <!-- Tiene tutoriales pero ninguno coincide con la búsqueda -->
        <div v-if="totalCount === 0" class="empty">
          <span class="empty-icon">🔍</span>
          <p>No hay tutoriales que coincidan con «{{ search }}».</p>
          <button class="btn btn-outline" @click="search = ''">Limpiar búsqueda</button>
        </div>

        <div v-else class="grid">
          <div v-for="t in items" :key="t.id" class="wrapper">
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
