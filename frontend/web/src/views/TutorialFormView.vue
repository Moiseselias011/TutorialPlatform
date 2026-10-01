<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api, { errorMessage } from '../api/client'

const route = useRoute()
const router = useRouter()

const isEdit = computed(() => Boolean(route.params.id))

const technologies = ref([])
const form = ref({
  title: '',
  description: '',
  url: '',
  imageUrl: '',
  technologyId: '',
})
const loading = ref(false)
const fetching = ref(isEdit.value)
const error = ref('')
const uploading = ref(false)

// Validaciones en cliente (espejo del servidor)
const urlError = computed(() => {
  const v = form.value.url.trim()
  if (!v) return 'La URL es obligatoria.'
  try {
    const u = new URL(v)
    return ['http:', 'https:'].includes(u.protocol) ? '' : 'Debe ser http:// o https://'
  } catch {
    return 'URL no válida.'
  }
})

const isValid = computed(
  () =>
    form.value.title.trim().length >= 3 &&
    form.value.description.trim().length >= 10 &&
    !urlError.value &&
    form.value.technologyId !== '',
)

async function loadTechnologies() {
  try {
    const { data } = await api.get('/technologies')
    technologies.value = data
  } catch (e) {
    error.value = errorMessage(e, 'No se pudieron cargar las tecnologías.')
  }
}

async function loadTutorial() {
  fetching.value = true
  error.value = ''
  try {
    const { data } = await api.get(`/tutorials/${route.params.id}`)
    if (!data.canEdit) {
      error.value = 'No tienes permiso para editar este tutorial.'
      return
    }
    form.value = {
      title: data.title,
      description: data.description,
      url: data.url,
      imageUrl: data.imageUrl || '',
      technologyId: data.technologyId,
    }
  } catch (e) {
    error.value = errorMessage(e, 'No se pudo cargar el tutorial.')
  } finally {
    fetching.value = false
  }
}

onMounted(async () => {
  await loadTechnologies()
  if (isEdit.value) await loadTutorial()
})

/** Sube la imagen y guarda la URL devuelta en el formulario. */
async function onFile(e) {
  const file = e.target.files?.[0]
  if (!file) return

  error.value = ''
  if (file.size > 5 * 1024 * 1024) {
    error.value = 'La imagen no puede superar 5 MB.'
    e.target.value = ''
    return
  }

  uploading.value = true
  try {
    const fd = new FormData()
    fd.append('file', file)
    const { data } = await api.post('/images', fd, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    form.value.imageUrl = data.url
  } catch (err) {
    error.value = errorMessage(err, 'No se pudo subir la imagen.')
  } finally {
    uploading.value = false
  }
}

async function submit() {
  error.value = ''
  if (!isValid.value) {
    error.value = 'Revisa los campos marcados.'
    return
  }

  loading.value = true
  try {
    const payload = {
      title: form.value.title.trim(),
      description: form.value.description.trim(),
      url: form.value.url.trim(),
      imageUrl: form.value.imageUrl.trim() || null,
      technologyId: Number(form.value.technologyId),
    }

    if (isEdit.value) {
      await api.put(`/tutorials/${route.params.id}`, payload)
      router.push(`/tutorials/${route.params.id}`)
    } else {
      const { data } = await api.post('/tutorials', payload)
      router.push(`/tutorials/${data.id}`)
    }
  } catch (e) {
    error.value = errorMessage(e, 'No se pudo guardar el tutorial.')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div style="max-width: 720px; margin: 0 auto">
    <button class="btn btn-outline btn-sm" @click="router.back()">← Volver</button>

    <div class="card mt-2">
      <h1 class="section-title">{{ isEdit ? 'Editar tutorial' : 'Agregar tutorial' }}</h1>
      <p class="section-sub">
        {{
          isEdit
            ? 'Solo tú (o un administrador) puedes modificar este tutorial.'
            : 'Se publicará en la lista general de la tecnología que elijas.'
        }}
      </p>

      <div v-if="error" class="alert alert-error" style="margin-bottom: 16px">{{ error }}</div>

      <div v-if="fetching" class="empty">
        <span class="spinner dark" style="width: 28px; height: 28px" />
        <p>Cargando tutorial…</p>
      </div>

      <form v-else class="form" @submit.prevent="submit">
        <div class="field">
          <label for="title">Título</label>
          <input
            id="title"
            v-model="form.title"
            type="text"
            maxlength="200"
            placeholder="Ej: Vue 3 desde cero"
            required
          />
          <span class="hint">{{ form.title.length }}/200 · mínimo 3 caracteres</span>
        </div>

        <div class="field">
          <label for="desc">Descripción</label>
          <textarea
            id="desc"
            v-model="form.description"
            maxlength="2000"
            placeholder="Qué aprenderá quien lo abra…"
            required
          />
          <span class="hint">{{ form.description.length }}/2000 · mínimo 10 caracteres</span>
        </div>

        <div class="field">
          <label for="url">URL del tutorial</label>
          <input
            id="url"
            v-model="form.url"
            type="url"
            maxlength="1000"
            placeholder="https://…"
            required
          />
          <span v-if="urlError && form.url" class="hint" style="color: var(--danger)">{{ urlError }}</span>
        </div>

        <!-- ComboBox de tecnología -->
        <div class="field">
          <label for="tech">Tecnología</label>
          <select id="tech" v-model="form.technologyId" required>
            <option value="" disabled>— Selecciona una tecnología —</option>
            <option v-for="t in technologies" :key="t.id" :value="t.id">
              {{ t.name }}
            </option>
          </select>
        </div>

        <div class="field">
          <label for="img">Imagen</label>
          <input id="img" type="file" accept="image/*" @change="onFile" />
          <span class="hint">
            <template v-if="uploading">Subiendo imagen…</template>
            <template v-else-if="form.imageUrl">✅ Imagen lista: {{ form.imageUrl }}</template>
            <template v-else>Opcional · JPG, PNG, GIF, WEBP o SVG · máx. 5 MB</template>
          </span>
          <img
            v-if="form.imageUrl"
            :src="form.imageUrl"
            alt="Vista previa"
            style="max-width: 100%; max-height: 180px; border-radius: 8px; margin-top: 8px; border: 1px solid var(--border)"
          />
        </div>

        <div class="row">
          <button class="btn btn-outline" type="button" @click="router.back()">Cancelar</button>
          <span class="spacer" />
          <button class="btn btn-primary" type="submit" :disabled="loading || uploading || !isValid">
            <span v-if="loading" class="spinner" />
            {{ loading ? 'Guardando…' : isEdit ? 'Guardar cambios' : 'Publicar tutorial' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
