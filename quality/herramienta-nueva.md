# Herramienta nueva: pruebas de mutación con Stryker.NET

Versión 1 · incorporada con el proceso de cinco pasos (requisitos, mercado, prueba de concepto, matriz de decisión, despliegue incremental).

## Resumen

| | |
|---|---|
| Necesidad | Saber si las pruebas **detectan defectos**, no solo si ejecutan el código |
| Herramienta elegida | Stryker.NET 4.8.1 (pruebas de mutación para C#) |
| Medida inicial | Puntaje de mutación **77,43 %** con 96,5 % de cobertura de líneas |
| Medida final | Puntaje de mutación **91,79 %** con 99,2 % de cobertura de líneas |
| Dónde corre | Local (`bash quality/run-local.sh`) y job 6 del pipeline, con umbral en el quality gate |

## 1. Requisitos

El problema: el pipeline mide **cobertura** (96,5 %), pero la cobertura solo dice qué líneas se ejecutaron, no si una prueba fallaría ante un error. Una prueba sin aserciones daría cobertura y no detectaría nada. Hacía falta una medida de la **eficacia** de las pruebas.

| # | Requisito | Por qué |
|---|---|---|
| R1 | Medir si las pruebas detectan defectos (no solo cobertura) | Es la necesidad |
| R2 | Funciona con C# / .NET 8 sobre la lógica de los juegos | La lógica se compila fuera de Unity (`Juegos.Logica`) |
| R3 | Corre en GitHub Actions sin licencia de Unity | Restricción del pipeline actual |
| R4 | Gratuita y de código abierto | Proyecto de tesis, sin presupuesto |
| R5 | Reporte legible (HTML) y automatizable (JSON) para el quality gate | El gate decide con números |
| R6 | Tiempo razonable (pocos minutos) | Si es lenta, nadie la corre |
| R7 | Funciona en macOS (arm64, equipos del grupo) y Linux (CI) | Se usa en los dos |

## 2. Mercado

| Alternativa | Qué es | Comentario |
|---|---|---|
| **Stryker.NET** | Herramienta de mutación para C#. Open source, se instala como `dotnet tool` | La opción estándar del ecosistema .NET |
| Mutaciones manuales | Cambiar a mano un operador, correr las pruebas, restaurar (lo que se hizo antes con `sed`) | No requiere herramienta, pero no escala ni da reporte |
| Subir el umbral de cobertura | Exigir más del 80 % actual | No requiere herramienta, pero no responde a la necesidad |

Se evaluaron también k6 y BenchmarkDotNet, pero cubren otra necesidad (carga contra una API y microbenchmarks). Se descartan para esta decisión. k6 queda como candidata para cuando exista la integración con un servidor real.

Para C# la oferta de herramientas de mutación es acotada; fuera del ecosistema .NET (PIT en Java, Cosmic Ray en Python) no aplican a este código.

## 3. Prueba de concepto (ejecutada)

Se ejecutó sobre `Juegos.Logica` con las 137 pruebas que había entonces.

| Intento | Resultado |
|---|---|
| Instalar la última versión (`dotnet tool install dotnet-stryker`) | Falló: *Settings file 'DotnetToolSettings.xml' was not found in the package* |
| Versión 4.4.0 | Instaló, pero falló al ejecutar en macOS arm64 (*Sequence contains no matching element* al desplegar los binarios de VSTest) |
| **Versión 4.8.1** | **Funciona.** 695 mutantes creados, 506 evaluados, 40 s |

Resultado de la prueba de concepto: puntaje **77,43 %** (412 mutantes detectados, 91 sobrevivieron, 3 por timeout y 30 sin cobertura). La versión quedó fijada en `quality/.config/dotnet-tools.json`. Existe una 5.0.0 que no se probó.

Lo que reveló: con 96,5 % de cobertura, 91 cambios al código no hacían fallar ninguna prueba. Los más débiles eran `SeguidorTrazo` (54 %), `ContratoSubida` (77 %) y `GeneradorHuecos` (79 %).

## 4. Matriz de decisión

Puntajes de 1 (malo) a 5 (muy bueno), propuestos a partir de la prueba de concepto y **pendientes de validar con el grupo**. Ponderación según la importancia del criterio.

| Criterio | Peso | Stryker.NET | Mutaciones manuales | Subir umbral de cobertura |
|---|---|---|---|---|
| Responde a la necesidad (R1) | 5 | 5 | 3 | 1 |
| Integración con el CI (R3, R5) | 4 | 4 | 2 | 5 |
| Esfuerzo de adopción | 3 | 4 | 2 | 5 |
| Costo (R4) | 3 | 5 | 5 | 5 |
| Mantenimiento y comunidad | 2 | 4 | 1 | 5 |
| Calidad del reporte (R5) | 3 | 5 | 1 | 3 |
| **Total ponderado (máx. 100)** | 20 | **91** | 49 | 74 |

Decisión: **Stryker.NET**. Es la única que responde a la necesidad con reporte automatizable. Subir el umbral de cobertura queda segunda, pero no mide eficacia.

Los puntajes son una primera valoración; el grupo tiene que revisarlos y ajustarlos antes de la entrega.

## 5. Despliegue incremental

| Fase | Qué se hizo | Estado |
|---|---|---|
| 1. Local, solo medir | Instalada como herramienta local (`dotnet tool`), configuración en `quality/Juegos.Tests/stryker-config.json`. Se midió sin bloquear nada | Hecha |
| 2. Mejorar con lo que muestra | Se escribieron 49 pruebas dirigidas a los mutantes que sobrevivían (`SeguidorTrazoLimitesTests`, `GeneradorHuecosLimitesTests`, `ContratoSubidaMensajesTests`) | Hecha |
| 3. Pipeline con umbral | Job 6 del CI y fila en el quality gate con umbral de 85 % | Hecha en el repositorio; falta ver la primera ejecución en GitHub |
| 4. Endurecer | Subir el umbral de a poco y, si el tiempo crece, mutar solo el código cambiado (`--since`) | Pendiente |

## 6. Mejora medida (antes y después)

| Medida | Antes | Después |
|---|---|---|
| Puntaje de mutación | 77,43 % | **91,79 %** |
| Mutantes que sobreviven | 91 | 42 |
| Mutantes sin cobertura | 30 | 2 |
| Pruebas | 137 | 186 |
| Cobertura de líneas | 96,5 % | 99,2 % |
| Cobertura de ramas | 91,6 % | 98,1 % |

Por archivo:

| Archivo | Antes | Después |
|---|---|---|
| `SeguidorTrazo.cs` | 54,4 % | 80,7 % |
| `ContratoSubida.cs` | 76,7 % | 98,3 % |
| `GeneradorHuecos.cs` | 79,1 % | 90,7 % |
| `TableroMemoria.cs`, `RondaDeOpciones.cs`, `RondaCompletar.cs` | 91,2 % / 88,9 % / 80,0 % | sin cambios |
| `ClienteSubida.cs`, `ColaDeSesiones.cs`, `ControlPausa.cs`, `TemporizadorInactividad.cs` | 100 % | 100 % |

## 7. Límites y riesgos

- **Mutantes equivalentes:** algunos cambios no alteran el comportamiento (por ejemplo, los bucles de mezcla `i >= 0` en vez de `i > 0`). Por eso no se apunta al 100 %.
- **Quedan 42 mutantes sin detectar**, 22 de ellos en `SeguidorTrazo` (geometría). Es la mejora siguiente.
- **Tiempo:** suma unos 40 s al pipeline (job aparte, en paralelo con el stress).
- **Alcance:** solo mide la lógica de `Logica/`, no los `MonoBehaviour` ni las escenas.
- **Versión fijada:** la 4.8.1 funcionó en macOS arm64 y está fijada en el manifiesto. Una actualización puede requerir otra prueba de concepto.
- **Falsa seguridad:** un puntaje alto no garantiza ausencia de defectos; mide cuántos cambios artificiales detectan las pruebas.
