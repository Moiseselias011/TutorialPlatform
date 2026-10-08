<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import api, { errorMessage } from '../api/client'
import { useAuthStore } from '../stores/auth'
import TutorialCard from '../components/TutorialCard.vue'

const auth = useAuthStore()
const route = useRoute()

// El mismo componente atiende todos los índices: «programación» (/),
// «dibujo» (/dibujo) y «marketing» (/marketing). El dominio lo decide la
// ruta y es lo que se le pide a la API; el texto de cada uno sale de aquí.
const domain = computed(() => route.meta.domain || 'programacion')

const INDICES = {
  programacion: {
    titulo: 'Tutoriales de la comunidad',
    subtitulo: 'Explora, filtra por tecnología y guarda lo que te interese en tu biblioteca.',
    filtro: 'tecnología',
    todas: 'Todas las tecnologías',
    vacio: 'No se encontraron tutoriales',
  },
  dibujo: {
    titulo: 'Tutoriales de dibujo',
    subtitulo: 'Explora, filtra por técnica y guarda lo que te interese en tu biblioteca.',
    filtro: 'técnica',
    todas: 'Todas las técnicas',
    vacio: 'No se encontraron tutoriales de dibujo',
  },
  marketing: {
    titulo: 'Tutoriales de marketing',
    subtitulo: 'Explora, filtra por estrategia y guarda lo que te interese en tu biblioteca.',
    filtro: 'estrategia',
    todas: 'Todas las estrategias',
    vacio: 'No se encontraron tutoriales de marketing',
  },
}

const indice = computed(() => INDICES[domain.value] ?? INDICES.programacion)

const tutorials = ref([])
const technologies = ref([])
const loading = ref(true)
const error = ref('')

// Filtros + paginación
const search = ref('')
const technology = ref('')   // id o slug (ComboBox)
const sort = ref('')
const page = ref(1)
const pageSize = 9
const total = ref(0)
const totalPages = ref(1)

let timer = null

// Guarda contra respuestas fuera de orden: load() se dispara desde el
// debounce del buscador, desde los ComboBox y desde la paginación. Si dos
// peticiones vuelan a la vez y la antigua llega la última, sobrescribiría
// los datos frescos con datos obsoletos.
let requestSeq = 0

async function load() {
  const seq = ++requestSeq
  loading.value = true
  error.value = ''
  try {
    const params = { page: page.value, pageSize, domain: domain.value }
    if (search.value.trim()) params.search = search.value.trim()
    if (technology.value) params.technology = technology.value
    if (sort.value) params.sort = sort.value

    const { data } = await api.get('/tutorials', { params })
    if (seq !== requestSeq) return // respuesta obsoleta
    tutorials.value = data.items
    total.value = data.totalCount
    totalPages.value = data.totalPages
  } catch (e) {
    if (seq !== requestSeq) return // fallo de una petición ya superada
    error.value = errorMessage(e, 'No se pudieron cargar los tutoriales.')
  } finally {
    if (seq === requestSeq) loading.value = false
  }
}

async function loadTechnologies() {
  try {
    const { data } = await api.get('/technologies', { params: { domain: domain.value } })
    technologies.value = data
  } catch {
    technologies.value = []
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

watch([technology, sort], () => {
  page.value = 1
  load()
})

watch(page, load)

// Al pasar de un índice al otro Vue Router REUTILIZA la instancia, así que
// onMounted no se vuelve a ejecutar: hay que vaciar los filtros (el de
// programación tendría tecnología de otro dominio) y recargar todo.
watch(domain, () => {
  search.value = ''
  technology.value = ''
  sort.value = ''
  page.value = 1
  clearTimeout(timer)
  loadTechnologies()
  load()
})

onMounted(() => {
  loadTechnologies()
  load()
})
</script>

<template>
  <div>
    <div class="hero">
      <div>
        <h1 class="section-title">{{ indice.titulo }}</h1>
        <p class="section-sub" style="margin: 0">{{ indice.subtitulo }}</p>
      </div>
      <!-- El dominio viaja en la consulta: sin él, el formulario pediría las
           tecnologías de programación y en /dibujo o /marketing no habría con
           qué crear. -->
      <router-link
        v-if="auth.isAuthenticated"
        class="btn btn-primary"
        :to="{ path: '/new-tutorial', query: { dominio: domain } }"
      >
        + Agregar tutorial
      </router-link>
    </div>

    <!-- Barra: buscador + ComboBox tecnología + orden -->
    <div class="toolbar">
      <input
        v-model="search"
        class="toolbar-search"
        type="search"
        placeholder="🔍 Buscar por título o descripción…"
        aria-label="Buscar tutoriales"
      />

      <!-- ComboBox de categoría: tecnología, técnica o estrategia según índice -->
      <select
        v-model="technology"
        class="toolbar-select"
        :aria-label="'Filtrar por ' + indice.filtro"
      >
        <option value="">{{ indice.todas }}</option>
        <option v-for="t in technologies" :key="t.id" :value="t.id">
          {{ t.name }} ({{ t.tutorialCount }})
        </option>
      </select>

      <select v-model="sort" class="toolbar-select" aria-label="Ordenar">
        <option value="">Más recientes</option>
        <option value="likes">Más populares</option>
        <option value="title">Título A-Z</option>
      </select>
    </div>

    <!-- Estado de error (tiene prioridad sobre el vacío: si falló, NO diremos
         "no se encontraron tutoriales", que sería un mensaje contradictorio) -->
    <div v-if="!loading && error" class="empty">
      <span class="empty-icon">⚠️</span>
      <p>{{ error }}</p>
      <button class="btn btn-outline" @click="load">Reintentar</button>
    </div>

    <!-- Estado de carga -->
    <div v-else-if="loading" class="empty">
      <span class="spinner dark" style="width: 30px; height: 30px" />
      <p>Cargando tutoriales…</p>
    </div>

    <!-- Estado vacío -->
    <div v-else-if="tutorials.length === 0" class="empty">
      <span class="empty-icon">🔍</span>
      <p>
        {{ indice.vacio }}
        <span v-if="search"> para «{{ search }}»</span>.
      </p>
      <button
        v-if="search || technology || sort"
        class="btn btn-outline"
        @click="search = ''; technology = ''; sort = ''"
      >
        Limpiar filtros
      </button>
    </div>

    <!-- Lista general -->
    <template v-else>
      <div class="grid">
        <TutorialCard v-for="t in tutorials" :key="t.id" :tutorial="t" @changed="load" />
      </div>

      <!-- Paginación -->
      <nav v-if="totalPages > 1" class="pagination" aria-label="Paginación">
        <button class="btn btn-outline btn-sm" :disabled="page <= 1" @click="page--">
          ← Anterior
        </button>

        <span class="page-info">
          Página <strong>{{ page }}</strong> de <strong>{{ totalPages }}</strong>
          <span class="muted"> · {{ total }} tutoriales</span>
        </span>

        <button class="btn btn-outline btn-sm" :disabled="page >= totalPages" @click="page++">
          Siguiente →
        </button>
      </nav>
    </template>
  </div>
</template>

<style scoped>
.hero {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 20px;
  flex-wrap: wrap;
  margin-bottom: 22px;
}

.grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 16px;
}
</style>
