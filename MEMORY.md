# MEMORY.md — TutorialPlatform

Memoria del proyecto entre sesiones. Resume el estado, decisiones, aprendizajes y próximos pasos importantes. Mantenerla breve y actualizarla cuando cambie el proyecto.

## Estado actual

* El proyecto está casi terminado, pero continúa en etapa de mejoras y ampliaciones.
* Actualmente se pueden crear y publicar tutoriales.
* Los usuarios pueden dar Me gusta a los tutoriales.
* Los usuarios pueden guardar tutoriales.
* Se pueden crear listas personales.
* Los comentarios están implementados: crear, editar y borrar (autor o ADMIN).
* Los roles están definidos y funcionan correctamente.
* Foto de perfil: implementada. Campo `User.PhotoUrl` (migración `AddUserPhotoUrl`), se sube con `POST /api/images` y se fija con `PUT /api/auth/me`. Se ve en la barra superior y en «Mi perfil» (cambiar o quitar).
* Miniatura de YouTube: implementada. `src/utils/youtube.js` deriva `i.ytimg.com/vi/{id}/hqdefault.jpg` a partir de la URL del tutorial; la usa la tarjeta y la cabecera del detalle. Las 6 URL de ejemplo —tanto en la base de datos como en `DbSeeder.cs`— apuntan ya a vídeos de YouTube reales, para que se vean nada más abrir la app y para que un re-seed no las revierta.
* Repositorio en GitHub: publicado y verificado. `https://github.com/Moiseselias011/-TutorialPlatform` (público, rama `main`), commit inicial `b6844e7`, 69 ficheros, sin `bin/`, `obj/`, `node_modules/`, `dist/` ni las fotos de `wwwroot/uploads/` (solo viaja `.gitkeep`).
* Funcionalidades pendientes conocidas:

  * Cambiar el orden por defecto de los tutoriales a «Más populares».
  * Contador de Me Gusta en los comentarios.
  * Ordenar los comentarios por cantidad de Me Gusta.

## Decisiones (y por qué)

* Se utiliza ASP.NET porque forma parte de la formación universitaria actual y permite practicar con tecnologías relacionadas con los estudios de C#.
* La arquitectura debe mantener MVC + API y un frontend reactivo.
* Las contraseñas deben permanecer cifradas.
* La autorización es fundamental para el funcionamiento del proyecto.
* Los roles son inamovibles. Cualquier modificación relacionada con ellos requiere confirmación.
* El resto de decisiones de arquitectura puede discutirse si existe una razón para cambiarlas.
* El repositorio de GitHub es **público**, por decisión del usuario. Para no exponer su correo personal en el historial, los commits llevan la dirección noreply de GitHub (`Moiseselias011@users.noreply.github.com`) mediante `git config` **local** del repositorio; la identidad global no se modifica.

## Aprendizajes y errores a evitar

* El proyecto permitió aprender que MVC no tiene por qué concentrar todo el desarrollo: el backend puede convivir con un frontend separado y reactivo.
* Al principio, la especificación principal no definía suficientemente los límites de lo que la IA no debía crear. Esto provocó que se generaran funcionalidades y estructuras que no estaban dentro de lo que se conocía del proyecto.
* Al trabajar con IA, las modificaciones grandes o estructurales requieren confirmación previa y una explicación detallada antes de realizarlas.
* No se han identificado errores recurrentes específicos que deban evitarse por ahora.
* La propia memoria puede contradecir al código: mantener un «pendiente» que ya está implementado hace perder el tiempo reimplementándolo. Antes de dar por bueno un pendiente, contrastarlo con el código. Caso real: los comentarios y el orden por Me Gusta ya existían (`CommentsController.cs`, `?sort=likes`) pero figuraban como pendientes.
* No todo lo que sobra en la base de datos es residuo de pruebas. `DbSeeder` no crea likes, listas ni guardados, así que esas filas pueden venir de los scripts **o de que el usuario haya probado la interfaz a mano**. Antes de borrar, contrastar con los scripts: si los nombres no aparecen en ellos (p. ej. listas «java»/«asp» o un comentario suelto), son datos del usuario y se dejan intactos.
* `Out-File -Encoding utf8` de PowerShell 5.1 escribe **siempre** un BOM (`EF BB BF`). Si se usa para generar un mensaje de commit, git lo incorpora literalmente y el asunto queda como `"\uFEFFVersión inicial…"`. Detectarlo por la consola de git o volcando de nuevo con `Out-File` da un resultado falso: hay que redirigir con `cmd /c "git cat-file commit HEAD > fichero"` y leer los bytes. Corregir con `[IO.File]::WriteAllText($ruta, $texto, [Text.UTF8Encoding]::new($false))` y `git commit --amend -F`.
* Para conseguir IDs de vídeo reales (u otros resultados de búsqueda) no sirve pedir la página de YouTube: se renderiza con JavaScript y solo devuelve el pie. Los buscadores bloquean con CAPTCHA si se lanzan peticiones en paralelo. Lo que funciona es descargar el HTML de `youtube.com/results` con `Invoke-WebRequest -UseBasicParsing` y extraer con regex los bloques `"videoRenderer":{"videoId":"…"… "title":{"runs":[{"text":"…"}]}`, y luego confirmar cada candidato con `youtube.com/oembed`. Los seis ID se verifican siempre por HTTP antes de guardarlos.

## Próximos pasos

Orden previsto de implementación:

1. Cambiar el orden por defecto de los tutoriales a «Más populares»: la opción ya existe en la API (`?sort=likes`) y en el ComboBox de orden del frontend, pero el orden por defecto sigue siendo por fecha (`PublishedAt`).
2. Agregar contador de Me Gusta a los comentarios.
3. Ordenar los comentarios por cantidad de Me Gusta.
4. Opcional: enseñar la foto de perfil junto a los comentarios. Requiere añadir `photoUrl` al DTO de comentarios, que hoy no lo expone (hoy solo se ve en la barra superior y en «Mi perfil»).

Nuevas funcionalidades o cambios que todavía no estén definidos se incorporarán posteriormente a esta memoria.
