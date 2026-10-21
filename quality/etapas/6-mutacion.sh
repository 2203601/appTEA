#!/usr/bin/env bash
# Etapa 6: pruebas de mutación con Stryker.NET. Genera out/mutacion/reports/ (HTML y JSON).
# Mide si las pruebas DETECTAN defectos: cambia el código de a un operador por vez (un "mutante") y comprueba
# que alguna prueba falle. Puntaje = mutantes detectados / mutantes evaluados. Umbral en el quality gate.
source "$(dirname "$0")/comun.sh"
echo "==> Etapa 6: pruebas de mutación"
dotnet tool restore >/dev/null || exit 1
cd "$QUALITY/Juegos.Tests"
dotnet tool run dotnet-stryker --config-file stryker-config.json -O "$OUT/mutacion" 2>&1 \
    | grep -E "mutants created|total mutants will be tested|Killed|Survived|Timeout|final mutation score|ERR|failed"
test -f "$OUT/mutacion/reports/mutation-report.json" || { echo "Stryker no generó reporte"; exit 1; }
