#!/usr/bin/env bash
# Etapa 3: pruebas unitarias completas + cobertura. Genera out/tests/ y out/cobertura/
source "$(dirname "$0")/comun.sh"
echo "==> Etapa 3: pruebas unitarias + cobertura"
dotnet tool restore >/dev/null || exit 1
dotnet restore Juegos.Tests/Juegos.Tests.csproj >/dev/null || exit 1
dotnet test Juegos.Tests/Juegos.Tests.csproj -c Release --no-restore \
    --collect:"XPlat Code Coverage" --results-directory "$OUT/tests" \
    --logger "trx;LogFileName=resultados.trx" | tail -n 8
rc="${PIPESTATUS[0]}"
dotnet tool run reportgenerator -reports:"$OUT/tests/**/coverage.cobertura.xml" -targetdir:"$OUT/cobertura" \
    -reporttypes:"Html;TextSummary;MarkdownSummaryGithub" -verbosity:Warning >/dev/null
exit "$rc"
