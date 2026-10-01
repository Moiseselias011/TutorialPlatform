<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api, { errorMessage } from '../api/client'
import { useAuthStore } from '../stores/auth'
import { youtubeThumb } from '../utils/youtube'
import SaveTutorialModal from '../components/SaveTutorialModal.vue'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const tutorial = ref(null)
const comments = ref([])
const loading = ref(true)
const error = ref('')

// Comentario nuevo / edición
const newComment = ref('')
const editingId = ref(null)
const editText = ref('')
const busy = ref(false)
const showSave = ref(false)
const notice = ref('')

// Miniatura de cabecera: imagen subida por el autor y, si no la hay,
// la miniatura derivada de la URL de YouTube. Si la imagen falla al
// cargarse se oculta y se muestra solo el contenido del tutorial.
const heroFailed = ref(false)
const heroImage = computed(() =>
  heroFailed.value
    ? null
    : tutorial.value?.imageUrl || youtubeThumb(tutorial.value?.url),
)

async function load() {
  loading.value = true
  error.value = ''
  heroFailed.value = false
  try {
    const id = route.params.id
    const [t, c] = await Promise.all([
      api.get(`/tutorials/${id}`),
      api.get(`/tutorials/${id}/comments`),
    ])
    tutorial.value = t.data
    comments.value = c.data
  } catch (e) {
    error.value = errorMessage(e, 'No se pudo cargar el tutorial.')
  } finally {
    loading.value = false
  }
}

onMounted(load)

async function toggleLike() {
  if (!auth.isAuthenticated) return
  busy.value = true
  try {
    const { data } = await api.post(`/tutorials/${tutorial.value.id}/likes`)
    tutorial.value.likedByMe = data.liked
    tutorial.value.likeCount = data.likeCount
  } catch (e) {
    notice.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

async function submitComment() {
  const content = newComment.value.trim()
  if (!content) return
  busy.value = true
  try {
    const { data } = await api.post(`/tutorials/${tutorial.value.id}/comments`, { content })
    comments.value.push(data)
    newComment.value = ''
    tutorial.value.commentCount = comments.value.length
  } catch (e) {
    notice.value = errorMessage(e, 'No se pudo publicar el comentario.')
  } finally {
    busy.value = false
  }
}

function startEdit(c) {
  editingId.value = c.id
  editText.value = c.content
}

async function saveEdit(c) {
  const content = editText.value.trim()
  if (!content) return
  busy.value = true
  try {
    const { data } = await api.put(`/tutorials/${tutorial.value.id}/comments/${c.id}`, { content })
    const i = comments.value.findIndex((x) => x.id === c.id)
    if (i !== -1) comments.value[i] = data
    editingId.value = null
  } catch (e) {
    notice.value = errorMessage(e, 'No se pudo editar.')
  } finally {
    busy.value = false
  }
}

async function removeComment(c) {
  if (!confirm('¿Eliminar este comentario?')) return
  busy.value = true
  try {
    await api.delete(`/tutorials/${tutorial.value.id}/comments/${c.id}`)
    comments.value = comments.value.filter((x) => x.id !== c.id)
    tutorial.value.commentCount = comments.value.length
  } catch (e) {
    notice.value = errorMessage(e, 'No se pudo eliminar.')
  } finally {
    busy.value = false
  }
}

async function removeTutorial() {
  if (!auth.isAdmin) return
  if (!confirm('¿Eliminar este tutorial de la plataforma? Esta acción no se puede deshacer.')) return
  busy.value = true
  try {
    await api.delete(`/tutorials/${tutorial.value.id}`)
    router.push('/')
  } catch (e) {
    notice.value = errorMessage(e, 'No se pudo eliminar.')
  } finally {
    busy.value = false
  }
}
</script>

<template>
  <div>
    <button class="btn btn-outline btn-sm" @click="router.back()">← Volver</button>

    <div v-if="loading" class="empty">
      <span class="spinner dark" style="width: 30px; height: 30px" />
      <p>Cargando…</p>
    </div>

    <div v-else-if="error" class="empty">
      <span class="empty-icon">⚠️</span>
      <p>{{ error }}</p>
      <button class="btn btn-outline" @click="load">Reintentar</button>
    </div>

    <template v-else-if="tutorial">
      <div class="card mt-2">
        <div class="row" style="align-items: flex-start">
          <span class="pill">{{ tutorial.technology }}</span>
          <span class="spacer" />

          <!-- Editar: solo autor o ADMIN -->
          <router-link
            v-if="tutorial.canEdit"
            class="btn btn-outline btn-sm"
            :to="`/tutorials/${tutorial.id}/edit`"
          >
            ✏️ Editar
          </router-link>

          <!-- Eliminar: SOLO ADMIN -->
          <button
            v-if="tutorial.canDelete"
            class="btn btn-danger btn-sm"
            :disabled="busy"
            @click="removeTutorial"
          >
            🗑️ Eliminar
          </button>
        </div>

        <!-- Cabecera visual: miniatura de YouTube o imagen subida por el autor.
             Toda la imagen es un enlace al propio tutorial. -->
        <a
          v-if="heroImage"
          class="hero"
          :href="tutorial.url"
          target="_blank"
          rel="noopener"
          :title="`Abrir ${tutorial.title}`"
        >
          <img :src="heroImage" :alt="tutorial.title" @error="heroFailed = true" />
          <span class="hero-play" aria-hidden="true">▶</span>
        </a>

        <h1 style="font-size: 26px; margin: 14px 0 8px">{{ tutorial.title }}</h1>
        <p class="muted" style="margin: 0 0 16px; line-height: 1.6">{{ tutorial.description }}</p>

        <div class="row meta-row">
          <span>✍️ <strong>{{ tutorial.author }}</strong></span>
          <span>📅 {{ new Date(tutorial.publishedAt).toLocaleString() }}</span>
          <span>👍 {{ tutorial.likeCount }} Me gusta</span>
          <span>💬 {{ tutorial.commentCount }} comentarios</span>
        </div>

        <div class="row mt-2">
          <a class="btn btn-primary" :href="tutorial.url" target="_blank" rel="noopener">
            Abrir tutorial ↗
          </a>

          <button
            class="btn btn-outline"
            :disabled="busy || !auth.isAuthenticated"
            :title="auth.isAuthenticated ? '' : 'Inicia sesión para dar Me gusta'"
            @click="toggleLike"
          >
            👍 {{ tutorial.likedByMe ? 'Te gusta' : 'Me gusta' }}
          </button>

          <button
            v-if="auth.isAuthenticated"
            class="btn btn-outline"
            @click="showSave = true"
          >
            🔖 {{ tutorial.savedByMe ? 'Guardado' : 'Guardar en mi lista' }}
          </button>
        </div>

        <div v-if="notice" class="alert alert-error" style="margin-top: 14px">{{ notice }}</div>
      </div>

      <!-- ============ COMENTARIOS ============ -->
      <div class="card mt-2">
        <h2 style="font-size: 18px; margin: 0 0 16px">Comentarios ({{ comments.length }})</h2>

        <!-- Nuevo comentario -->
        <div v-if="auth.isAuthenticated" class="comment-form">
          <textarea
            v-model="newComment"
            placeholder="Escribe un comentario…"
            maxlength="1000"
            rows="3"
          />
          <div class="row">
            <span class="hint">{{ newComment.length }}/1000</span>
            <span class="spacer" />
            <button
              class="btn btn-primary btn-sm"
              :disabled="busy || !newComment.trim()"
              @click="submitComment"
            >
              Publicar
            </button>
          </div>
        </div>
        <p v-else class="hint" style="margin-bottom: 14px">
          <router-link class="link" to="/login">Inicia sesión</router-link> para comentar.
        </p>

        <!-- Lista de comentarios -->
        <div v-if="comments.length === 0" class="empty" style="padding: 26px">
          <span class="empty-icon" style="font-size: 28px">💬</span>
          <p>Sé el primero en comentar.</p>
        </div>

        <ul v-else class="comment-list">
          <li v-for="c in comments" :key="c.id" class="comment">
            <div class="c-head">
              <span class="c-avatar">{{ c.author.slice(0, 2).toUpperCase() }}</span>
              <strong>{{ c.author }}</strong>
              <span class="muted" style="font-size: 12.5px">
                {{ new Date(c.createdAt).toLocaleString() }}
                <template v-if="c.updatedAt"> · editado</template>
              </span>
              <span class="spacer" />

              <!-- Editar / eliminar: autor o ADMIN -->
              <template v-if="c.canEdit">
                <button class="mini" @click="startEdit(c)">✏️</button>
                <button class="mini" :disabled="busy" @click="removeComment(c)">🗑️</button>
              </template>
            </div>

            <template v-if="editingId === c.id">
              <textarea v-model="editText" rows="2" maxlength="1000" />
              <div class="row" style="margin-top: 8px">
                <span class="spacer" />
                <button class="btn btn-outline btn-sm" @click="editingId = null">Cancelar</button>
                <button class="btn btn-primary btn-sm" :disabled="busy || !editText.trim()" @click="saveEdit(c)">
                  Guardar
                </button>
              </div>
            </template>
            <p v-else class="c-text">{{ c.content }}</p>
          </li>
        </ul>
      </div>
    </template>

    <SaveTutorialModal
      v-if="showSave && tutorial"
      :tutorial="tutorial"
      @close="showSave = false"
      @changed="load"
    />
  </div>
</template>

<style scoped>
.pill {
  background: #eef2ff;
  color: var(--primary);
  font-size: 12px;
  font-weight: 700;
  padding: 5px 12px;
  border-radius: 999px;
}
.meta-row { gap: 18px; font-size: 13.5px; color: var(--text-soft); }

/* Miniatura de cabecera: 16:9, enlace al tutorial con botón de reproducción. */
.hero {
  position: relative;
  display: block;
  margin-top: 14px;
  border-radius: 12px;
  overflow: hidden;
  background: #0f172a;
  aspect-ratio: 16 / 9;
  max-height: 340px;
  width: 100%;
}
.hero img { width: 100%; height: 100%; object-fit: cover; display: block; }
.hero-play {
  position: absolute;
  inset: 0;
  margin: auto;
  width: 62px;
  height: 62px;
  display: grid;
  place-items: center;
  border-radius: 50%;
  background: rgba(15, 23, 42, .72);
  color: #fff;
  font-size: 22px;
  padding-left: 4px;
  transition: background .15s, transform .15s;
}
.hero:hover .hero-play { background: rgba(15, 23, 42, .9); transform: scale(1.06); }

.comment-form { display: flex; flex-direction: column; gap: 10px; margin-bottom: 20px; }
.comment-form textarea,
.comment textarea {
  padding: 11px 13px;
  border: 1px solid var(--border);
  border-radius: 8px;
  font-family: inherit;
  font-size: 14.5px;
  resize: vertical;
}
.comment-form textarea:focus,
.comment textarea:focus {
  outline: none;
  border-color: var(--primary);
  box-shadow: 0 0 0 3px rgba(79, 70, 229, .12);
}

.comment-list { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 14px; }
.comment { background: #f8fafc; border: 1px solid var(--border); border-radius: 10px; padding: 13px 15px; }
.c-head { display: flex; align-items: center; gap: 9px; margin-bottom: 8px; flex-wrap: wrap; }
.c-avatar {
  width: 26px; height: 26px;
  border-radius: 50%;
  background: var(--primary);
  color: #fff;
  font-size: 10.5px;
  font-weight: 700;
  display: grid; place-items: center;
}
.c-text { margin: 0; font-size: 14.5px; line-height: 1.6; white-space: pre-wrap; }
.mini {
  border: none; background: none; cursor: pointer;
  font-size: 13px; padding: 3px 5px; border-radius: 5px;
}
.mini:hover { background: #e2e8f0; }
</style>
