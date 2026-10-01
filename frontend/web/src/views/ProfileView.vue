<script setup>
import { computed, onMounted, ref } from 'vue'
import { useAuthStore } from '../stores/auth'
import api, { errorMessage } from '../api/client'

const auth = useAuthStore()

const stats = ref({ tutorials: 0, comments: 0, lists: 0, likes: 0 })
const loading = ref(true)
const error = ref('')

// Formulario de edición del propio perfil
const form = ref({ email: '', currentPassword: '', newPassword: '' })
// Separados a propósito: si se usa un único "notice" para éxito y fallo,
// el error acaba pintado en verde con alert-success.
const notice = ref('')
const formError = ref('')
const saving = ref(false)

// ---- Foto de perfil ----
// Se sube con POST /api/images (mismo endpoint y misma validación que el
// servidor) y luego se fija la ruta devuelta con PUT /api/auth/me.
const photoInput = ref(null)
const uploading = ref(false)
const photoError = ref('')
// Aviso propio de la foto: si se reutiliza `notice`, el mensaje de la foto
// aparecería dentro de la tarjeta «Datos de la cuenta», que es de otro formulario.
const photoNotice = ref('')

// Espejo exacto de las allow-lists de ImagesController.
const ALLOWED_PHOTO_EXT = ['.jpg', '.jpeg', '.png', '.gif', '.webp', '.svg']
const MAX_PHOTO_BYTES = 5 * 1024 * 1024

async function loadStats() {
  loading.value = true
  error.value = ''
  try {
    const [tuts, lists] = await Promise.all([
      api.get('/tutorials', { params: { pageSize: 50 } }),
      api.get('/lists'),
    ])

    // Cuenta los tutoriales propios filtrando por el autor actual
    const mine = (tuts.data.items || []).filter((t) => t.authorId === auth.user?.id)

    stats.value = {
      tutorials: mine.length,
      comments: mine.reduce((acc, t) => acc + (t.commentCount || 0), 0),
      likes: mine.reduce((acc, t) => acc + (t.likeCount || 0), 0),
      lists: (lists.data || []).length,
    }
  } catch (e) {
    // Sin error los tarjetas mostrarían "0" y parecería que el usuario no
    // tiene nada; es mejor decir que no se pudieron cargar los datos.
    error.value = errorMessage(e, 'No se pudieron cargar las estadísticas.')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  form.value.email = auth.user?.email ?? ''
  loadStats()
})

const cards = computed(() => [
  { label: 'Tutoriales publicados', value: stats.value.tutorials, icon: '📝' },
  { label: 'Me gusta recibidos', value: stats.value.likes, icon: '👍' },
  { label: 'Comentarios recibidos', value: stats.value.comments, icon: '💬' },
  { label: 'Listas personales', value: stats.value.lists, icon: '📁' },
])

/** Abre el selector de archivo del navegador. */
function pickPhoto() {
  photoInput.value?.click()
}

async function onPhotoSelected(event) {
  const file = event.target.files?.[0]
  // Se limpia siempre: así se puede volver a elegir el mismo archivo.
  event.target.value = ''
  if (!file) return

  photoError.value = ''
  photoNotice.value = ''

  // Validación en cliente, espejada con ImagesController.
  const ext = '.' + (file.name.split('.').pop() || '').toLowerCase()
  if (!ALLOWED_PHOTO_EXT.includes(ext)) {
    photoError.value = `Tipo no permitido. Usa: ${ALLOWED_PHOTO_EXT.join(', ')}`
    return
  }
  if (file.size > MAX_PHOTO_BYTES) {
    photoError.value = 'La imagen no puede superar 5 MB.'
    return
  }

  uploading.value = true
  try {
    const body = new FormData()
    body.append('file', file)
    const { data } = await api.post('/images', body)
    // Cadena vacía en el servidor = eliminar; aquí enviamos la ruta nueva.
    await api.put('/auth/me', { photoUrl: data.url })
    await auth.fetchMe()
    photoNotice.value = 'Foto de perfil actualizada.'
  } catch (e) {
    photoError.value = errorMessage(e, 'No se pudo subir la imagen.')
  } finally {
    uploading.value = false
  }
}

async function removePhoto() {
  photoError.value = ''
  photoNotice.value = ''
  uploading.value = true
  try {
    await api.put('/auth/me', { photoUrl: '' })
    await auth.fetchMe()
    photoNotice.value = 'Foto de perfil eliminada.'
  } catch (e) {
    photoError.value = errorMessage(e, 'No se pudo eliminar la foto.')
  } finally {
    uploading.value = false
  }
}

async function save() {
  notice.value = ''
  formError.value = ''

  // Validación en cliente, espejada con el servidor (MinLength(6))
  if (form.value.newPassword && form.value.newPassword.length < 6) {
    formError.value = 'La contraseña nueva debe tener al menos 6 caracteres.'
    return
  }
  if (form.value.newPassword && !form.value.currentPassword) {
    formError.value = 'Indica tu contraseña actual para cambiarla.'
    return
  }

  saving.value = true
  try {
    const payload = { email: form.value.email }
    if (form.value.newPassword) {
      payload.newPassword = form.value.newPassword
      payload.currentPassword = form.value.currentPassword
    }
    await api.put('/auth/me', payload)
    await auth.fetchMe()
    notice.value = 'Perfil actualizado correctamente.'
    form.value.currentPassword = ''
    form.value.newPassword = ''
  } catch (e) {
    formError.value = errorMessage(e, 'No se pudo actualizar el perfil.')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div>
    <h1 class="section-title">Mi perfil</h1>
    <p class="section-sub">
      Solo tú puedes ver y editar esta información. No existen perfiles públicos.
    </p>

    <!-- Estado de error de las estadísticas (con reintentar) -->
    <div v-if="!loading && error" class="empty">
      <span class="empty-icon">⚠️</span>
      <p>{{ error }}</p>
      <button class="btn btn-outline" @click="loadStats">Reintentar</button>
    </div>

    <!-- Ocultas durante el error para no mostrar "0" junto al aviso de fallo -->
    <div v-if="!error" class="stats-grid">
      <div v-for="c in cards" :key="c.label" class="card stat">
        <span class="stat-icon">{{ c.icon }}</span>
        <div>
          <div class="stat-value">{{ loading ? '…' : c.value }}</div>
          <div class="stat-label">{{ c.label }}</div>
        </div>
      </div>
    </div>

    <!-- Foto de perfil: subida propia, no depende del formulario de la cuenta -->
    <div class="card mt-2">
      <h2 style="font-size: 17px; margin: 0 0 4px">Foto de perfil</h2>
      <p class="hint" style="margin: 0 0 16px">
        Opcional. No existen perfiles públicos: solo tú puedes verla.
      </p>

      <div v-if="photoError" class="alert alert-error" style="margin-bottom: 16px">
        {{ photoError }}
      </div>
      <div v-if="photoNotice" class="alert alert-success" style="margin-bottom: 16px">
        {{ photoNotice }}
      </div>

      <div class="photo-row">
        <div class="avatar avatar-lg">
          <img v-if="auth.user?.photoUrl" :src="auth.user.photoUrl" alt="Foto de perfil" />
          <span v-else>{{ (auth.user?.username || '?').charAt(0).toUpperCase() }}</span>
        </div>

        <div class="photo-actions">
          <input
            ref="photoInput"
            type="file"
            accept=".jpg,.jpeg,.png,.gif,.webp,.svg"
            hidden
            @change="onPhotoSelected"
          />
          <div class="photo-buttons">
            <button class="btn btn-outline" type="button" :disabled="uploading" @click="pickPhoto">
              <span v-if="uploading" class="spinner" />
              {{ uploading ? 'Subiendo…' : auth.user?.photoUrl ? 'Cambiar foto' : 'Subir foto' }}
            </button>
            <button
              v-if="auth.user?.photoUrl"
              class="btn btn-outline"
              type="button"
              :disabled="uploading"
              @click="removePhoto"
            >
              Quitar
            </button>
          </div>
          <span class="hint">JPG, PNG, GIF, WEBP o SVG · máximo 5 MB.</span>
        </div>
      </div>
    </div>

    <div class="card mt-2">
      <h2 style="font-size: 17px; margin: 0 0 16px">Datos de la cuenta</h2>

      <div v-if="formError" class="alert alert-error" style="margin-bottom: 16px">
        {{ formError }}
      </div>

      <div v-if="notice" class="alert alert-success" style="margin-bottom: 16px">
        {{ notice }}
      </div>

      <form class="form" @submit.prevent="save">
        <div class="row">
          <div class="field" style="flex: 1">
            <label>Usuario</label>
            <input :value="auth.user?.username" disabled />
            <span class="hint">El nombre de usuario no se puede cambiar.</span>
          </div>
          <div class="field" style="flex: 1">
            <label>Rol</label>
            <input :value="auth.user?.role" disabled />
          </div>
        </div>

        <div class="field">
          <label for="email">Correo</label>
          <input id="email" v-model="form.email" type="email" required />
        </div>

        <hr style="border: none; border-top: 1px solid var(--border); margin: 6px 0" />

        <div class="field">
          <label for="cp">Contraseña actual</label>
          <input
            id="cp"
            v-model="form.currentPassword"
            type="password"
            autocomplete="current-password"
            placeholder="Necesaria para cambiar la contraseña"
          />
        </div>

        <div class="field">
          <label for="np">Nueva contraseña</label>
          <input
            id="np"
            v-model="form.newPassword"
            type="password"
            autocomplete="new-password"
            placeholder="Déjalo vacío para no cambiarla"
          />
          <span class="hint">Mínimo 6 caracteres.</span>
        </div>

        <div class="row">
          <span class="spacer" />
          <button class="btn btn-primary" type="submit" :disabled="saving">
            <span v-if="saving" class="spinner" />
            {{ saving ? 'Guardando…' : 'Guardar cambios' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<style scoped>
.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 14px;
}
.stat { display: flex; align-items: center; gap: 14px; padding: 18px; }
.stat-icon { font-size: 26px; }
.stat-value { font-size: 26px; font-weight: 700; line-height: 1.1; }
.stat-label { font-size: 13px; color: var(--text-soft); }

.photo-row { display: flex; align-items: center; gap: 18px; flex-wrap: wrap; }
.photo-actions { display: flex; flex-direction: column; gap: 8px; }
.photo-buttons { display: flex; gap: 10px; flex-wrap: wrap; }

/* Avatar: foto si existe, inicial del usuario si no. */
.avatar {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 56px;
  height: 56px;
  border-radius: 50%;
  background: var(--border);
  color: var(--text-soft);
  font-weight: 700;
  font-size: 22px;
  overflow: hidden;
  flex-shrink: 0;
  text-transform: uppercase;
}
.avatar-lg { width: 88px; height: 88px; font-size: 34px; }
.avatar img { width: 100%; height: 100%; object-fit: cover; display: block; }
</style>
