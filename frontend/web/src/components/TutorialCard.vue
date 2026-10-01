<script setup>
import { computed, ref } from 'vue'
import api, { errorMessage } from '../api/client'
import { useAuthStore } from '../stores/auth'
import { youtubeThumb } from '../utils/youtube'
import SaveTutorialModal from './SaveTutorialModal.vue'

const props = defineProps({
  tutorial: { type: Object, required: true },
})
const emit = defineEmits(['changed'])

const auth = useAuthStore()
const showSave = ref(false)
const busy = ref(false)
const notice = ref('')

// Colores por tecnología para el fallback cuando no hay imagen
const palette = ['#4f46e5', '#0891b2', '#16a34a', '#d97706', '#dc2626', '#7c3aed', '#db2777']

function colorFor(name = '') {
  let sum = 0
  for (let i = 0; i < name.length; i++) sum += name.charCodeAt(i)
  return palette[sum % palette.length]
}

// Orden de preferencia de la imagen de la tarjeta:
//   1. la imagen subida por el autor (imageUrl), si la hay;
//   2. la miniatura derivada de la URL de YouTube;
//   3. ninguna → inicial de la tecnología.
// Si una opción falla al cargarse se avanza a la siguiente; agotadas las
// opciones, `thumb` pasa a null y se muestra el fallback.
const imageCandidates = computed(() =>
  [props.tutorial.imageUrl, youtubeThumb(props.tutorial.url)].filter(Boolean),
)
const candidateIndex = ref(0)
const thumb = computed(() => imageCandidates.value[candidateIndex.value] ?? null)

function onImgError() {
  candidateIndex.value += 1
}

async function toggleLike() {
  if (!auth.isAuthenticated) return
  busy.value = true
  notice.value = ''
  try {
    const { data } = await api.post(`/tutorials/${props.tutorial.id}/likes`)
    // Actualización optimista del contador
    props.tutorial.likedByMe = data.liked
    props.tutorial.likeCount = data.likeCount
  } catch (e) {
    notice.value = errorMessage(e)
  } finally {
    busy.value = false
  }
}

function onSaved() {
  emit('changed')
}
</script>

<template>
  <article class="t-card">
    <!-- Imagen o fallback con la inicial de la tecnología -->
    <div class="t-media" :style="{ background: colorFor(tutorial.technology) }">
      <img
        v-if="thumb"
        :src="thumb"
        :alt="tutorial.title"
        loading="lazy"
        @error="onImgError"
      />
      <span v-else class="t-fallback">{{ (tutorial.technology || '?').slice(0, 2) }}</span>

      <span class="t-tech">{{ tutorial.technology }}</span>
    </div>

    <div class="t-body">
      <router-link class="t-title" :to="`/tutorials/${tutorial.id}`">
        {{ tutorial.title }}
      </router-link>

      <p class="t-desc">{{ tutorial.description }}</p>

      <div class="t-meta">
        <span>✍️ {{ tutorial.author }}</span>
        <span>📅 {{ new Date(tutorial.publishedAt).toLocaleDateString() }}</span>
      </div>

      <div class="t-actions">
        <!-- ME GUSTA -->
        <button
          class="act"
          :class="{ on: tutorial.likedByMe, disabled: !auth.isAuthenticated }"
          :disabled="busy || !auth.isAuthenticated"
          :title="auth.isAuthenticated ? 'Dar/Quitar Me gusta' : 'Inicia sesión para dar Me gusta'"
          @click="toggleLike"
        >
          👍 {{ tutorial.likeCount }}
        </button>

        <!-- COMENTARIOS -->
        <router-link class="act" :to="`/tutorials/${tutorial.id}`">
          💬 {{ tutorial.commentCount }}
        </router-link>

        <span class="spacer" />

        <!-- GUARDAR (buscador + ComboBox) -->
        <button
          v-if="auth.isAuthenticated"
          class="act"
          :class="{ on: tutorial.savedByMe }"
          :title="tutorial.savedByMe ? 'Guardado en una de tus listas' : 'Guardar en mi lista'"
          @click="showSave = true"
        >
          {{ tutorial.savedByMe ? '✓ Guardado' : '🔖 Guardar' }}
        </button>
      </div>

      <div v-if="notice" class="alert alert-error" style="margin-top: 10px">{{ notice }}</div>
    </div>
  </article>

  <SaveTutorialModal
    v-if="showSave"
    :tutorial="tutorial"
    @close="showSave = false"
    @saved="onSaved"
  />
</template>

<style scoped>
.t-card {
  background: #fff;
  border: 1px solid var(--border);
  border-radius: var(--radius);
  overflow: hidden;
  display: flex;
  flex-direction: column;
  transition: box-shadow .15s, transform .15s;
}
.t-card:hover { box-shadow: var(--shadow); transform: translateY(-2px); }

.t-media {
  position: relative;
  height: 132px;
  display: grid;
  place-items: center;
  overflow: hidden;
}
.t-media img { width: 100%; height: 100%; object-fit: cover; }
.t-fallback {
  color: #fff;
  font-size: 34px;
  font-weight: 800;
  text-transform: uppercase;
  opacity: .9;
}
.t-tech {
  position: absolute;
  top: 9px; left: 9px;
  background: rgba(15, 23, 42, .78);
  color: #fff;
  font-size: 11px;
  font-weight: 700;
  padding: 3px 9px;
  border-radius: 999px;
}

.t-body { padding: 15px; display: flex; flex-direction: column; flex: 1; }
.t-title {
  font-size: 15.5px;
  font-weight: 700;
  color: var(--text);
  text-decoration: none;
  line-height: 1.35;
}
.t-title:hover { color: var(--primary); }

.t-desc {
  font-size: 13.5px;
  color: var(--text-soft);
  margin: 8px 0 12px;
  line-height: 1.5;
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.t-meta {
  display: flex;
  gap: 14px;
  font-size: 12.5px;
  color: var(--text-soft);
  margin-top: auto;
}

.t-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-top: 13px;
  padding-top: 12px;
  border-top: 1px solid var(--border);
}

.act {
  font-size: 13px;
  font-weight: 600;
  padding: 6px 10px;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: #fff;
  color: #475569;
  cursor: pointer;
  text-decoration: none;
  transition: .15s;
  display: inline-flex;
  align-items: center;
  gap: 5px;
}
.act:hover:not(.disabled) { background: #f1f5f9; }
.act.on { background: #eef2ff; border-color: #c7d2fe; color: var(--primary); }
.act.disabled { opacity: .55; cursor: not-allowed; }
</style>
