# Smoke test – módulo de juegos

Versión 2 · dos partes que se complementan.

## Parte A – Smoke automático (lo ejecuta el pipeline)

13 pruebas marcadas `[Category("Smoke")]`, entre 1 y 3 por juego, que verifican lo esencial de la lógica: se arma el par en Memoria, la pista aparece tras 2 fallos, el temporizador dispara en el umbral, la pausa congela el avance, se completa un hueco en Completar, un trazo en Línea, y la subida de sesiones (contrato de la integración y cola sin conexión).

- Se ejecuta solo en cada push: paso "Smoke test automático" de `bash quality/run-local.sh` y del CI. Dura milisegundos.
- Sale como primera fila del quality gate. Si falla, el gate rechaza.
- Solo local: `dotnet test quality/Juegos.Tests/Juegos.Tests.csproj --filter "Category=Smoke"`.
- **Alcance:** cubre la lógica, no las escenas de Unity.

## Parte B – Smoke manual (ejecutado el 25/10/2026)

> **Ejecución:** se ejecutó a mano en el editor de Unity 6000.4.11f1 (modo Play), sin tablet, el 25/10/2026. Resultado: **smoke fallido**, 31 de 32 pasos obligatorios OK; falla el paso M4 de Memoria (defecto D-07, abierto, TPC-13). Los resultados por paso y el veredicto están al final de este archivo.

Se ejecuta en menos de 15 minutos.

**Objetivo:** confirmar que los cinco juegos arrancan, se pueden jugar hasta el final y respetan las reglas TEA básicas (pausa, errores silenciosos, pista). No reemplaza a las pruebas automáticas: cubre lo que viven en las escenas y los `MonoBehaviour`.

**Criterio de aceptación (se fija antes de ejecutar):** el smoke pasa si los 5 juegos cumplen todos los pasos marcados como obligatorios. Un solo paso obligatorio fallido = smoke fallido, y se registra un bug en Jira.

## Antes de empezar

- [x] Abrir el proyecto `My project` en Unity y esperar a que compile. La consola (Window → General → Console) no debe mostrar errores en rojo.
- [x] Rama y commit probados: `feature/tpcalidad`, commit `af647f3` · Fecha: `25/10/2026` · Quien ejecuta: `Luciana Hueda`
- [x] Para no esperar el tiempo real de la pista, bajar temporalmente el campo **Segundos Para Pista** en el Inspector (Memoria: 10 s en vez de 180 s; Encajar: 5 s en vez de 20 s). **Al terminar, descartar el cambio de la escena con `git checkout -- <escena>`.** No subir esos valores.

## Pasos por juego

Escenas: `JuegoMemoria`, `JuegoEncajarPiezas`, `JuegoCompletar`, `JuegoLinea`, `JuegoEmociones`.

### 1. Memoria

| # | Paso | Esperado | Obligatorio | Resultado |
|---|---|---|---|---|
| M1 | Abrir la escena y dar Play | Carga sin errores en consola, el tablero se ve completo | Sí | OK |
| M2 | Elegir Fácil | 4 pares (8 cartas), cartas boca arriba al empezar | Sí | OK |
| M3 | Dar vuelta dos cartas que no coinciden | Se voltean de nuevo tras una pausa breve, sin sonido de error | Sí | OK |
| M4 | Dar vuelta dos cartas que coinciden | Quedan visibles, feedback suave | Sí | Falla (no da feedback) |
| M5 | Esperar el tiempo de la pista sin acertar | Brilla suavemente la carta que corresponde dar vuelta | Sí | OK |
| M6 | Abrir la pausa durante la espera de la pista | La pista no aparece mientras está pausado; al reanudar la cuenta sigue | Sí | OK |
| M7 | Encontrar todos los pares | Pantalla de fin del juego | Sí | OK |
| M8 | Probar Medio y Difícil | 6 y 8 pares | No | OK |

### 2. Encajar piezas

| # | Paso | Esperado | Obligatorio | Resultado |
|---|---|---|---|---|
| E1 | Abrir la escena y dar Play | Carga sin errores | Sí | OK |
| E2 | Elegir Fácil | 3 formas, piezas abajo, siluetas arriba | Sí | OK |
| E3 | Soltar una pieza sobre una silueta incorrecta | Vuelve a su lugar, sin sonido de error | Sí | OK |
| E4 | Soltar una pieza sobre su silueta | Encaja, silueta se ilumina brevemente | Sí | OK |
| E5 | Esperar el tiempo de la pista sin tocar la pantalla | Brilla la pieza que corresponde | Sí | OK |
| E6 | Elegir Difícil (rompecabezas de imagen) | Aparecen las piezas sueltas | Sí | OK |
| E7 | **Abrir el panel de pausa en Difícil y arrastrar una pieza** | **No se mueve ninguna pieza** (defecto D-01) | **Sí** | OK |
| E8 | Cerrar la pausa y arrastrar una pieza | Se mueve con normalidad | Sí | OK |
| E9 | Completar el juego | Pantalla de fin | Sí | OK |
| E10 | Probar Medio | 5 formas, solo contorno | No | OK |

### 3. Completar palabras o números

| # | Paso | Esperado | Obligatorio | Resultado |
|---|---|---|---|---|
| C1 | Abrir la escena y dar Play | Carga sin errores | Sí | OK |
| C2 | Elegir una opción incorrecta | Sin sonido de error, se puede volver a intentar | Sí | OK |
| C3 | Fallar dos veces seguidas | La opción correcta late como pista | Sí | OK |
| C4 | Elegir la opción correcta | Feedback suave y avanza al siguiente ítem | Sí | OK |
| C5 | Abrir y cerrar la pausa | El juego retoma donde estaba | Sí | OK |
| C6 | Terminar la partida | Pantalla de fin | Sí | OK |

### 4. Escribir siguiendo la línea

| # | Paso | Esperado | Obligatorio | Resultado |
|---|---|---|---|---|
| L1 | Abrir la escena y dar Play | Carga sin errores, se ve el camino guía | Sí | OK |
| L2 | Trazar el camino dentro del margen | El trazo avanza y se completa | Sí | OK |
| L3 | Salirse del camino | Sin sonido de error; se puede volver a retomar | Sí | OK |
| L4 | Abrir y cerrar la pausa | El trazo retoma | Sí | OK |
| L5 | Terminar el trazo | Feedback de finalización | Sí | OK |

### 5. Identificar emociones

| # | Paso | Esperado | Obligatorio | Resultado |
|---|---|---|---|---|
| EM1 | Abrir la escena y dar Play | Carga sin errores | Sí | OK |
| EM2 | Elegir una emoción incorrecta | Sin sonido de error | Sí | OK |
| EM3 | Fallar dos veces seguidas | La opción correcta se destaca con borde suave **y la ronda sigue abierta** (defecto D-02) | Sí | OK |
| EM4 | Elegir la opción correcta | Feedback suave, siguiente ronda | Sí | OK |
| EM5 | Abrir y cerrar la pausa | El juego retoma | Sí | OK |
| EM6 | Probar el segundo nivel (situaciones) | Carga y se puede jugar | No | OK |

## Resultado

| Juego | Pasos obligatorios OK | Pasos obligatorios fallidos | Bug en Jira |
|---|---|---|---|
| Memoria | 6 /7 | 1 (M4) | D-07 (TPC-13) |
| Encajar | 9 /9 | 0 | — |
| Completar | 6 /6 | 0 | — |
| Línea | 5 /5 | 0 | — |
| Emociones | 5 /5 | 0 | — |

**Veredicto:** ☐ Smoke aprobado ☒ Smoke fallido (31 de 32 pasos obligatorios OK; los 3 opcionales también OK)

**Observaciones:** el único paso fallido es M4 (Memoria): al dar vuelta dos cartas que coinciden no hay feedback. El resto de los pasos obligatorios pasó en los cinco juegos, incluidos E7 (la pausa frena las piezas en Difícil, D-01) y EM3 (la respuesta se destaca sin cerrar la ronda, D-02). Según el criterio fijado antes de ejecutar, un paso obligatorio fallido hace fallar el smoke. El defecto no se corrigió en esta entrega y queda registrado como D-07 (Media, abierto, TPC-13; caso TC-07 = TPC-14). Se ejecutó en el editor de Unity; no se probó en una tablet.

## Después de ejecutarlo

- [ ] Descartar los cambios de prueba en las escenas (`git status` no debe mostrar escenas modificadas).
- [ ] Guardar una captura por juego y una del panel de pausa en Difícil (paso E7).
- [x] Registrar en Jira cada paso fallido como bug (D-07 = TPC-13).
