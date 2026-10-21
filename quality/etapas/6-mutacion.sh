#!/usr/bin/env bash
# Etapa 6: pruebas de mutación con Stryker.NET. Genera out/mutacion/reports/ (HTML y JSON).
# Mide si las pruebas DETECTAN defectos: cambia el código de a un operador por vez (un "mutante") y comprueba
# que alguna prueba falle. Puntaje = mutantes detectados / mutantes evaluados. Umbral en el quality gate.
source "$(dirname "$0")/comun.sh"
echo "==> Etapa 6: pruebas de mutación"
dotnet tool restore >/dev/null || exit 1
cd "$QUALITY/Juegos.Tests"
mkdir -p "$OUT/mutacion"
dotnet tool run dotnet-stryker --config-file stryker-config.json -O "$OUT/mutacion" > "$OUT/mutacion/stryker.log" 2>&1
rc=$?

# resumen
grep -E "mutants created|total mutants will be tested|Killed|Survived|Timeout|final mutation score" "$OUT/mutacion/stryker.log"

if [ ! -f "$OUT/mutacion/reports/mutation-report.json" ]; then
    echo "Stryker no generó reporte (código de salida $rc). Registro completo:"
    echo "--- dotnet --info ---"; dotnet --list-sdks; dotnet --list-runtimes
    echo "--- stryker.log ---"; cat "$OUT/mutacion/stryker.log"
    exit 1
fi
