# TutorialHub — Plataforma Colaborativa de Tutoriales

Aplicación web para **compartir, descubrir y organizar tutoriales de programación**.
Cualquier visitante puede consultar los tutoriales; los usuarios registrados pueden
publicar, comentar, dar *Me gusta* y guardar tutoriales en sus **listas personales
privadas**. Solo un **Administrador** puede eliminar tutoriales.

---

## 1. Stack tecnológico

| Capa | Tecnología |
|---|---|
| Frontend | **Vue 3** (Composition API) + Vite, Vue Router, Pinia, Axios |
| Backend | **ASP.NET Core Web API (C# / .NET 10)** |
| Base de datos | **SQL Server** (Entity Framework Core 10) |
| Autenticación | **JWT** + BCrypt.Net para el hash de contraseñas |
| Documentación | OpenAPI (`/openapi/v1.json`) |

---

## 2. Requisitos previos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) (probado con Node 24 / npm 11)
- SQL Server (probado con **SQL Server Express**, instancia `localhost\SQLEXPRESS`)
- `dotnet-ef` para crear migraciones (solo si modificas el modelo):

  ```bash
  dotnet tool install --global dotnet-ef
  ```

---

## 3. Estructura del proyecto

```
TutorialPlatform/
├── backend/
│   └── TutorialPlatform.Api/
│       ├── Controllers/      # Endpoints y reglas de autorización
│       ├── Dtos/             # Contratos de entrada/salida + validaciones
│       ├── Entities/         # Modelos del dominio
│       ├── Data/
│       │   ├── AppDbContext.cs      # Relaciones e índices únicos
│       │   ├── DbSeeder.cs          # Datos de prueba iniciales
│       │   └── Migrations/          # Migraciones de EF Core
│       ├── Json/             # JsonTrimmingConverter (recorte antes de validar)
│       ├── Middleware/       # Manejador global de excepciones
│       ├── Services/         # TokenService, CurrentUser, TutorialMapper
│       └── wwwroot/uploads/  # Imágenes subidas (se crea solo)
├── frontend/
│   └── web/
│       └── src/
│           ├── api/          # Cliente Axios + interceptor JWT
│           ├── components/   # TutorialCard, SaveTutorialModal
│           ├── router/       # Rutas con guards
│           ├── stores/       # Pinia: sesión
│           └── views/        # Home, Login, Register, detalle, listas, perfil…
└── README.md
```

---

## 4. Puesta en marcha

### 4.1. Base de datos (automática)

**No hay que crearla a mano.** Al arrancar el backend, EF Core:

1. Crea la base de datos `TutorialPlatformDb` si no existe.
2. Aplica las migraciones pendientes.
3. Inserta los datos de demostración **solo si la tabla `Users` está vacía**.

La cadena de conexión está en
`backend/TutorialPlatform.Api/appsettings.json`:

```
Server=localhost\SQLEXPRESS;Database=TutorialPlatformDb;Trusted_Connection=True;TrustServerCertificate=True;
```

### 4.2. Backend (puerto 5099)

```bash
cd backend/TutorialPlatform.Api
dotnet run --launch-profile http
```

- API: <http://localhost:5099>
- Especificación OpenAPI: <http://localhost:5099/openapi/v1.json>

> El perfil `http` fija `http://localhost:5099`, que es exactamente el puerto al
> que apunta el proxy del frontend. Si cambias uno, cambia los dos
> (`Properties/launchSettings.json` y `frontend/web/vite.config.js`).

### 4.3. Frontend (puerto 5173)

```bash
cd frontend/web
npm install
npm run dev
```

Abre <http://localhost:5173>.

El servidor de desarrollo de Vite **proxya** `/api` y `/uploads` hacia el
backend, así que no hay que configurar nada más.

### 4.4. Compilar para producción

```bash
cd frontend/web && npm run build     # genera frontend/web/dist
cd backend/TutorialPlatform.Api && dotnet publish -c Release
```

---

## 5. Credenciales de demostración

| Usuario | Contraseña | Rol |
|---|---|---|
| `admin` | `admin123` | **ADMIN** |
| `maria` | `maria123` | USER |
| `carlos` | `carlos123` | USER |

Datos sembrados: **8 tecnologías** (JavaScript, Python, Java, Vue, C#, SQL, React,
HTML/CSS) y **6 tutoriales** de ejemplo.

---

## 6. Reglas de negocio (invariantes)

Estas reglas están validadas **en el servidor**, nunca solo en el cliente:

1. **Solo ADMIN elimina tutoriales** — un USER recibe `403`.
2. **Solo el autor edita su tutorial**, con ADMIN como excepción.
3. **Nadie elimina contenido que no le pertenece** salvo ADMIN
   (comentarios: autor o ADMIN; listas: solo su dueño; imágenes y tecnologías: ADMIN).
4. **Un usuario = un *Me gusta* por tutorial** — índice único + toggle sincronizado.
5. **Las listas personales solo las ve y administra su dueño** (`403` para ajenas).
6. **Guardar dos veces el mismo tutorial no duplica** la entrada.
7. **No existen** mensajería privada, seguidores, perfiles públicos, muro ni publicaciones.
8. **Toda la autorización ocurre en el backend** (JWT con rol).
9. **`likeCount` siempre refleja la tabla `Likes`** (se actualiza en la misma operación).
10. **Sin *soft-delete***: para usuarios el borrado simplemente no existe; para ADMIN es borrado real.

### Validaciones cliente ↔ servidor (alineadas)

| Campo | Regla |
|---|---|
| Título del tutorial | ≥ 3 y ≤ 200 caracteres |
| Descripción | ≥ 10 y ≤ 2000 caracteres |
| URL | obligatoria y con formato `http(s)://` |
| Tecnología | obligatoria (debe existir) |
| Usuario | ≥ 3 caracteres, solo `[a-zA-Z0-9_.-]` |
| Contraseña | ≥ 6 caracteres |
| Nombre de lista | ≥ 2 y ≤ 120 caracteres |
| Comentario | 1 a 1000 caracteres |

Todos los textos se **recortan (trim)** durante la deserialización del JSON,
*antes* de que se ejecuten las validaciones, de modo que `"   "` no puede
colarse como un título o un nombre de lista.

---

## 7. Endpoints principales

| Método | Ruta | Acceso |
|---|---|---|
| POST | `/api/auth/register` · `/api/auth/login` | público |
| GET/PUT | `/api/auth/me` | autenticado |
| GET | `/api/tutorials` | público (paginación, `search`, `technology`, `sort`) |
| POST | `/api/tutorials` | autenticado |
| PUT | `/api/tutorials/{id}` | autor o ADMIN |
| DELETE | `/api/tutorials/{id}` | **solo ADMIN** |
| GET/POST | `/api/tutorials/{id}/comments` | público / autenticado |
| PUT/DELETE | `/api/tutorials/{id}/comments/{id}` | autor o ADMIN |
| GET/POST | `/api/tutorials/{id}/likes` | ADMIN / autenticado (toggle) |
| GET/POST | `/api/lists` | autenticado (solo las propias) |
| GET/PUT/DELETE | `/api/lists/{id}` | solo el dueño |
| POST | `/api/lists/{id}/save` | solo el dueño |
| DELETE | `/api/lists/{id}/save/{tutorialId}` | solo el dueño |
| GET | `/api/lists/search` | autenticado |
| GET/POST | `/api/technologies` | público / **ADMIN** |
| DELETE | `/api/technologies/{id}` | **solo ADMIN** (409 si tiene tutoriales) |
| POST/GET | `/api/images` | **solo ADMIN** |

---

## 8. Imágenes

- Formatos admitidos: `.jpg`, `.jpeg`, `.png`, `.webp`, `.gif`.
- Se comprueba el **tipo MIME** y las *magic bytes* reales del archivo (no solo la extensión).
- Tamaño máximo: 5 MB. Los nombres se reemplazan por un GUID.
- Archivos en `wwwroot/uploads/`, servidos en `/uploads/...`.
- La carpeta se crea automáticamente al arrancar **antes** de montar los
  archivos estáticos.

---

## 9. Notas para producción

Antes de publicar, revisa `appsettings.json`:

- **`Jwt:SecretKey`** — cámbialo por un valor propio y guárdalo en
  variables de entorno, nunca en el repositorio.
- **`ConnectionStrings:DefaultConnection`** — usa una cadena con credenciales
  apropiadas en lugar de autenticación de Windows.
- **`Cors:Frontend`** — apunta al dominio real del frontend.
- Los errores inesperados devuelven un `500` con un `errorId`; el detalle va
  solo al log del servidor (en Development sí se expone para depurar).

---

## 10. Pruebas de verificación

La validación se realizó con dos baterías de pruebas sobre la API en ejecución:

| Batería | Cobertura | Resultado |
|---|---|---|
| Invariantes de §5 | Los 10 puntos de la sección 6, con roles ADMIN/USER y anónimo | **46/46** |
| Validaciones | Cliente ↔ servidor, recorte de espacios, regresión de roles | **22/22** |

Verificaciones directas en SQL: **0** pares `(UserId, TutorialId)` duplicados en
`Likes`, **0** discrepancias entre `Tutorials.LikeCount` y `COUNT(*)` de `Likes`,
y **0** registros huérfanos de integridad referencial.
