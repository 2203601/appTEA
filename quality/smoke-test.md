# Smoke test – módulo de juegos

Versión 2 · dos partes que se complementan.

## Parte A – Smoke automático (lo ejecuta el pipeline)

13 pruebas marcadas `[Category("Smoke")]`, entre 1 y 3 por juego, que verifican lo esencial de la lógica: se arma el par en Memoria, la pista aparece tras 2 fallos, el temporizador dispara en el umbral, la pausa congela el avance, se completa un hueco en Completar, un trazo en Línea, y la subida de sesiones (contrato de la integración y cola sin conexión).

- Se ejecuta solo en cada push: paso "Smoke test automático" de `bash quality/run-local.sh` y del CI. Dura milisegundos.
- Sale como primera fila del quality gate. Si falla, el gate rechaza.
- Solo local: `dotnet test quality/Juegos.Tests/Juegos.Tests.csproj --filter "Category=Smoke"`.
- **Alcance:** cubre la lógica, no las escenas de Unity.

## Parte B – Smoke manual (pendiente de ejecutar)

> **Marcado para el informe final:** se ejecutará a mano en Unity (Play) o en la tablet para aportar evidencia de lo que el automático no cubre: escenas, `MonoBehaviour`, pausa real en Difícil (D-01), compilación de `GameManagerEncajar.cs` y aspecto de las pistas. Registrar resultados y capturas en este archivo y en Jira.

Se ejecuta en menos de 15 minutos.

**Objetivo:** confirmar que los cinco juegos arrancan, se pueden jugar hasta el final y respetan las reglas TEA básicas (pausa, errores silenciosos, pista). No reemplaza a las pruebas automáticas: cubre lo que viven en las escenas y los `MonoBehaviour`.

**Criterio de aceptación (se fija antes de ejecutar):** el smoke pasa si los 5 juegos cumplen todos los pasos marcados como obligatorios. Un solo paso obligatorio fallido = smoke fallido, y se registra un bug en Jira.

## Antes de empezar

- [ ] Abrir el proyecto `My project` en Unity y esperar a que compile. La consola (Window → General → Console) no debe mostrar errores en rojo.
- [ ] Rama y commit probados: `________________` · Fecha: `__/__/____` · Quien ejecuta: `________________`
- [ ] Para no esperar el tiempo real de la pista, bajar temporalmente el campo **Segundos Para Pista** en el Inspector (Memoria: 10 s en vez de 180 s; Encajar: 5 s en vez de 20 s). **Al terminar, descartar el cambio de la escena con `git checkout -- <escena>`.** No subir esos valores.

## Pasos por juego

Escenas: `JuegoMemoria`, `JuegoEncajarPiezas`, `JuegoCompletar`, `JuegoLinea`, `JuegoEmociones`.

### 1. Memoria

| # | Paso | Esperado | Obligatorio | Resultado |
|---|---|---|---|---|
| M1 | Abrir la escena y dar Play | Carga sin errores en consola, el tablero se ve completo | Sí | |
| M2 | Elegir Fácil | 4 pares (8 cartas), cartas boca arriba al empezar | Sí | |
| M3 | Dar vuelta dos cartas que no coinciden | Se voltean de nuevo tras una pausa breve, sin sonido de error | Sí | |
| M4 | Dar vuelta dos cartas que coinciden | Quedan visibles, feedback suave | Sí | |
| M5 | Esperar el tiempo de la pista sin acertar | Brilla suavemente la carta que corresponde dar vuelta | Sí | |
| M6 | Abrir la pausa durante la espera de la pista | La pista no aparece mientras está pausado; al reanudar la cuenta sigue | Sí | |
| M7 | Encontrar todos los pares | Pantalla de fin del juego | Sí | |
| M8 | Probar Medio y Difícil | 6 y 8 pares | No | |

### 2. Encajar piezas

| # | Paso | Esperado | Obligatorio | Resultado |
|---|---|---|---|---|
| E1 | Abrir la escena y dar Play | Carga sin errores | Sí | |
| E2 | Elegir Fácil | 3 formas, piezas abajo, siluetas arriba | Sí | |
| E3 | Soltar una pieza sobre una silueta incorrecta | Vuelve a su lugar, sin sonido de error | Sí | |
| E4 | Soltar una pieza sobre su silueta | Encaja, silueta se ilumina brevemente | Sí | |
| E5 | Esperar el tiempo de la pista sin tocar la pantalla | Brilla la pieza que corresponde | Sí | |
| E6 | Elegir Difícil (rompecabezas de imagen) | Aparecen las piezas sueltas | Sí | |
| E7 | **Abrir el panel de pausa en Difícil y arrastrar una pieza** | **No se mueve ninguna pieza** (defecto D-01) | **Sí** | |
| E8 | Cerrar la pausa y arrastrar una pieza | Se mueve con normalidad | Sí | |
| E9 | Completar el juego | Pantalla de fin | Sí | |
| E10 | Probar Medio | 5 formas, solo contorno | No | |

### 3. Completar palabras o números

| # | Paso | Esperado | Obligatorio | Resultado |
|---|---|---|---|---|
| C1 | Abrir la escena y dar Play | Carga sin errores | Sí | |
| C2 | Elegir una opción incorrecta | Sin sonido de error, se puede volver a intentar | Sí | |
| C3 | Fallar dos veces seguidas | La opción correcta late como pista | Sí | |
| C4 | Elegir la opción correcta | Feedback suave y avanza al siguiente ítem | Sí | |
| C5 | Abrir y cerrar la pausa | El juego retoma donde estaba | Sí | |
| C6 | Terminar la partida | Pantalla de fin | Sí | |

### 4. Escribir siguiendo la línea

| # | Paso | Esperado | Obligatorio | Resultado |
|---|---|---|---|---|
| L1 | Abrir la escena y dar Play | Carga sin errores, se ve el camino guía | Sí | |
| L2 | Trazar el camino dentro del margen | El trazo avanza y se completa | Sí | |
| L3 | Salirse del camino | Sin sonido de error; se puede volver a retomar | Sí | |
| L4 | Abrir y cerrar la pausa | El trazo retoma | Sí | |
| L5 | Terminar el trazo | Feedback de finalización | Sí | |

### 5. Identificar emociones

| # | Paso | Esperado | Obligatorio | Resultado |
|---|---|---|---|---|
| EM1 | Abrir la escena y dar Play | Carga sin errores | Sí | |
| EM2 | Elegir una emoción incorrecta | Sin sonido de error | Sí | |
| EM3 | Fallar dos veces seguidas | La opción correcta se destaca con borde suave **y la ronda sigue abierta** (defecto D-02) | Sí | |
| EM4 | Elegir la opción correcta | Feedback suave, siguiente ronda | Sí | |
| EM5 | Abrir y cerrar la pausa | El juego retoma | Sí | |
| EM6 | Probar el segundo nivel (situaciones) | Carga y se puede jugar | No | |

## Resultado

| Juego | Pasos obligatorios OK | Pasos obligatorios fallidos | Bug en Jira |
|---|---|---|---|
| Memoria | /7 | | |
| Encajar | /9 | | |
| Completar | /6 | | |
| Línea | /5 | | |
| Emociones | /5 | | |

**Veredicto:** ☐ Smoke aprobado ☐ Smoke fallido

**Observaciones:** ______________________________________________

## Después de ejecutarlo

- [ ] Descartar los cambios de prueba en las escenas (`git status` no debe mostrar escenas modificadas).
- [ ] Guardar una captura por juego y una del panel de pausa en Difícil (paso E7).
- [ ] Registrar en Jira cada paso fallido como bug.
