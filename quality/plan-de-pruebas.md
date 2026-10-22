# Plan de pruebas – módulo de juegos de appTEA

| | |
|---|---|
| Versión | 1.0 |
| Fecha | 21/10/2026 |
| Rama | `feature/tpcalidad` |
| Alcance | Módulo de juegos: Memoria, Encajar piezas, Completar palabras, Escribir siguiendo la línea, Identificar emociones, y la subida de sesiones de juego |
| Cambios | 1.0: versión inicial |

Las cifras de este documento salen de las ejecuciones del pipeline (GitHub Actions) y de `bash quality/run-local.sh`. Lo que no se pudo medir se marca como pendiente.

## 1. Alcance y estrategia

**Dentro del alcance:** la lógica de los cinco juegos (carpetas `Logica/`), el código común (`Comun/`: pausa, pistas, rondas de opciones, subida de sesiones) y el pipeline de calidad.

**Fuera del alcance:** login y creación de cuenta, menú de juegos, panel de seguimiento, backend en Supabase (el esquema existe pero no se prueba en este plan), y los `MonoBehaviour` y escenas de Unity, que no se miden con cobertura.

**Estrategia.** La lógica de cada juego se separó de Unity en clases puras. Eso permite probarla con NUnit fuera de Unity, medir cobertura y correr todo en el pipeline sin licencia de Unity. Lo que vive en las escenas (botones, animaciones, sonido, pausa en el juego real) se verifica con un smoke test manual.

**Nivel declarado (propuesta, a decidir por el grupo):** nivel 1, con dos metas del nivel 2 cumplidas. Ver la sección 11.

### Pirámide de pruebas

| Nivel | Qué hay | Cantidad | Dónde corre |
|---|---|---|---|
| Unitarias | Reglas de cada juego, pausa, temporizador, contrato de subida | 157 pruebas | Pipeline, jobs 2 y 3 |
| Integración | Cliente de subida y cola de sesiones contra un doble de prueba del servicio | 29 pruebas | Pipeline, job 3 |
| Smoke automático | Subconjunto esencial de las anteriores (`Category("Smoke")`) | 13 pruebas | Pipeline, job 2 (primero) |
| Stress y esfuerzo sostenido | 5 escenarios de partidas completas, en paralelo y durante 60 s | 5 escenarios | Pipeline, job 5 |
| Mutación | Mide si las pruebas detectan defectos | 536 mutantes evaluados | Pipeline, job 6 |
| Sistema (smoke manual) | Los cinco juegos en Unity, incluidas pausa y pistas | 35 pasos, 32 obligatorios | A mano, `quality/smoke-test.md` (pendiente de ejecutar) |
| Aceptación | Validación con las profesionales y con niños | — | Pendiente, fuera de este TP |

Total de pruebas automáticas: **186**, todas en verde.

## 2. Criterios de entrada y de salida

**Criterios de entrada** (para correr el quality gate completo):
- El código compila (job 1) sin errores.
- La rama es `feature/tpcalidad` y el commit llegó al repositorio.
- El runner usa el SDK 8 (`quality/global.json`) y `ubuntu-24.04`.

**Criterios de salida** (todos son umbrales del quality gate, `quality/gate.py`):

| Criterio | Umbral | Valor actual |
|---|---|---|
| Smoke automático | 0 fallos | 13/13 |
| Pruebas | 0 fallos | 186/186 |
| Cobertura de líneas de la lógica de juegos | ≥ 80 % | 99,2 % |
| Duplicación de código | ≤ 5 % | 3,28 % |
| Análisis estático | 0 bugs o vulnerabilidades bloqueantes | 4 code smells, 0 bloqueantes |
| Stress y esfuerzo sostenido | 0 fallas (p99, degradación, memoria) | 0 fallas |
| Puntaje de mutación | ≥ 85 % | 91,79 % |

Criterios de salida adicionales, que el gate no verifica:
- Ningún defecto de severidad Alta abierto.
- Smoke manual ejecutado y con resultado registrado (pendiente).

## 3. Requisitos y trazabilidad

Los requisitos salen de las especificaciones de cada juego del proyecto; los identificadores se asignaron en este plan. La cadena es requisito → caso de prueba → defecto.

| Req. | Requisito | Casos de prueba automáticos | Smoke manual | Defecto |
|---|---|---|---|---|
| RF-MEM-01 | Memoria arma el mazo con 4, 6 u 8 pares según la dificultad | `TableroMemoriaTests`: ArmarMazo_*, CantidadPares_* | M2, M8 | — |
| RF-MEM-02 | Dos cartas distintas no forman par y se vuelven a tapar sin sonido de error | `TableroMemoriaTests`: SegundaCartaDistinta_*, TrasUnNoPar_* | M3 | — |
| RF-MEM-03 | A los 3 minutos sin acertar, brilla la carta que toca dar vuelta | `TableroMemoriaTests`: Pista_* · `TemporizadorInactividadTests` | M5, M6 | — |
| RF-MEM-04 | El juego termina al encontrar todos los pares | `TableroMemoriaTests`: EncontrarTodosLosPares_*, TableroVacio_* | M7 | — |
| RF-ENC-01 | Encajar usa 3 formas (Fácil), 5 (Medio) y 7 rotadas (Difícil) | Sin prueba automática (lógica dentro de `GameManagerEncajar`) | E2, E6, E10 | — |
| RF-ENC-02 | Una pieza incorrecta vuelve a su lugar sin sonido de error | Sin prueba automática | E3 | — |
| RF-ENC-03 | A los 20 s sin tocar la pantalla brilla la pieza que corresponde | `TemporizadorInactividadTests` | E5 | — |
| RF-ENC-04 | En Difícil se arma un rompecabezas de imagen | Sin prueba automática | E6, E9 | — |
| RF-COM-01 | Completar muestra 2, 3 o 4 opciones, con la correcta incluida y sin repetir | `RondaCompletarTests`: Opciones_* · `GeneradorHuecosTests`: NoHayDistractoresRepetidos_* · `GeneradorHuecosLimitesTests` | — | — |
| RF-COM-02 | Tras 2 fallos, la opción correcta late como pista | Sin prueba automática (la regla vive en el juego) | C3 | — |
| RF-COM-03 | Ningún distractor forma otra palabra del diccionario | `GeneradorHuecosTests`: NingunDistractorFormaUnaPalabraDelDiccionario | — | — |
| RF-LIN-01 | El trazo avanza solo siguiendo el camino y no se puede saltar al final | `SeguidorTrazoTests`: NoSePuedeSaltarAlFinal_*, NoRetrocede_* · `SeguidorTrazoLimitesTests` | L2, L3 | — |
| RF-LIN-02 | El trazo se completa al llegar al 97 % del camino | `SeguidorTrazoTests`: UnaRectaSeCompleta, UnCirculoSeCompleta · `SeguidorTrazoLimitesTests`: Completo_* | L5 | — |
| RF-EMO-01 | Emociones muestra de 2 a 4 opciones distintas, con la correcta incluida | `RondaDeOpcionesTests`: ElegirOpciones_* | EM1 | — |
| RF-EMO-02 | Tras 2 intentos fallidos se destaca la correcta y la ronda sigue abierta | `RondaDeOpcionesTests`: PrimerFallo_*, SegundoFallo_*, LuegoDeLaPista_* | EM3 | **D-02** |
| RT-01 | La pausa congela temporizadores y movimientos en todos los juegos | `ControlPausaTests` (5) · `TemporizadorInactividadTests`: ConDeltaCero_* · stress: "Pausa + pista" | M6, E7, C5, L4, EM5 | **D-01** |
| RT-02 | Los errores del niño son silenciosos (sin sonido ni mensaje negativo) | Sin prueba automática | M3, E3, C2, L3, EM2 | — |
| RT-03 | La subida de sesiones respeta el contrato y no duplica | `ContratoSubidaTests` · `ContratoSubidaMensajesTests` · `ClienteSubidaTests`: Subir_UnReintento_* | — | — |
| RT-04 | La app funciona sin conexión permanente: las sesiones esperan en una cola | `ColaDeSesionesTests` · `ClienteSubidaTests`: Subir_SinConexion_* | — | — |
| RT-05 | El rendimiento se mantiene bajo carga prolongada | Stress y esfuerzo sostenido (5 escenarios) | — | — |
| RP-01 | El pipeline compila, analiza, prueba y decide en cada push | Ejecuciones del pipeline en GitHub Actions | — | **D-03, D-04, D-05, D-06** |

**Huecos de trazabilidad (declarados):** RF-ENC-01, RF-ENC-02, RF-ENC-04, RF-COM-02 y RT-02 no tienen prueba automática. Dependen del smoke manual, que todavía no se ejecutó.

## 4. Casos de prueba: técnicas aplicadas

### 4.1 Clases de equivalencia y valores límite

| Entrada | Clases válidas | Clases inválidas | Valores límite probados |
|---|---|---|---|
| `dificultad` de la sesión | `facil`, `medio`, `dificil`; nulo | `extremo`, vacío, `Facil` | cada valor válido; nulo |
| `duration_sec`, `paused_sec` | 0 y mayores | negativos | **0** (válido), **-1** (inválido) |
| `ended_at` contra `started_at` | igual o posterior | anterior | **igual** (válido), **1 s antes** (inválido) |
| Contadores (aciertos, errores, pistas) | 0 y mayores | negativos | **0** (válido), **-1** (inválido) |
| `metric_value` | números finitos | NaN, ±infinito | NaN, +∞, -∞ |
| `metric_key` | no vacía y única | vacía, repetida | vacía; repetida |
| Código HTTP de la respuesta | 2xx, 401, 4xx, 408/429/5xx | 1xx y 3xx (inesperados) | **199, 299, 300, 499, 599** |
| Cantidad de puntos del trazo | 2 o más | nulo, 1 | **1** (inválido), **2** (válido) |
| Tolerancia del trazo | mayor que 0 | 0, negativa | **0** (inválido), **0,001** (válido) |
| Distancia del dedo al camino | hasta la tolerancia | mayor que la tolerancia | **exactamente la tolerancia** (dentro), **+1** (fuera) |
| Salto del dedo hacia adelante | dentro de la ventana (4 × tolerancia) | más allá | **justo el borde** (avanza), **más allá** (no avanza) |
| Huecos pedidos contra letras ocultables | hasta la cantidad de letras ocultables | más que las ocultables | **igual** (válido), **+1** (inválido) |
| Opciones de Completar | 1 o más | 0 | **1** (sin distractores), **0** |

### 4.2 Tablas de decisión

**Clasificación de la respuesta del servidor** (`ClienteSubida.Clasificar`):

| Código HTTP | Resultado | Acción sobre la sesión |
|---|---|---|
| 200–299 | Subida | Se saca de la cola |
| 401 | NoAutorizado | Se conserva; se detiene el vaciado |
| 400, 403, 404, 409, 422 y otros 4xx | Rechazada | Se aparta; la cola sigue |
| 408, 429, 5xx | Reintentar | Se conserva; se detiene el vaciado |
| Sin red | Reintentar | Se conserva; se detiene el vaciado |
| 1xx, 3xx | Reintentar | Se conserva; se detiene el vaciado |

**Respuesta del niño en una ronda con pista** (`RondaDeOpciones.Responder`, umbral = 2 fallos):

| Respuesta correcta | Fallos acumulados ≥ umbral | Ronda ya resuelta | Resultado |
|---|---|---|---|
| Sí | — | — | Acierto |
| No | No | No | Fallo (suma un fallo) |
| No | Sí | No | FalloConPista (la correcta se destaca) |
| No | — | Sí | No suma fallos |

**Vaciado de la cola de sesiones:**

| Resultado del envío de la primera sesión | Acción |
|---|---|
| Subida | La saca y sigue con la siguiente |
| Rechazada o inválida | La aparta y sigue con la siguiente |
| Reintentar | Corta el vaciado y conserva todo |
| NoAutorizado | Corta el vaciado y conserva todo |

### 4.3 Cobertura de sentencia y de decisión

Medida con Coverlet sobre la lógica de los juegos: **99,2 % de líneas** (862 líneas) y **98 % de ramas** (257 de 262). Reporte HTML en el artefacto `reportes-calidad` de cada ejecución.

### 4.4 Plantilla de caso de prueba

| Campo | Contenido |
|---|---|
| ID | TC-XX |
| Requisito | RF-… / RT-… |
| Técnica | Equivalencia, valor límite, tabla de decisión, smoke |
| Precondición | Estado inicial |
| Pasos / entrada | Qué se ejecuta |
| Resultado esperado | Fijado **antes** de ejecutar |
| Resultado obtenido | Lo que pasó |
| Estado | Pasa / Falla |
| Defecto | Enlace a Jira si falla |

Las pruebas automáticas siguen el formato Arrange–Act–Assert, con el resultado esperado escrito en la aserción antes de correr.

## 5. Matriz ISO/IEC 25010

| Característica | Qué se verifica | Evidencia | Cobertura | Brecha |
|---|---|---|---|---|
| Adecuación funcional | Las reglas de cada juego y la subida de sesiones hacen lo que dice la especificación | 186 pruebas, 99,2 % de líneas, puntaje de mutación 91,79 % | **Alta** para la lógica | Encajar y las pistas de Completar no tienen prueba automática |
| Eficiencia de desempeño | Rendimiento y memoria de la lógica bajo carga | Stress: 5 escenarios, p99 máximo entre 38 µs (GitHub) y 253 µs (local); 60 s sostenidos con rendimiento estable (103 % del inicial en GitHub) y memoria sin crecimiento | **Media** | No mide FPS ni memoria en una tablet real |
| Compatibilidad | Interoperabilidad con el servicio externo; convivencia de entornos | Contrato de subida probado con dobles; la lógica corre en macOS y Linux | **Baja** | Sin prueba contra Supabase real ni con distintos dispositivos |
| Usabilidad (capacidad de uso y accesibilidad) | Reglas TEA: errores silenciosos, pausa, pistas suaves, sin límite de tiempo | Pausa y pistas probadas en lógica; resto en el smoke manual | **Baja** | Smoke manual sin ejecutar; sin pruebas con niños ni profesionales |
| Fiabilidad | Tolerancia a fallos de red, reintentos sin duplicar, sesión que no se pierde | `ColaDeSesionesTests`, `ClienteSubidaTests` (reintento con mismo cuerpo, corte ante falla), D-01 | **Media** | La cola vive en memoria: se pierde si la app se cierra |
| Seguridad | Autorización por token, validación de datos antes de enviar, acceso por fila en el esquema | Validación del contrato (42 pruebas); políticas del esquema verificadas en su momento contra un Postgres 16 en Docker | **Baja** en este pipeline | Las pruebas del esquema no están en el pipeline; sin pruebas contra Supabase real; datos de menores (Ley 25.326) |
| Mantenibilidad | Modularidad, duplicación, deuda, capacidad de probar | Lógica separada en `Logica/`, duplicación 3,28 %, 4 code smells, mutación, pipeline en 7 etapas | **Alta** | Los `MonoBehaviour` siguen concentrando lógica en Encajar |
| Portabilidad | Que el código compile y corra fuera de Unity y en otros sistemas | La lógica compila con .NET 8 en macOS y Linux (CI) | **Media** | Sin exportación probada a tablet |

### Las tres características prioritarias

1. **Usabilidad (accesibilidad para niños con TEA).** Es la razón de ser del proyecto: un error silencioso o una pausa que no congela puede generar frustración o ansiedad. Es también la característica con **menos evidencia hoy** (cobertura baja), así que es donde más conviene invertir el tiempo restante: ejecutar el smoke manual.
2. **Adecuación funcional.** Si las reglas de los juegos fallan, la app no sirve. Es la característica mejor cubierta (cobertura alta).
3. **Fiabilidad.** Las métricas de cada sesión son el insumo del panel de seguimiento de profesionales; perderlas invalida el seguimiento. Falta persistir la cola en disco.

## 6. Riesgos

Probabilidad (P) e impacto (I) de 1 a 5; prioridad = P × I.

| ID | Riesgo | P | I | Prioridad | Mitigación | Estado |
|---|---|---|---|---|---|---|
| R1 | El juego no se probó en Unity ni en una tablet: puede fallar algo que la lógica no ve (pausa en Difícil, pistas, escenas) | 4 | 4 | **16** | Ejecutar el smoke manual; D-01 cubierto por prueba de regla más verificación manual | Abierto |
| R2 | No hay pruebas con niños ni profesionales: se puede incumplir una adaptación TEA sin notarlo | 4 | 4 | **16** | Validar con las profesionales del equipo antes de declarar el módulo terminado | Abierto, fuera del TP |
| R3 | La cola de sesiones no persiste: se pierden sesiones si la app se cierra antes de subir | 3 | 3 | **9** | Guardar la cola en disco (siguiente paso del módulo de subida) | Abierto |
| R4 | Cobertura y mutación miden solo la lógica: los `MonoBehaviour` quedan sin medir | 4 | 3 | **12** | Mover lógica de Encajar a clases puras; mientras tanto, smoke manual | Abierto |
| R5 | El pipeline depende de versiones externas (SDK de .NET, Stryker, runner, GitHub Actions): ya falló dos veces (D-05, D-06) | 4 | 2 | **8** | `global.json` con SDK 8, runner fijado en `ubuntu-24.04`, versiones de herramientas fijadas en el manifiesto | Mitigado |
| R6 | Un valor de prueba llega al repositorio por error (por ejemplo `segundosParaPista: 10` en una escena) | 3 | 2 | **6** | Descartar las escenas antes de cada commit; revisar `git status` | Abierto |
| R7 | Supabase no está disponible (caída o proyecto pausado) o la integración real difiere del contrato | 3 | 3 | **9** | Cola con reintentos; contrato documentado; exportar la base antes de la defensa; probar contra un entorno real | Abierto |
| R8 | Exposición de datos de salud de menores (esquema mal configurado) | 2 | 5 | **10** | Políticas de acceso por fila; probar con cuentas de adulto, profesional y un tercero; minimizar los datos guardados | Abierto |
| R9 | Los umbrales del stress son flojos y no detectarían una degradación moderada | 3 | 2 | **6** | Ajustarlos con los números de varias ejecuciones de GitHub | Abierto |
| R10 | Plazo corto (entrega el 26/10) | 3 | 4 | **12** | Priorizar lo que pesa en la evaluación; automatizar todo lo posible | Abierto |

## 7. Prácticas de QA y de QC

| QA: prevenir (el proceso) | QC: detectar (el producto) |
|---|---|
| Gitflow con una rama por módulo | Pruebas unitarias y de integración |
| Pipeline en cada push, con quality gate que bloquea | Smoke test automático y manual |
| Criterios de entrada y salida escritos | Stress y esfuerzo sostenido |
| Plantilla de defecto y de caso de prueba | Pruebas de mutación |
| Registro de defectos con trazabilidad a casos y commits | Análisis estático y duplicación |
| Convención: la lógica va en `Logica/`, sin `UnityEngine` | Cobertura de sentencia y de decisión |
| Resultado esperado fijado antes de ejecutar | Verificación manual de pausa y pistas en Unity |
| Versiones de SDK y herramientas fijadas (`global.json`, manifiesto) | |

## 8. Métricas interpretadas

| Métrica | Valor | Umbral | Interpretación |
|---|---|---|---|
| Cobertura de líneas | 99,2 % (862 líneas) | ≥ 80 % | La lógica casi se ejecuta entera. Por sí sola no prueba eficacia. |
| Cobertura de ramas (decisión) | 98 % (257 de 262) | — | Quedan 5 decisiones sin ejecutar. |
| Puntaje de mutación | 91,79 % (492 de 536) | ≥ 85 % | Las pruebas detectan 9 de cada 10 defectos artificiales. Quedan 42 sin detectar, 22 en `SeguidorTrazo`. |
| Duplicación | 3,28 % (11 clones) | ≤ 5 % | Aceptable. Cerca del límite; vigilar al agregar código. |
| Hallazgos del análisis estático | 4 code smells, 0 bloqueantes | 0 bloqueantes | Deuda baja y estable. Los 4 son de estilo. |
| Latencia p99 de una partida | 38 µs (GitHub) a 253 µs (local), el peor escenario | 5 ms | Muy por debajo; el umbral es flojo. |
| Rendimiento sostenido (60 s) | 103 % del inicial | ≥ 60 % | No hay degradación. |
| Crecimiento de memoria (60 s) | ~0 MB | ≤ 20 MB | Sin fugas visibles en la lógica. |
| Defectos registrados | 6 (2 del producto, 4 del proceso), 0 abiertos | 0 Altos abiertos | El proceso encuentra y corrige; hay más defectos del proceso que del producto. |

**Métrica de proceso: dónde se detectan los defectos del pipeline.** De los 4 defectos del proceso, 2 se detectaron localmente antes del primer push (D-03, D-04) y 2 los detectó GitHub Actions al ejecutar (D-05, D-06). Interpretación: correr el pipeline en local ahorra ciclos, pero algunos problemas (versiones de SDK del runner) solo aparecen en el entorno real.

**Métrica de proyecto: puntaje de mutación por pruebas escritas.** Pasó de 77,43 % con 137 pruebas a 91,79 % con 186 pruebas: 49 pruebas dirigidas elevaron 14,4 puntos. Interpretación: las pruebas dirigidas por mutantes tienen mejor rendimiento que sumar pruebas sin guía (la cobertura ya estaba en 96,5 % y no lo mostraba).

**Mejora medida antes y después:** ver `quality/herramienta-nueva.md` sección 6.

**Pendiente de medir:** tiempo total del pipeline en GitHub (completar con la captura de Actions). Referencia: el job de mutación tarda 83 s.

## 9. Objetivos de calidad medibles

| Perspectiva | Objetivo | Cómo se mide | Meta | Estado |
|---|---|---|---|---|
| Usuario (niño con TEA) | La pausa congela el 100 % de temporizadores y movimientos | `ControlPausaTests` y smoke manual E7, M6, C5, L4, EM5 | 0 casos que avancen en pausa | Regla probada; verificación en juego pendiente |
| Usuario | Ningún error del niño produce sonido ni mensaje negativo | Smoke manual M3, E3, C2, L3, EM2 | 5 de 5 | Pendiente |
| Usuario / datos | Ninguna sesión se pierde por una falla de red | `ColaDeSesionesTests` | 0 sesiones perdidas con la app abierta | Cumplido; falla si la app se cierra (R3) |
| Equipo | Cobertura y eficacia de las pruebas | Cobertura de líneas y puntaje de mutación | ≥ 80 % y ≥ 85 % | 99,2 % y 91,79 % |
| Equipo | Feedback rápido del pipeline | Duración de la ejecución en Actions | ≤ 5 minutos | Por medir |
| Organización (cátedra) | Todo defecto con registro completo y prueba de regresión | Registro de defectos | 100 % | 6 de 6 en el repositorio; carga en Jira pendiente |

**Prácticas de proceso que sostienen los objetivos:** pipeline en cada push, quality gate bloqueante, registro de defectos con trazabilidad, convención de lógica pura.

## 10. Plan de contingencia ante la caída de una dependencia

| Dependencia crítica | Si cae | Plan |
|---|---|---|
| Supabase (o plan gratuito pausado) | No se pueden subir sesiones | La app sigue funcionando: las sesiones esperan en la cola y se reintentan. Exportar la base antes de la defensa. Demo con el doble de prueba si hace falta |
| GitHub Actions | No corre el pipeline | `bash quality/run-local.sh` ejecuta las mismas 7 etapas en local y genera los reportes |
| Licencia o editor de Unity | No se pueden correr pruebas en Unity | Las pruebas corren con `dotnet test`, sin Unity |
| Jira | No se pueden consultar los defectos | Respaldo en `quality/registro-defectos.md` |
| Versiones de SDK, Stryker o runner | El pipeline falla (ya ocurrió: D-06) | Versiones fijadas en `global.json` y en el manifiesto de herramientas; el log completo se muestra al fallar |
| Paquetes externos (NuGet, npm para jscpd) | No se pueden restaurar | Reintentar; si persiste, ejecutar en local con paquetes ya descargados |

## 11. Nivel declarado y metas

Contraste con la tabla de la consigna (la decisión del nivel es del grupo):

| Aspecto | Nivel 1 | Nivel 2 | Estado |
|---|---|---|---|
| Pipeline: compilación, pruebas, análisis estático, cobertura, quality gate y reporte | Sí | Sí | **Cumplido** (7 etapas) |
| Quality gate: bugs críticos, duplicación > 5 %, cobertura | Sí | Sí | **Cumplido** |
| Plan de pruebas y riesgos | Sí | Sí | **Cumplido** (este documento) |
| Defectos registrados en Jira u otra herramienta | Sí | Sí | **Parcial**: registro en el repositorio; carga en Jira pendiente |
| Herramienta nueva con proceso de cinco pasos | Sí | Sí | **Cumplido** (Stryker.NET) |
| Cobertura mínima sobre el código existente | 50 % | 60 % | **Superado** (99,2 %) |
| Smoke test | Definido y ejecutable a mano | Ejecutado por el pipeline | **Cumplido**: automático en el pipeline; manual definido |
| Integración externa | Contrato y dobles de prueba | Probada contra un entorno real | **Nivel 1**: sin prueba contra Supabase real |
| Stress y esfuerzo sostenido | Plan y script ejecutado | Ejecutado con resultados y umbrales | **Nivel 2**: ejecutado, con umbrales |

**Propuesta:** declarar el **nivel 1**, destacando que dos metas (smoke por el pipeline y stress con umbrales) cumplen el nivel 2. La integración contra un entorno real es lo que falta para el nivel 2.

## 12. Pendientes

1. Ejecutar el smoke manual en Unity y registrar resultados y capturas.
2. Cargar los defectos y los casos de prueba en Jira.
3. Persistir la cola de sesiones en disco (R3).
4. Probar la integración contra un Supabase real (nivel 2).
5. Ajustar los umbrales del stress con más ejecuciones (R9).
6. Medir el tiempo total del pipeline en Actions.
7. Mover la lógica de Encajar a clases puras (R4).
8. Validar con las profesionales del equipo las adaptaciones TEA (R2).
