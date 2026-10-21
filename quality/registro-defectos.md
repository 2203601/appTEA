# Registro de defectos – módulo de juegos

Versión 1 · rama `feature/tpcalidad`

Cada defecto tiene pasos, resultado esperado, resultado obtenido, el caso de prueba que lo cubre y el commit de la corrección. Los defectos se clasifican por **origen**: del producto (juego) o del proceso (pipeline).

**Severidad:** Alta = el juego se comporta mal para el niño o el pipeline no puede correr · Media = desvío de la especificación · Baja = molestia sin efecto en el uso.

| ID | Título | Origen | Severidad | Estado |
|---|---|---|---|---|
| D-01 | Encajar (Difícil): las piezas se mueven con el panel de pausa abierto | Producto | Alta | Corregido |
| D-02 | Emociones: al segundo error se revelaba la respuesta y se cerraba la ronda | Producto | Media | Corregido |
| D-03 | Pipeline: los `.csproj` de `quality/` no llegaban al repositorio | Proceso | Alta | Corregido |
| D-04 | Pipeline: el análisis estático daba 0 hallazgos por un `obj/` compartido | Proceso | Media | Corregido |
| D-05 | Pipeline: `ci.yml` con YAML inválido | Proceso | Alta | Corregido |

---

## D-01 · Encajar (Difícil): piezas movibles con el panel de pausa abierto

- **Origen / módulo:** Producto · Encajar piezas, `GameManagerEncajar.Update`
- **Severidad:** Alta (rompe la regla "ningún temporizador ni acción avanza en pausa")
- **Cómo se encontró:** revisión del código al agregar la pista de 20 s. El `Update` del rompecabezas lee `Pointer.current` directamente y no pasa por el `EventSystem`, así que la pausa no lo frena.
- **Pasos para reproducir**
  1. Abrir Encajar en dificultad Difícil.
  2. Abrir el panel de pausa (`Time.timeScale = 0`).
  3. Arrastrar con el dedo una de las piezas sueltas del rompecabezas.
- **Resultado esperado:** con la pausa abierta no se mueve ninguna pieza ni avanza la pista.
- **Resultado obtenido:** las piezas se mueven, y esos toques reinician el contador de la pista.
- **Corrección:** el `Update` corta el cuadro si el juego está en pausa. La regla quedó en `Comun/Logica/ControlPausa.cs` y el juego la usa mediante `ControlPausa.EnPausa(Time.timeScale)`.
- **Commits:** corrección inicial en `bd07cd9`; regla extraída y probada en `0cad24e`.
- **Casos de prueba** (`Comun/Tests/EditMode/ControlPausaTests.cs`)
  - `ConTimeScaleCero_EstaEnPausa`
  - `ConTimeScaleMayorACero_NoEstaEnPausa` (0,5 / 1 / 2)
  - `EnPausa_LaPistaNoAvanzaNiConPasoDeTiempoGrande`
- **Contraste rojo/verde (ejecutado en local con `bash quality/run-local.sh`)**
  - Rojo, `EnPausa` devolviendo siempre `false` (versión con el defecto): 80/82 pasan, 2 fallan, quality gate rechazado.
  - Verde, con la corrección: 82/82 pasan, quality gate aprobado.
- **Limitación:** las pruebas cubren la regla, no el `Update` de Unity. La verificación en el juego es manual (ver smoke test).

## D-02 · Emociones: se revelaba la respuesta al segundo error

- **Origen / módulo:** Producto · Identificar emociones, `GameManagerEmocion`
- **Severidad:** Media (desvío de la especificación)
- **Pasos para reproducir:** en cualquier nivel, fallar dos veces seguidas en la misma ronda.
- **Resultado esperado (especificación):** tras el segundo intento fallido, la opción correcta se destaca con un borde suave y el niño tiene que elegirla.
- **Resultado obtenido:** el juego mostraba la respuesta y cerraba la ronda sin que el niño la eligiera.
- **Corrección:** la pista destaca la opción correcta y la ronda sigue abierta. La regla "pista tras 2 fallos" quedó en `Comun/Logica/RondaDeOpciones.cs`.
- **Commits:** `bd07cd9` (comportamiento) y `0cad24e` / `2b9e422` (regla testeable).
- **Casos de prueba** (`RondaDeOpcionesTests`)
  - `PrimerFallo_NoActivaLaPista`
  - `SegundoFallo_ActivaLaPista`
  - `LuegoDeLaPista_CadaFalloSigueDandoPista`
  - `ValorPorDefecto_SonDosFallos`
- **Contraste rojo/verde:** el rojo se demostró en el pipeline con el cambio `Fallos >= FallosParaPista` → `Fallos > FallosParaPista` (ejecución `[demo-fallo]`, commit `effddca`): 3 pruebas fallan y el quality gate rechaza. Esto demuestra que las pruebas detectan la regresión de esta regla.

## D-03 · Pipeline: los `.csproj` de `quality/` quedaban fuera del repositorio

- **Origen / módulo:** Proceso · `.gitignore`
- **Severidad:** Alta (el pipeline no habría podido compilar en GitHub)
- **Cómo se encontró:** al preparar el primer commit, revisando qué archivos entraban.
- **Pasos para reproducir:** clonar el repositorio sin los `.csproj` y ejecutar `bash quality/run-local.sh`.
- **Resultado esperado:** el pipeline compila las pruebas.
- **Resultado obtenido:** el `.gitignore` de Unity ignora todos los `*.csproj`, así que `quality/Juegos.Logica` y `quality/Juegos.Tests` no se subían.
- **Corrección:** excepción en `.gitignore` solo para `quality/`.
- **Commit:** `2b9e422`.
- **Caso de prueba:** la propia ejecución del pipeline en GitHub (ejecución #1, verde).

## D-04 · Pipeline: el análisis estático daba 0 hallazgos por un `obj/` compartido

- **Origen / módulo:** Proceso · `quality/Juegos.Logica` y `quality/Juegos.Tests`
- **Severidad:** Media (resultado engañoso, "0 hallazgos" sin que fuera cierto)
- **Cómo se encontró:** el resultado "0 hallazgos" resultó sospechoso en una ejecución local.
- **Pasos para reproducir:** restaurar y compilar ambos proyectos desde la misma carpeta.
- **Resultado esperado:** el análisis estático informa los hallazgos reales del código.
- **Resultado obtenido:** ambos `.csproj` compartían `obj/project.assets.json` y cada restauración pisaba la del otro.
- **Corrección:** cada proyecto en su propia subcarpeta.
- **Commit:** `2b9e422`.
- **Caso de prueba:** el quality gate informa 4 hallazgos (code smells), valor estable entre ejecuciones locales y de GitHub.

## D-05 · Pipeline: `ci.yml` con YAML inválido

- **Origen / módulo:** Proceso · `.github/workflows/ci.yml`
- **Severidad:** Alta (el CI no arrancaba)
- **Cómo se encontró:** GitHub mostró "Invalid workflow file … error in your yaml syntax on line 29".
- **Pasos para reproducir:** hacer push del workflow con `name: Inyectar defecto (… gate: solo …)` sin comillas.
- **Resultado esperado:** el workflow se ejecuta.
- **Resultado obtenido:** el `: ` dentro del nombre del paso se interpreta como una clave nueva y el archivo no se carga.
- **Corrección:** nombre del paso entre comillas.
- **Commit:** `a330366`.
- **Caso de prueba:** ejecución verde posterior en GitHub (`91dbce8`).

---

## Plantilla para defectos nuevos

```
## D-XX · <título>
- Origen / módulo:
- Severidad:
- Cómo se encontró:
- Pasos para reproducir:
- Resultado esperado:
- Resultado obtenido:
- Corrección:
- Commit:
- Casos de prueba:
- Contraste rojo/verde:
```
