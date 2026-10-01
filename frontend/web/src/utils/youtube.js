/**
 * Miniaturas de YouTube sin API ni dependencias nuevas.
 *
 * Solo se deriva una URL a partir de la que ya tiene el tutorial
 * (`https://i.ytimg.com/vi/{id}/...`), así que no hay llamadas de
 * autenticación ni cuota: el navegador pide la imagen directamente.
 */

const ID_RE = /^[A-Za-z0-9_-]{11}$/

/**
 * Extrae el ID de vídeo de una URL de YouTube.
 *
 * Formatos admitidos:
 *   https://www.youtube.com/watch?v=XXXXXXXXXXX
 *   https://youtu.be/XXXXXXXXXXX
 *   https://www.youtube.com/embed/XXXXXXXXXXX
 *   https://www.youtube.com/shorts/XXXXXXXXXXX
 *   https://www.youtube.com/live/XXXXXXXXXXX
 *   youtube.com/watch?v=XXXXXXXXXXX&t=30s   (sin esquema)
 *
 * @param {string} url
 * @returns {string|null} el ID de 11 caracteres o null si no es de YouTube
 */
export function youtubeId(url) {
  if (!url) return null

  let value = String(url).trim()
  if (!value) return null

  // Se acepta también «youtu.be/...» escrito sin esquema.
  if (!/^[a-z][a-z0-9+.-]*:\/\//i.test(value)) value = `https://${value}`

  let parsed
  try {
    parsed = new URL(value)
  } catch {
    return null
  }

  // «www.», «m.» y «music.» son el mismo origen a efectos prácticos.
  const host = parsed.hostname.toLowerCase().replace(/^(www|m|music)\./, '')

  let id = null

  if (host === 'youtu.be') {
    id = parsed.pathname.split('/').filter(Boolean)[0] ?? null
  } else if (host === 'youtube.com' || host === 'youtube-nocookie.com') {
    if (parsed.pathname.replace(/\/+$/, '') === '/watch') {
      id = parsed.searchParams.get('v')
    } else {
      // /embed/ID, /shorts/ID, /live/ID, /v/ID
      const match = parsed.pathname.match(/^\/(?:embed|shorts|live|v)\/([^/?#]+)/)
      if (match) id = match[1]
    }
  }

  return id && ID_RE.test(id) ? id : null
}

/**
 * URL de la miniatura del vídeo.
 *
 * Se usa `hqdefault.jpg` (480×360): existe para todos los vídeos, a
 * diferencia de `maxresdefault.jpg`, que devuelve 404 en los más antiguos.
 *
 * @param {string} url
 * @returns {string|null}
 */
export function youtubeThumb(url) {
  const id = youtubeId(url)
  return id ? `https://i.ytimg.com/vi/${id}/hqdefault.jpg` : null
}
