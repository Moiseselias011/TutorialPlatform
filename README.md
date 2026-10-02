# TutorialHub

Plataforma colaborativa de tutoriales de programación desarrollada con Vue 3, ASP.NET Core Web API, C# y SQL Server.

Problema

Tenía muchos tutoriales y recursos de programación guardados sin una organización clara, por lo que después costaba encontrarlos y recordar para qué servía cada uno.

Solución

Desarrollé una aplicación para compartir, descubrir y organizar tutoriales de programación.

Los usuarios pueden consultar tutoriales, registrarse, publicar contenido, comentar, dar Me gusta y guardar tutoriales en listas personales privadas.

El proyecto también incorpora autenticación mediante JWT, control de acceso según roles y validaciones realizadas en el servidor.

Tecnologías utilizadas

* Frontend: Vue 3, Vite, Vue Router, Pinia y Axios.
* Backend: ASP.NET Core Web API, C# y .NET 10.
* Base de datos: SQL Server y Entity Framework Core 10.
* Autenticación: JWT y BCrypt.Net.
* Documentación: OpenAPI.

Funcionalidades principales

* Registro e inicio de sesión.
* Publicación y edición de tutoriales.
* Búsqueda y paginación de tutoriales.
* Clasificación por tecnología.
* Comentarios.
* Sistema de Me gusta.
* Listas personales privadas.
* Guardado de tutoriales.
* Roles USER y ADMIN.
* Gestión de tecnologías e imágenes por parte del administrador.
* Validaciones en cliente y servidor.

Reglas principales

* Solo ADMIN puede eliminar tutoriales.
* Solo el autor puede editar su tutorial, salvo ADMIN.
* Los usuarios solo pueden administrar sus propias listas.
* Un usuario no puede dar más de un Me gusta al mismo tutorial.
* Guardar un tutorial dos veces no genera duplicados.
* La autorización se controla desde el backend.
* Las contraseñas se almacenan mediante hash.

Puesta en marcha

Requisitos

* .NET SDK 10
* Node.js 20+
* SQL Server
* dotnet-ef si se modifican los modelos

Backend

cd backend/TutorialPlatform.Api
dotnet run --launch-profile http

API: http://localhost:5099

OpenAPI: http://localhost:5099/openapi/v1.json

Frontend

cd frontend/web
npm install
npm run dev

Aplicación: http://localhost:5173

La base de datos se crea y actualiza automáticamente al iniciar el backend mediante las migraciones de EF Core.

Verificación

* 46/46 pruebas de invariantes.
* 22/22 pruebas de validaciones.
* Sin duplicados en Likes.
* Sin diferencias entre likeCount y Likes.
* Sin registros huérfanos.

Objetivo del proyecto

El proyecto nació a partir de una necesidad real y personal, con el objetivo de practicar y aplicar conceptos de desarrollo web, bases de datos, autenticación y desarrollo de APIs.
