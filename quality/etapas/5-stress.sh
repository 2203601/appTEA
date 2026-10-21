#!/usr/bin/env bash
# Etapa 5: stress y esfuerzo sostenido sobre la lógica de los juegos. Genera out/stress/resultados.json
# Duración del esfuerzo sostenido: STRESS_SEGUNDOS (por defecto 60). Umbrales: ver Juegos.Stress/Program.cs
source "$(dirname "$0")/comun.sh"
echo "==> Etapa 5: stress y esfuerzo sostenido"
dotnet run -c Release --project Juegos.Stress -- "$OUT/stress" 2>&1 | grep -v "warning"
exit "${PIPESTATUS[0]}"
