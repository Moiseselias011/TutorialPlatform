# AGENTS.md — TutorialPlatform

Aplicación web de tutoriales. El proyecto contiene un backend ASP.NET Core Web API y un frontend Vite, con SQL Server como base de datos. El objetivo es desarrollar y verificar la plataforma respetando la especificación funcional y técnica definida en `PROMPT_plataforma_tutoriales.md`.

## Stack y estructura

* Backend: ASP.NET Core Web API / .NET.
* Frontend: Vue 3 + Vite (Vue Router, Pinia, Axios).
* Base de datos: SQL Server Express.
* ORM: Entity Framework Core.
* Autenticación: JWT — Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`).
* Hash de contraseñas: BCrypt (`BCrypt.Net-Next`).
* Autorización: ASP.NET Core Authorization (`[Authorize]` + `[Authorize(Roles = Roles.Admin)]`).
* No existe archivo `.sln`; el único proyecto es:
  `backend/TutorialPlatform.Api/TutorialPlatform.Api.csproj`.

Estructura relevante:

* `PROMPT_plataforma_tutoriales.md` — especificación maestra del proyecto.
* `TutorialPlatform/README.md` — instalación, arranque, credenciales y endpoints.
* `TutorialPlatform/backend/TutorialPlatform.Api/` — API.
* `TutorialPlatform/backend/TutorialPlatform.Api/Controllers/` — endpoints, autorización y reglas de negocio.
* `TutorialPlatform/backend/TutorialPlatform.Api/Services/` — servicios existentes; `ITutorialMapper` centraliza el mapeo entidad → DTO.
* `TutorialPlatform/backend/TutorialPlatform.Api/Json/JsonTrimmingConverter.cs` — recorte global de strings durante la deserialización JSON.
* `TutorialPlatform/backend/TutorialPlatform.Api/Program.cs` — configuración de la aplicación, migración y seed de la base de datos.
* `TutorialPlatform/backend/TutorialPlatform.Api/Properties/launchSettings.json` — puerto 5099 del backend.
* `TutorialPlatform/frontend/web/` — frontend.
* `TutorialPlatform/frontend/web/vite.config.js` — configuración de Vite y proxy hacia el backend.

`C:\Users\vanne\` es el directorio de inicio de Windows, no un repositorio git. El único proyecto es `TutorialPlatform/`. El resto de la raíz (`AppData`, `Documents`, `Downloads`, etc.) pertenece al sistema: no modificarlo ni limpiarlo.

Fuentes de verdad, en orden de prioridad:

1. `PROMPT_plataforma_tutoriales.md` — especificación maestra. Define, entre otros, los invariantes de §5, stack de §6, fases de §7, criterios de aceptación de §8 y forma de trabajo con el usuario de §9.
2. `TutorialPlatform/README.md` — instalación, arranque, credenciales y endpoints.
3. Código del proyecto.
4. `TutorialPlatform/MEMORY.md` — memoria entre sesiones. **OpenCode no la carga por sí solo**, ver «Memoria entre sesiones».

Si el código o la documentación contradicen el `PROMPT_plataforma_tutoriales.md`, se debe señalar el conflicto antes de implementar.

No existen `opencode.json`, `CLAUDE.md`, `.cursorrules` ni reglas de Cursor.

Todo el proyecto se escribe y se responde en español: especificación, README, comentarios del código, mensajes de error de la API e interfaz.

## Comandos

### Backend

Puerto: `5099`.

```powershell
cd TutorialPlatform/backend/TutorialPlatform.Api
dotnet run --launch-profile http
```

Compilación:

```powershell
dotnet build
```

### Frontend

Puerto: `5173`.

Primera instalación:

```powershell
cd TutorialPlatform/frontend/web
npm install
```

Arranque:

```powershell
npm run dev
```

Compilación:

```powershell
npm run build
```

No existe framework de tests automatizados. No hay xUnit, NUnit, Vitest ni Jest.

Los servidores suelen estar ya ejecutándose en segundo plano. Antes de arrancar otra instancia del backend, comprobar el puerto:

```powershell
Get-NetTCPConnection -State Listen -LocalPort 5099
```

Si el puerto está ocupado, probablemente ya existe una instancia funcionando. No matar procesos `dotnet` o `node` a ciegas.

## Convenciones

* Escribir y responder siempre en español.
* Los comentarios del código deben estar en español.
* Los mensajes de error de la API deben estar en español.
* La interfaz debe estar en español.
* Mantener las convenciones de nombres y patrones ya utilizados por el proyecto.
* Las reglas de autorización se implementan mediante `[Authorize]` y `[Authorize(Roles = Roles.Admin)]` en las acciones correspondientes.
* Las comprobaciones de propiedad se realizan dentro de las acciones mediante `OwnerId` o `AuthorId`.
* Utilizar `ITutorialMapper` para centralizar el mapeo de entidades a DTOs y evitar duplicación.
* Las validaciones de campos de texto deben ser iguales entre cliente y servidor.
* Si se agrega un campo `string` a un DTO recibido como JSON, el recorte global de strings ya se realiza mediante `Json/JsonTrimmingConverter.cs`.
* Para campos recibidos mediante otros mecanismos, como query string o multipart, realizar el recorte manualmente cuando corresponda.

Archivo de referencia para la especificación y decisiones de arquitectura:

`PROMPT_plataforma_tutoriales.md`

Archivo de referencia para instalación y ejecución:

`TutorialPlatform/README.md`

## Reglas de dominio / trampas conocidas

### Especificación y fases

* Trabajar una fase a la vez: implementar, verificar y reportar.
* No romper nunca los invariantes definidos en §5 del `PROMPT_plataforma_tutoriales.md`.
* Toda autorización debe validarse en el backend aunque el frontend también la restrinja.
* Si un requisito nuevo entra en conflicto con el PROMPT, señalar el conflicto antes de implementarlo.
* Ante requisitos ambiguos o decisiones relacionadas con el stack definido en §6, consultar antes de decidir.

### Puertos acoplados

El backend y el frontend dependen del mismo puerto del backend:

* `Properties/launchSettings.json` → `http://localhost:5099`
* `frontend/web/vite.config.js` → proxy hacia `http://localhost:5099`

Si se modifica uno, se debe modificar el otro.

Un cambio incorrecto puede hacer que el frontend deje de comunicarse con la API aunque ambos proyectos sigan compilando correctamente.

En desarrollo, el navegador utiliza `/api` en el origen del frontend (`5173`) y Vite reenvía las peticiones al backend. Por este motivo no se utiliza CORS para esta comunicación.

### Base de datos

SQL Server:

* Servidor: `localhost\SQLEXPRESS`
* Base de datos: `TutorialPlatformDb`
* Autenticación: Windows.

La base de datos se crea, migra y siembra automáticamente al arrancar la aplicación mediante `Program.cs`:

* `MigrateAsync()`
* `SeedAsync()`

No ejecutar manualmente:

```powershell
dotnet ef database update
```

`DbSeeder` solamente realiza el seed cuando la tabla `Users` está vacía.

Para regenerar los datos de demostración, eliminar la base de datos y volver a arrancar la aplicación.

Datos de seed:

* `admin` / `admin123` — ADMIN
* `maria` / `maria123`
* `carlos` / `carlos123`
* 8 tecnologías.
* 6 tutoriales.

Los IDs no son fijos y pueden cambiar después de volver a sembrar la base de datos. Nunca asumir IDs en pruebas. Consultar `/api/technologies` para obtener los IDs actuales.

Si el arranque falla con:

```text
sp_releaseapplock ... tiempo de espera
```

puede haber quedado un bloqueo de EF Core porque un proceso fue terminado durante una migración. Comprobar que no haya sesiones bloqueadas y volver a arrancar.

Existen dos instancias de SQL Server:

* `SQLEXPRESS` — instancia utilizada por el proyecto.
* `SQLEXPRESS01` — vacía e inactiva.

El equipo dispone de poca RAM y bajo carga se han observado consultas de más de 100 segundos. Si una petición parece quedar colgada, comprobar primero el estado de la base de datos antes de asumir que existe un problema en el código.

### Reglas de autorización

No existe una capa de servicios general para las reglas de negocio. La autorización y las reglas se encuentran principalmente en los controladores.

Reglas importantes:

* `DELETE /api/tutorials/{id}` → solamente ADMIN.
* Un USER que intente eliminar un tutorial debe recibir `403`.
* `PUT /api/tutorials/{id}` → permitido al autor o a ADMIN.
* Las listas personales deben devolver `Forbid()` a cualquier usuario que no sea el propietario, incluido en operaciones GET.
* La API no expone:

  * borrado de usuarios,
  * perfiles públicos,
  * seguidores,
  * mensajería.

### Validaciones

Los campos nuevos de texto deben mantener la misma regla de validación en cliente y servidor.

Ejemplos de reglas existentes:

* título ≥ 3 caracteres;
* descripción ≥ 10 caracteres;
* usuario ≥ 3 caracteres;
* contraseña ≥ 6 caracteres;
* lista ≥ 2 caracteres.

`Json/JsonTrimmingConverter.cs` está registrado globalmente y recorta los strings del body JSON durante la deserialización, antes de que ASP.NET realice la validación.

Por ejemplo, un valor compuesto solamente por espacios no debe poder pasar como un título vacío.

Para valores recibidos mediante query string, multipart u otros mecanismos, realizar el recorte manualmente cuando sea necesario. `TutorialsController` contiene un ejemplo con `q.Search`.

### PowerShell 5.1

* `$error` es una variable automática de solo lectura. No utilizarla como nombre de variable.
* PowerShell utiliza comilla invertida para escapar caracteres; `\"` es sintaxis de C#, no de PowerShell.
* `'texto'$var'más'` no concatena strings. Utilizar `+` o el operador `-f`.
* Los comentarios utilizan `#`, no `//`.
* `Invoke-RestMethod` no dispone de `-Form`. Para multipart utilizar `curl.exe`.
* Los acentos pueden mostrarse incorrectamente en la consola. Un texto como `Gu?a` o `Pagina` puede ser un problema de visualización de la consola y no de los archivos.
* No corregir archivos basándose únicamente en cómo aparecen los acentos en la terminal.
* Para verificar UTF-8 se puede utilizar:

```powershell
[System.Text.Encoding]::UTF8.GetString([IO.File]::ReadAllBytes($ruta))
```

También se pueden comprobar los code points (`í` = 237) o utilizar `UNICODE(...)` en SQL Server.

* Los archivos del proyecto utilizan UTF-8 válido.
* `sqlcmd` puede devolver también `(N filas afectadas)`. Si se necesita parsear un número, utilizar `-h -1` y filtrar el resultado, por ejemplo:

```powershell
Where-Object { $_ -match '^\s*\d+\s*$' }
```

## Memoria entre sesiones

`TutorialPlatform/MEMORY.md` es la memoria del proyecto: estado, decisiones, aprendizajes y próximos pasos. Los agentes se fundamentan en ella a lo largo del tiempo.

**OpenCode no carga `MEMORY.md` automáticamente** — solo reconoce `AGENTS.md` —, así que este archivo es el que obliga a usarla. Sin estas reglas, `MEMORY.md` sería un archivo muerto que nadie abre.

Reglas obligatorias, sin excepción:

1. **Leer `TutorialPlatform/MEMORY.md` al empezar cada tarea**, antes de proponer cambios, estimar qué está hecho o decidir por dónde seguir.
2. **Actualizar `TutorialPlatform/MEMORY.md` SIEMPRE después de cada tarea.** Al terminar, reflejar el resultado real: qué quedó hecho, qué no, qué se decidió y por qué, qué se aprendió y qué pasa a pendientes. Si la tarea no cambió nada, confirmarlo brevemente — no inventar novedades ni dejar la sección desactualizada.
3. Registrar también los **errores y los límites encontrados**, no solo los éxitos: para eso está la sección de aprendizajes.
4. Los «próximos pasos» expresan **intención, no estado verificado**: contrastarlos con el código antes de asumir que algo existe o que aún no está hecho.
5. Mantenerla breve. Lo que explica **cómo** se trabaja va en `AGENTS.md`; lo que define **qué** hay que hacer va en el `PROMPT`.
6. No volcar en ella detalles operativos que ya estén en `AGENTS.md` (puertos, rutas, trampas de PowerShell): se duplicarían y divergirían.

## Forma de trabajar

* Trabajar una fase a la vez: implementar → verificar → reportar.
* Antes de implementar algo definido de forma ambigua, preguntar.
* Antes de tomar decisiones relacionadas con el stack definido en §6 del PROMPT, preguntar.
* Antes de implementar un requisito que contradiga el PROMPT, señalar el conflicto.
* Mantener los cambios acotados al objetivo solicitado.
* No realizar modificaciones no relacionadas con la tarea actual.
* Después de realizar cambios, explicar qué se modificó y cómo se verificó.
* Reportar resultados verificables: endpoints probados, compilación, pruebas realizadas y estado relevante de la base de datos.
* No considerar terminado un cambio solamente porque el código compila: debe verificarse su comportamiento cuando corresponda.
* **Leer `TutorialPlatform/MEMORY.md` al empezar cada tarea** y **actualizarla SIEMPRE después de cada tarea** — también si la tarea falló o no cambió nada. Detalle completo en «Memoria entre sesiones».
* Este `AGENTS.md` se edita desde VS Code y es la fuente única; `C:\Users\vanne\AGENTS.md` solo contiene un puntero corto. OpenCode lo descubre al explorar `TutorialPlatform/`, pero **no vuelve a comprobarlo si ya lo ha cargado**: una edición no se detecta sola dentro de una sesión en curso.
* Si el usuario indica que ha modificado este archivo, **volver a leerlo antes de seguir**; si no lo indica, la edición surtirá efecto en la sesión siguiente. El puntero de la raíz sí se recarga en caliente antes de cada petición, pero solo guarda las reglas de seguridad básicas.
* Aquí solo hay guía, no bloqueo: una restricción que no se pueda saltar exigiría `permissions` (`effect: "deny"` o `"ask"`) en `opencode.jsonc`; por decisión del usuario no se crea ese fichero.

## Límites

### ✅ Siempre

* Consultar `PROMPT_plataforma_tutoriales.md` antes de tomar decisiones que puedan afectar requisitos, arquitectura, stack o invariantes.
* Respetar los invariantes de §5.
* Validar las autorizaciones en el backend.
* Mantener cliente y servidor alineados en las validaciones.
* Mantener el proyecto en español.
* Comprobar el puerto 5099 antes de arrancar otra instancia del backend.
* Verificar los IDs actuales mediante la API cuando una prueba dependa de IDs de la base de datos.
* Limpiar los datos creados por scripts de prueba.
* Verificar los cambios antes de darlos por terminados.
* Leer `TutorialPlatform/MEMORY.md` al empezar cada tarea.
* Actualizar `TutorialPlatform/MEMORY.md` después de **cada** tarea, sin excepción.

### ⚠️ Pregunta antes

* Cambiar tecnologías o versiones definidas en §6 del PROMPT.
* Resolver requisitos ambiguos mediante una decisión propia.
* Implementar un requisito que contradiga el PROMPT.
* Cambiar contratos de API o comportamientos establecidos sin verificar primero el impacto.
* Modificar la estructura o estrategia de persistencia cuando no sea necesario para la tarea.
* Crear dependencias nuevas o introducir herramientas que no formen parte del stack existente.

### 🚫 Nunca

* No modificar ni limpiar `C:\Users\vanne\AppData`, `Documents`, `Downloads` u otros directorios del sistema fuera del proyecto.
* No tratar `C:\Users\vanne\` como si fuera un repositorio git.
* No asumir que los IDs de la base de datos son permanentes.
* No ejecutar `dotnet ef database update` manualmente.
* No matar procesos `dotnet` o `node` a ciegas.
* No romper los invariantes definidos en §5 del PROMPT.
* No confiar únicamente en las restricciones del frontend para garantizar autorización.
* No inventar endpoints que la API no expone.
* No dejar datos de prueba en la base de datos.
* No corregir archivos por errores aparentes de acentos basándose únicamente en la salida de la consola.

## Verificación

No existe actualmente un framework de tests automatizados.

La verificación funcional se realiza mediante scripts HTTP ad-hoc en PowerShell contra el servidor levantado.

Los scripts utilizados durante el desarrollo se han ubicado temporalmente en:

```text
%LOCALAPPDATA%\Temp\opencode\test_fase*.ps1
```

Estos scripts son efímeros y no están versionados.

Cuando se cree un script de prueba:

1. Ejecutarlo contra el servidor correspondiente.
2. Comprobar las respuestas HTTP esperadas.
3. Verificar los efectos sobre la base de datos cuando corresponda.
4. Eliminar al finalizar todos los datos de prueba creados por el script.
5. No dejar tutoriales, listas, tecnologías ni usuarios de prueba en la base de datos de demostración.

Para cambios de código, como mínimo:

```powershell
dotnet build
```

y, para el frontend:

```powershell
npm run build
```

Cuando el cambio afecte al comportamiento de la API, realizar pruebas HTTP contra el servidor ya levantado.

La verificación debe informar resultados concretos: endpoints probados, códigos HTTP obtenidos, compilación, comportamiento observado y cualquier estado relevante de la base de datos.

Como referencia histórica, las baterías realizadas durante esta sesión sumaron 100 aserciones:

* 46 invariantes.
* 22 validaciones.
* 32 pruebas E2E.
* 0 fallos.
