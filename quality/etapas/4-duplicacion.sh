#!/usr/bin/env bash
# Etapa 4: duplicación de código (jscpd). Genera out/jscpd/
source "$(dirname "$0")/comun.sh"
echo "==> Etapa 4: duplicación de código"
npx -y jscpd@4 --format csharp --min-lines 5 --min-tokens 50 \
    --ignore "**/Editor/**,**/Tests/**" --reporters json --output "$OUT/jscpd" \
    "$SCRIPTS"/Juego* "$SCRIPTS/Comun" >/dev/null 2>&1
test -f "$OUT/jscpd/jscpd-report.json" && echo "Reporte: out/jscpd/jscpd-report.json" || { echo "jscpd no generó reporte"; exit 1; }
