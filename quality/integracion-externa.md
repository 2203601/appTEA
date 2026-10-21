# Integración externa – subida de sesiones de juego

Versión 1 · alcance: módulo de juegos.

## 1. Qué se integra

Al terminar una partida, la tablet sube la sesión (datos y métricas de un niño) a un servicio externo. El servicio elegido es **Supabase** (Postgres administrado con autenticación y API REST). La subida usa una función de base de datos, `upload_game_session`, que escribe de forma atómica las tablas `game_sessions`, `game_session_participants`, `metrics` y `game_metrics_detail`.

La integración tiene que funcionar **sin conexión permanente**: la sesión se guarda en una cola local y se sube cuando hay red.

## 2. Contrato

| | |
|---|---|
| Método y ruta | `POST {SUPABASE_URL}/rest/v1/rpc/upload_game_session` |
| Cabeceras | `apikey: <clave anon>`, `Authorization: Bearer <token del usuario>`, `Content-Type: application/json` |
| Cuerpo | `{"profile_id", "session": {...}, "metrics": {...}?, "details": [...]?}` (ver abajo) |
| Autorización | Solo el adulto responsable del niño (`profile_id`) puede subir. La base lo comprueba con el token. |
| Idempotencia | Todos los ids los genera la tablet. Subir dos veces la misma sesión no duplica filas (`on conflict do nothing`). |

Cuerpo de ejemplo (es exactamente lo que genera `ContratoSubida.ConstruirPayload` y lo que verifica una prueba):

```json
{
  "profile_id": "11111111-1111-1111-1111-111111111111",
  "session": {
    "id": "22222222-2222-2222-2222-222222222222",
    "game_id": "00000000-0000-0000-0000-000000000001",
    "started_at": "2026-10-20T12:00:00Z",
    "ended_at": "2026-10-20T12:02:10Z",
    "duration_sec": 120,
    "paused_sec": 10,
    "completed": true,
    "difficulty": "facil",
    "level": 1
  },
  "metrics": {
    "id": "33333333-3333-3333-3333-333333333333",
    "correct_answers": 5,
    "wrong_answers": 2,
    "avg_response_ms": 1234.5,
    "hints_used": 1,
    "recorded_at": "2026-10-20T12:02:10Z"
  },
  "details": [
    { "id": "44444444-4444-4444-4444-444444444444", "metric_key": "first_try_pieces", "metric_value": 2 }
  ]
}
```

Reglas de los datos (se validan en la tablet antes de enviar, con las mismas restricciones que la base):

| Campo | Regla |
|---|---|
| `profile_id`, `session.id`, `session.game_id` | UUID no vacío |
| `ended_at` | nulo, o no anterior a `started_at` |
| `duration_sec` (tiempo activo), `paused_sec` | enteros ≥ 0 |
| `difficulty` | nulo, o `facil`, `medio`, `dificil` |
| `correct_answers`, `wrong_answers`, `hints_used` | ≥ 0. Nulos en juegos sin aciertos (Línea) |
| `details[].metric_key` | no vacía y única dentro de la sesión |
| `details[].metric_value`, `avg_response_ms` | número finito (sin NaN ni infinito), con punto decimal sea cual sea la cultura del dispositivo |

## 3. Respuestas y qué hace la app

| Código HTTP | Resultado en la app | Qué pasa con la sesión |
|---|---|---|
| 200–299 | `Subida` | Se saca de la cola |
| 401 | `NoAutorizado` | Se conserva; se detiene el vaciado hasta renovar el token |
| 400, 403, 404, 409, 422 y otros 4xx | `Rechazada` | Se aparta (no se reintenta) y la cola sigue con las demás |
| 408, 429, 5xx, 1xx, 3xx | `Reintentar` | Se conserva; se detiene el vaciado |
| Sin red (timeout, DNS) | `Reintentar` | Se conserva; se detiene el vaciado |
| Datos inválidos (validación local) | `Invalida` | No se envía ni se encola |

El vaciado de la cola es **en orden**: ante una falla transitoria se corta, para no insistir con cada sesión mientras no haya red.

## 4. Dónde está en el código

| Pieza | Archivo (`My project/Assets/_Scripts/Comun/Logica/Sesiones/`) |
|---|---|
| Datos de la sesión | `SesionDeJuego.cs` |
| Contrato: JSON y validación | `ContratoSubida.cs` |
| Frontera con el servicio (interfaz) | `ITransporte.cs` |
| Cliente: clasifica las respuestas | `ClienteSubida.cs` |
| Cola sin conexión | `ColaDeSesiones.cs` |

Es lógica pura (sin `UnityEngine`), así que se prueba en el pipeline sin Unity. La implementación real de `ITransporte` (con `UnityWebRequest`) es la única parte que depende de Unity.

## 5. Pruebas

Se usa un **doble de prueba** (`TransporteFalso`) que devuelve respuestas guionadas y registra lo enviado. Las pruebas están en `Comun/Tests/EditMode/Sesiones/` y corren en el pipeline.

| Qué se prueba | Pruebas |
|---|---|
| Cuerpo JSON exacto del contrato, omisión de bloques opcionales, nulos, escape de caracteres, punto decimal con cultura de coma | `ContratoSubidaTests` |
| Validación: clases de equivalencia y valores límite (duración 0, fin = inicio, dificultad nula, NaN, claves repetidas) | `ContratoSubidaTests` |
| Clasificación de cada código HTTP, incluidos los límites 199, 299, 300, 499, 599 | `ClienteSubidaTests` |
| Sin red, sin token, sesión inválida (no se envía) | `ClienteSubidaTests` |
| Idempotencia: un reintento manda exactamente el mismo cuerpo | `ClienteSubidaTests` |
| Cola: orden, corte ante falla de red, recuperación sin duplicar, rechazada apartada, token vencido | `ColaDeSesionesTests` |

Dos de ellas están marcadas como smoke (`Category("Smoke")`) y corren primero en el pipeline.

Se verificó que las pruebas detectan defectos reales mediante mutaciones locales: sin cultura invariante falla 1 prueba; tratar el 429 como rechazo definitivo falla 1; seguir vaciando tras una falla de red falla 2.

## 6. Lo que NO está cubierto (límites declarados)

- **No hay prueba contra un Supabase real.** Hoy la integración se prueba con dobles. Una integración contra un entorno real corresponde al nivel 2 de la consigna.
- **La implementación de `ITransporte` con `UnityWebRequest` no está escrita.** Depende de Unity y del login, que es otro módulo.
- **El esquema de la base** (`supabase/migrations/`) se verificó en su momento contra un Postgres 16 en Docker con autenticación simulada, no contra Supabase. Esas pruebas no forman parte de este pipeline.
- **La cola no persiste en disco todavía**: vive en memoria. Si la app se cierra antes de subir, se pierde. Es un riesgo a registrar.
- **Las pruebas no detectan cambios en el contrato del servidor.** Si la base cambia, hay que actualizar el contrato a mano.

## 7. Plan de contingencia

Si Supabase no está disponible (caída o plan gratuito pausado), la app sigue funcionando: la sesión queda en la cola y se reintenta. Ningún juego depende de la subida para jugar.

## 8. Próximos pasos para subir de nivel

1. Implementar `ITransporte` con `UnityWebRequest` y guardar la cola en disco.
2. Crear el proyecto de Supabase y aplicar `supabase/migrations/`.
3. Probar la subida contra el entorno real con una cuenta de prueba (el token va como secreto, nunca en el repositorio).
4. Medir la carga contra la API con una herramienta de carga (k6) y registrar umbrales.
