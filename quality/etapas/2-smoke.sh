#!/usr/bin/env bash
# Etapa 2: smoke test automático (pruebas marcadas Category=Smoke). Falla rápido. Genera out/smoke/
source "$(dirname "$0")/comun.sh"
echo "==> Etapa 2: smoke test automático"
dotnet restore Juegos.Tests/Juegos.Tests.csproj >/dev/null || exit 1
dotnet test Juegos.Tests/Juegos.Tests.csproj -c Release --no-restore --filter "Category=Smoke" \
    --results-directory "$OUT/smoke" --logger "trx;LogFileName=smoke.trx" | tail -n 4
exit "${PIPESTATUS[0]}"
