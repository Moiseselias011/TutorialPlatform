# TutorialHub — Frontend (Vue 3)

Esta es la aplicación **Vue 3** de TutorialHub.

Las instrucciones completas de instalación, arranque, credenciales de
demostración y reglas de negocio están en el
[README de la raíz del proyecto](../../README.md).

## Comandos

```bash
npm install      # dependencias
npm run dev      # servidor de desarrollo en http://localhost:5173
npm run build    # compila a dist/
npm run preview  # previsualiza la compilación
```

## Configuración

No hay variables de entorno que configurar: `vite.config.js` hace *proxy* de
`/api` y `/uploads` a `http://localhost:5099`, que debe ser el puerto del
backend.
