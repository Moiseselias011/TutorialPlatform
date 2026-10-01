<script setup>
import { computed, onMounted, ref } from 'vue'
import api, { errorMessage } from '../api/client'

const props = defineProps({
  tutorial: { type: Object, required: true },
})
const emit = defineEmits(['close', 'saved'])

const lists = ref([])
const query = ref('')          // ← BUSCADOR
const selectedId = ref('')     // ← COMBOBOX
const creating = ref(false)    // ← crear lista nueva desde aquí
const newName = ref('')
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const success = ref('')

// Listas filtradas por el buscador (el filtro es en cliente: son pocas)
const filtered = computed(() => {
  const q = query.value.trim().toLowerCase()
  if (!q) return lists.value
  return lists.value.filter((l) => l.name.toLowerCase().includes(q))
})

async function loadLists() {
  try {
    const { data } = await api.get('/lists/search', { params: query.value.trim() ? { q: query.value.trim() } : {} })
    lists.value = data
    if (!selectedId.value && data.length) selectedId.value = data[0].id
  } catch (e) {
    error.value = errorMessage(e, 'No se pudieron cargar tus listas.')
  } finally {
    loading.value = false
  }
}

async function save() {
  error.value = ''
  success.value = ''

  if (!selectedId.value) {
    error.value = 'Selecciona una lista destino.'
    return
  }

  saving.value = true
  try {
    const { data } = await api.post(`/lists/${selectedId.value}/save`, {
      tutorialId: props.tutorial.id,
    })
    props.tutorial.savedByMe = true
    success.value = data.message || 'Guardado.'
    emit('saved', data)
    setTimeout(() => emit('close'), 750)
  } catch (e) {
    error.value = errorMessage(e, 'No se pudo guardar.')
  } finally {
    saving.value = false
  }
}

async function createList() {
  const name = newName.value.trim()
  if (name.length < 2) {
    error.value = 'El nombre debe tener al menos 2 caracteres.'
    return
  }
  error.value = ''
  saving.value = true
  try {
    const { data } = await api.post('/lists', { name })
    lists.value.push(data)
    selectedId.value = data.id
    creating.value = false
    newName.value = ''
    success.value = `Lista "${data.name}" creada.`
  } catch (e) {
    error.value = errorMessage(e, 'No se pudo crear la lista.')
  } finally {
    saving.value = false
  }
}

function onOverlay(e) {
  if (e.target === e.currentTarget) emit('close')
}

onMounted(loadLists)
</script>

<template>
  <div class="overlay" @click="onOverlay" @keydown.esc="$emit('close')">
    <div class="modal" role="dialog" aria-modal="true" aria-label="Guardar tutorial">
      <header class="modal-head">
        <div>
          <h3>Guardar tutorial</h3>
          <p class="muted" style="margin: 3px 0 0; font-size: 13px">«{{ tutorial.title }}»</p>
        </div>
        <button class="x" aria-label="Cerrar" @click="$emit('close')">✕</button>
      </header>

      <div class="modal-body">
        <div v-if="error" class="alert alert-error">{{ error }}</div>
        <div v-if="success" class="alert alert-success">{{ success }}</div>

        <!-- ===== BUSCADOR ===== -->
        <div class="field">
          <label for="q">Buscar una de tus listas</label>
          <input
            id="q"
            v-model="query"
            type="search"
            placeholder="🔍 Escribe para filtrar tus listas…"
            @input="loadLists"
          />
        </div>

        <!-- ===== COMBOBOX ===== -->
        <div class="field">
          <label for="combo">Lista destino</label>
          <select id="combo" v-model="selectedId" :disabled="loading || lists.length === 0">
            <option value="" disabled>
              {{ loading ? 'Cargando…' : lists.length ? '— Selecciona una lista —' : 'No tienes listas aún' }}
            </option>
            <option v-for="l in filtered" :key="l.id" :value="l.id">
              {{ l.name }} ({{ l.tutorialCount }})
            </option>
          </select>
          <span class="hint">
            Mostrando {{ filtered.length }} de {{ lists.length }} listas.
          </span>
        </div>

        <!-- ===== CREAR LISTA DESDE AQUÍ ===== -->
        <div v-if="!creating" class="new-toggle">
          <button class="btn btn-outline btn-sm" @click="creating = true">
            ＋ Crear una lista nueva
          </button>
        </div>

        <div v-else class="new-box">
          <div class="field">
            <label for="newname">Nombre de la nueva lista</label>
            <input
              id="newname"
              v-model="newName"
              type="text"
              maxlength="120"
              placeholder="Ej: Aprendizaje 2026"
              @keyup.enter="createList"
            />
          </div>
          <div class="row">
            <button class="btn btn-outline btn-sm" @click="creating = false; newName = ''">
              Cancelar
            </button>
            <span class="spacer" />
            <button class="btn btn-primary btn-sm" :disabled="saving || newName.trim().length < 2" @click="createList">
              Crear y usar
            </button>
          </div>
        </div>
      </div>

      <footer class="modal-foot">
        <button class="btn btn-ghost" style="color: #64748b; border-color: var(--border)" @click="$emit('close')">
          Cancelar
        </button>
        <button class="btn btn-primary" :disabled="saving || !selectedId" @click="save">
          <span v-if="saving" class="spinner" />
          {{ saving ? 'Guardando…' : 'Guardar aquí' }}
        </button>
      </footer>
    </div>
  </div>
</template>

<style scoped>
.overlay {
  position: fixed;
  inset: 0;
  background: rgba(15, 23, 42, .55);
  display: grid;
  place-items: center;
  z-index: 100;
  padding: 20px;
}
.modal {
  background: #fff;
  border-radius: 14px;
  width: 100%;
  max-width: 470px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, .25);
  overflow: hidden;
}
.modal-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  padding: 18px 20px;
  border-bottom: 1px solid var(--border);
}
.modal-head h3 { margin: 0; font-size: 17px; }
.x {
  border: none;
  background: none;
  font-size: 17px;
  cursor: pointer;
  color: var(--text-soft);
  padding: 2px 6px;
  border-radius: 6px;
}
.x:hover { background: #f1f5f9; color: var(--text); }

.modal-body { padding: 20px; display: flex; flex-direction: column; gap: 15px; }

.new-toggle { padding-top: 2px; }
.new-box {
  background: #f8fafc;
  border: 1px dashed var(--border);
  border-radius: 10px;
  padding: 14px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.modal-foot {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  padding: 15px 20px;
  border-top: 1px solid var(--border);
  background: #f8fafc;
}
</style>
