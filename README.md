# appTEA

App para tablet (Unity 2D, C#) con juegos para niños de 6 a 10 años con TEA. Proyecto final de Ingeniería en Sistemas (UCC).

## Juegos

Memoria, Encajar piezas, Completar palabras, Escribir siguiendo la línea e Identificar emociones. Todos tienen pausa, errores silenciosos y pistas suaves.

## Estructura

| Carpeta | Contenido |
|---|---|
| `My project/` | Proyecto de Unity |
| `My project/Assets/_Scripts/` | Scripts de cada juego y código común (`Comun/`) |
| `quality/` | Pipeline de calidad, pruebas de stress y documentos del TP |
| `.github/workflows/ci.yml` | Integración continua en GitHub Actions |

La lógica de los juegos vive en carpetas `Logica/` y no usa `UnityEngine`. Eso permite probarla y medir cobertura fuera de Unity. Las pruebas están en carpetas `Tests/EditMode/` junto a cada juego.

## Calidad: un solo comando

Requisitos: .NET SDK 8, Node 18 o superior y Python 3.

```bash
bash quality/run-local.sh
```

Corre las mismas etapas que GitHub Actions y deja los reportes en `quality/out/`:

| Etapa | Qué hace | Salida |
|---|---|---|
| 1. Compilación y análisis estático | Compila la lógica y analiza con SonarAnalyzer | `analisis.sarif` |
| 2. Smoke test automático | Pruebas marcadas `Category("Smoke")` | `smoke/` |
| 3. Pruebas y cobertura | 186 pruebas NUnit con cobertura (Coverlet) | `tests/`, `cobertura/index.html` |
| 4. Duplicación | jscpd sobre el código de los juegos | `jscpd/` |
| 5. Stress y esfuerzo sostenido | Partidas completas en paralelo y durante 60 s | `stress/resultados.json` |
| 6. Pruebas de mutación | Stryker.NET | `mutacion/reports/` |
| 7. Quality gate | Decide con los umbrales | `resumen.md` |

Cada etapa se puede correr sola: `bash quality/etapas/<n>-<nombre>.sh`.

### Umbrales del quality gate

| Chequeo | Umbral |
|---|---|
| Smoke y pruebas | 0 fallos |
| Cobertura de líneas de la lógica de los juegos | ≥ 80 % |
| Duplicación de código | ≤ 5 % |
| Análisis estático | 0 bugs o vulnerabilidades Blocker o Critical |
| Stress y esfuerzo sostenido | 0 fallas de latencia, degradación o memoria |
| Puntaje de mutación | ≥ 85 % |

Se pueden ajustar con variables de entorno (por ejemplo `MIN_COBERTURA`, `MIN_MUTACION`, `STRESS_SEGUNDOS`).

### Integración continua

`.github/workflows/ci.yml` corre cada etapa como un job y publica los reportes como artefacto `reportes-calidad`. Se ejecuta con cada push a `feature/tpcalidad`. Un commit con `[demo-fallo]` en el mensaje rompe una regla a propósito para demostrar que el gate rechaza.

Las pruebas también se pueden correr desde Unity: *Window → General → Test Runner → EditMode*.

## Documentos del TP de Calidad

| Documento | Contenido |
|---|---|
| `quality/plan-de-pruebas.md` | Plan, criterios de entrada y salida, trazabilidad, matriz ISO 25010, riesgos y métricas |
| `quality/registro-defectos.md` | Defectos con pasos, esperado, obtenido y casos de prueba |
| `quality/smoke-test.md` | Smoke automático y smoke manual |
| `quality/integracion-externa.md` | Contrato de subida de sesiones y sus pruebas |
| `quality/herramienta-nueva.md` | Incorporación de Stryker.NET con el proceso de cinco pasos |
