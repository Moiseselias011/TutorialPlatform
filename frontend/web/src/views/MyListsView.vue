<script setup>
import { onMounted, ref } from 'vue'
import api, { errorMessage } from '../api/client'

const lists = ref([])
const loading = ref(true)
const error = ref('')
const notice = ref('')

const newName = ref('')
const editingId = ref(null)
const editName = ref('')
const busy = ref(false)

async function load() {
  loading.value = true
  error.value = ''
  try {
    const { data } = await api.get('/lists')
    lists.value = data
  } catch (e) {
    error.value = errorMessage(e, 'No se pudieron cargar tus listas.')
  } finally {
    loading.value = false
  }
}

onMounted(load)

async function create() {
  const name = newName.value.trim()
  if (name.length < 2) {
    error.value = 'El nombre debe tener al menos 2 caracteres.'
    return
  }
  error.value = ''
  busy.value = true
  try {
    const { data } = await api.post('/lists', { name })
    lists.value.unshift(data)
    newName.value = ''
    notice.value = `Lista "${data.name}" creada.`
  } catch (e) {
    error.value = errorMessage(e, 'No se pudo crear la lista.')
  } finally {
    busy.value = false
  }
}

async function rename(list) {
  const name = editName.value.trim()
  if (name.length < 2) return
  busy.value = true
  try {
    const { data } = await api.put(`/lists/${list.id}`, { name })
    const i = lists.value.findIndex((l) => l.id === list.id)
    if (i !== -1) lists.value[i] = { ...lists.value[i], ...data }
    editingId.value = null
    notice.value = 'Lista renombrada.'
  } catch (e) {
    error.value = errorMessage(e, 'No se pudo renombrar.')
  } finally {
    busy.value = false
  }
}

async function remove(list) {
  if (!confirm(`¿Eliminar la lista "${list.name}"? Los tutoriales NO se borran de la plataforma.`)) return
  busy.value = true
  try {
    await api.delete(`/lists/${list.id}`)
    lists.value = lists.value.filter((l) => l.id !== list.id)
    notice.value = 'Lista eliminada.'
  } catch (e) {
    error.value = errorMessage(e, 'No se pudo eliminar.')
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <div>
    <div class="row" style="margin-bottom: 20px">
      <div>
        <h1 class="section-title">Mis listas</h1>
        <p class="section-sub" style="margin: 0">Tu biblioteca personal de aprendizaje.</p>
      </div>
    </div>

    <!-- Crear lista -->
    <div class="card" style="margin-bottom: 20px">
      <form class="row" @submit.prevent="create">
        <div class="field" style="flex: 1; min-width: 240px">
          <label for="nname">Nueva lista</label>
          <input
            id="nname"
            v-model="newName"
            type="text"
            maxlength="120"
            placeholder="Ej: Vue para dominar"
          />
        </div>
        <button class="btn btn-primary" type="submit" :disabled="busy || newName.trim().length < 2" style="margin-top: 24px">
          ＋ Crear lista
        </button>
      </form>
    </div>

    <div v-if="notice" class="alert alert-success" style="margin-bottom: 16px">{{ notice }}</div>

    <!-- Estado de error (prioridad sobre el vacío + opción de reintentar) -->
    <div v-if="!loading && error" class="empty">
      <span class="empty-icon">⚠️</span>
      <p>{{ error }}</p>
      <button class="btn btn-outline" @click="load">Reintentar</button>
    </div>

    <div v-else-if="loading" class="empty">
      <span class="spinner dark" style="width: 30px; height: 30px" />
      <p>Cargando tus listas…</p>
    </div>

    <div v-else-if="lists.length === 0" class="empty">
      <span class="empty-icon">📁</span>
      <p>Aún no tienes listas. Crea una arriba y empieza a guardar tutoriales.</p>
    </div>

    <div v-else class="lists">
      <div v-for="l in lists" :key="l.id" class="card list-card">
        <template v-if="editingId === l.id">
          <div class="field">
            <input v-model="editName" maxlength="120" @keyup.enter="rename(l)" />
          </div>
          <div class="row" style="margin-top: 12px">
            <button class="btn btn-outline btn-sm" @click="editingId = null">Cancelar</button>
            <span class="spacer" />
            <button class="btn btn-primary btn-sm" :disabled="busy || editName.trim().length < 2" @click="rename(l)">
              Guardar
            </button>
          </div>
        </template>

        <template v-else>
          <router-link class="list-link" :to="`/my-lists/${l.id}`">
            <span class="list-icon">📁</span>
            <span>
              <span class="list-name">{{ l.name }}</span>
              <span class="list-count">{{ l.tutorialCount }} tutoriales</span>
            </span>
            <span class="chevron">→</span>
          </router-link>

          <div class="row" style="margin-top: 12px">
            <button
              class="btn btn-outline btn-sm"
              @click="editingId = l.id; editName = l.name"
            >
              ✏️ Renombrar
            </button>
            <span class="spacer" />
            <button class="btn btn-danger btn-sm" :disabled="busy" @click="remove(l)">
              🗑️ Eliminar
            </button>
          </div>
        </template>
      </div>
    </div>
  </div>
</template>

<style scoped>
.lists {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 15px;
}
.list-card { display: flex; flex-direction: column; }
.list-link {
  display: flex;
  align-items: center;
  gap: 13px;
  text-decoration: none;
  color: inherit;
  flex: 1;
}
.list-icon { font-size: 26px; }
.list-name { display: block; font-weight: 700; font-size: 15.5px; }
.list-count { display: block; font-size: 13px; color: var(--text-soft); }
.chevron { margin-left: auto; color: var(--text-soft); font-size: 18px; }
.list-link:hover .list-name { color: var(--primary); }
</style>
