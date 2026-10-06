<script setup>
import { computed, onMounted, ref } from 'vue'
import api, { errorMessage } from '../api/client'

/**
 * Panel de administración — SOLO ADMIN.
 *
 * Dos endpoints, los dos `[Authorize(Roles = Admin)]` en el backend:
 *   - GET /api/stats → visitas (totales, distintas y por página).
 *   - GET /api/users  → cuentas registradas con su correo.
 *
 * Las guardas del router y de la barra son solo UX: si un USER llega aquí a
 * la fuerza, el backend devuelve 403 y sale el mensaje de error (§5.8).
 * La sesión de ADMIN no se registra como visita, así que las cifras miden
 * a los demás.
 */
const stats = ref(null)
const usuarios = ref([])
const loading = ref(true)
const error = ref('')

async function load() {
  loading.value = true
  error.value = ''
  try {
    // Las dos peticiones van juntas: si una falla, no se queda media pantalla
    // cargando y la otra sin pintar.
    const [s, u] = await Promise.all([api.get('/stats'), api.get('/users')])
    stats.value = s.data
    usuarios.value = u.data
  } catch (e) {
    error.value = errorMessage(e, 'No se pudieron cargar las estadísticas.')
  } finally {
    loading.value = false
  }
}

onMounted(load)

/** Día con más visitas, para escalar las barras. Mínimo 1 para no dividir entre 0. */
const maxDia = computed(() => {
  const dias = stats.value?.porDia ?? []
  return Math.max(1, ...dias.map((d) => d.visitas))
})

const pct = (visitas) => Math.round((visitas / maxDia.value) * 100) + '%'

/**
 * "2026-10-05T00:00:00" → "05/10". Con `conAnio` → "05/10/2026": en las
 * cuentas el año importa (una alta de 2026 y otra de 2024 no son lo mismo),
 * en la serie de 30 días sobra.
 */
function fecha(cuando, conAnio = false) {
  const d = new Date(cuando)
  if (Number.isNaN(d.getTime())) return String(cuando).slice(0, 10)
  const dd = String(d.getDate()).padStart(2, '0')
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  return conAnio ? `${dd}/${mm}/${d.getFullYear()}` : `${dd}/${mm}`
}

const esAdmin = (rol) => rol === 'ADMIN'
</script>

<template>
  <div>
    <div class="row" style="margin-bottom: 20px">
      <div>
        <h1 class="section-title">Estadísticas</h1>
        <p class="section-sub" style="margin: 0">
          Cuánta gente entra en la plataforma, por dónde navega y qué cuentas hay.
        </p>
      </div>
      <span class="spacer" />
      <button class="btn btn-outline" :disabled="loading" @click="load">Actualizar</button>
    </div>

    <div v-if="!loading && error" class="empty">
      <span class="empty-icon">⚠️</span>
      <p>{{ error }}</p>
      <button class="btn btn-outline" @click="load">Reintentar</button>
    </div>

    <div v-else-if="loading" class="empty">
      <span class="spinner dark" style="width: 30px; height: 30px" />
      <p>Cargando estadísticas…</p>
    </div>

    <template v-else-if="stats">
      <!-- Cifras principales: siempre, aunque no haya visitas todavía -->
      <div class="tiles">
        <div class="card tile">
          <span class="tile-num">{{ stats.visitasTotales }}</span>
          <span class="tile-label">Visitas totales</span>
        </div>
        <div class="card tile">
          <span class="tile-num">{{ stats.visitantesUnicos }}</span>
          <span class="tile-label">Visitantes distintos</span>
        </div>
        <div class="card tile">
          <span class="tile-num">{{ stats.visitasHoy }}</span>
          <span class="tile-label">Visitas de hoy</span>
        </div>
        <div class="card tile">
          <span class="tile-num">{{ stats.paginasDistintas }}</span>
          <span class="tile-label">Páginas distintas</span>
        </div>
      </div>

      <!-- Visitas por página: solo si las hay -->
      <template v-if="stats.visitasTotales > 0">
        <div class="card seccion">
          <h2 class="seccion-titulo">Por página</h2>
          <table class="tabla">
            <thead>
              <tr>
                <th>Ruta</th>
                <th class="num">Visitas</th>
                <th class="num">Distintos</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="p in stats.porPagina" :key="p.ruta">
                <td class="ruta">{{ p.ruta }}</td>
                <td class="num">{{ p.visitas }}</td>
                <td class="num">{{ p.unicos }}</td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="card seccion">
          <h2 class="seccion-titulo">Últimos 30 días</h2>
          <div v-for="d in stats.porDia" :key="d.fecha" class="dia">
            <span class="dia-fecha">{{ fecha(d.fecha) }}</span>
            <span class="dia-pista">
              <span class="dia-relleno" :style="{ width: pct(d.visitas) }" />
            </span>
            <span class="dia-num">{{ d.visitas }}</span>
            <span class="dia-unicos">{{ d.unicos }} distintos</span>
          </div>
          <p v-if="stats.porDia.length === 0" class="section-sub" style="margin: 0">
            Sin visitas en los últimos 30 días.
          </p>
        </div>
      </template>

      <div v-else class="empty" style="margin-bottom: 4px">
        <span class="empty-icon">📊</span>
        <p>Todavía no hay ninguna visita registrada. En cuanto alguien abra la página, aparecerá aquí.</p>
      </div>

      <!-- Cuentas: SIEMPRE se enseña, aunque el contador esté en cero -->
      <div class="card seccion">
        <h2 class="seccion-titulo">
          Cuentas registradas
          <span class="cuenta-n">{{ usuarios.length }}</span>
        </h2>

        <table class="tabla">
          <thead>
            <tr>
              <th>Usuario</th>
              <th>Correo</th>
              <th class="num">Alta</th>
              <th>Rol</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="u in usuarios" :key="u.username">
              <td class="ruta">{{ u.username }}</td>
              <td class="correo">{{ u.email }}</td>
              <td class="num">{{ fecha(u.createdAt, true) }}</td>
              <td>
                <span class="rol" :class="{ rolAdmin: esAdmin(u.role) }">{{ u.role }}</span>
              </td>
            </tr>
          </tbody>
        </table>

        <p v-if="usuarios.length === 0" class="section-sub" style="margin: 0">
          No hay ninguna cuenta.
        </p>
      </div>

      <p class="nota">
        Todo lo de esta pantalla es interno: el backend devuelve 403 a cualquier
        cuenta que no sea ADMIN. Los rastreadores (Google, previsualizadores de
        enlaces…) no cuentan como visita, tu propia sesión de ADMIN tampoco, y
        las IPs no se guardan — solo su hash.
      </p>
    </template>
  </div>
</template>

<style scoped>
.tiles {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 16px;
}

.tile {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding: 18px 20px;
}

.tile-num {
  font-size: 34px;
  font-weight: 800;
  line-height: 1;
  color: var(--text);
}

.tile-label {
  font-size: 13px;
  color: var(--text-soft);
}

.seccion {
  margin-top: 20px;
  padding: 18px 20px;
}

.seccion-titulo {
  margin: 0 0 14px;
  font-size: 16px;
  font-weight: 700;
  color: var(--text);
}

/* Contador de cuentas a la derecha del título */
.cuenta-n {
  display: inline-block;
  margin-left: 8px;
  padding: 1px 9px;
  border-radius: 999px;
  background: var(--gold);
  color: #0f172a;
  font-size: 12px;
  font-weight: 800;
  vertical-align: 2px;
}

.tabla {
  width: 100%;
  border-collapse: collapse;
  font-size: 14px;
}

.tabla th,
.tabla td {
  padding: 8px 6px;
  text-align: left;
  border-bottom: 1px solid #e5e7eb;
}

.tabla th {
  color: var(--text-soft);
  font-weight: 600;
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 0.04em;
}

.tabla tr:last-child td {
  border-bottom: none;
}

.num {
  text-align: right;
  font-variant-numeric: tabular-nums;
}

.ruta {
  font-family: Consolas, 'Courier New', monospace;
  color: var(--text);
}

/* Los correos son largos: sin esto empujan la tabla fuera de la tarjeta */
.correo {
  word-break: break-all;
  color: var(--text);
}

/* Etiqueta de rol: ADMIN en oro para que salte a la vista */
.rol {
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
  background: #e5e7eb;
  color: #374151;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.04em;
}

.rolAdmin {
  background: var(--gold);
  color: #0f172a;
}

.dia {
  display: grid;
  grid-template-columns: 52px 1fr 44px 90px;
  align-items: center;
  gap: 10px;
  padding: 5px 0;
  font-size: 13px;
}

.dia-fecha {
  color: var(--text-soft);
  font-variant-numeric: tabular-nums;
}

.dia-pista {
  display: block;
  height: 12px;
  background: #e5e7eb;
  border-radius: 6px;
  overflow: hidden;
}

.dia-relleno {
  display: block;
  height: 100%;
  background: var(--gold);
  border-radius: 6px;
  min-width: 2px;
}

.dia-num {
  text-align: right;
  font-weight: 700;
  color: var(--text);
  font-variant-numeric: tabular-nums;
}

.dia-unicos {
  text-align: right;
  color: var(--text-soft);
  font-size: 12px;
}

.nota {
  margin-top: 18px;
  font-size: 12.5px;
  line-height: 1.6;
  color: var(--text-soft);
}
</style>
