#!/usr/bin/env bash
# Pipeline de calidad completo en un solo comando (lo mismo que corre GitHub Actions):
#   bash quality/run-local.sh
# Requisitos: .NET SDK 8, Node 18+ (para jscpd) y Python 3.
set -uo pipefail
cd "$(dirname "$0")"
REPO="$(cd .. && pwd)"
SCRIPTS="$REPO/My project/Assets/_Scripts"

rm -rf out && mkdir -p out

echo "==> 1/6 Restaurar herramientas y paquetes"
dotnet tool restore >/dev/null || exit 1
dotnet restore Juegos.Tests/Juegos.Tests.csproj >/dev/null || exit 1

echo "==> 2/6 Compilar + análisis estático (SonarAnalyzer)"
dotnet build Juegos.Logica/Juegos.Logica.csproj -c Release --no-restore --no-incremental -p:ErrorLog="$PWD/out/analisis.sarif" >/dev/null || { echo "Falló la compilación"; exit 1; }

echo "==> 3/6 Smoke test automático (pruebas marcadas Category=Smoke)"
dotnet test Juegos.Tests/Juegos.Tests.csproj -c Release --no-restore --filter "Category=Smoke" \
    --results-directory out/smoke --logger "trx;LogFileName=smoke.trx" | tail -n 4

echo "==> 4/6 Pruebas unitarias + cobertura"
dotnet test Juegos.Tests/Juegos.Tests.csproj -c Release --no-restore \
    --collect:"XPlat Code Coverage" --results-directory out/tests \
    --logger "trx;LogFileName=resultados.trx" | tail -n 8
dotnet tool run reportgenerator -reports:"out/tests/**/coverage.cobertura.xml" -targetdir:out/cobertura \
    -reporttypes:"Html;TextSummary;MarkdownSummaryGithub" -verbosity:Warning >/dev/null

echo "==> 5/6 Duplicación de código (jscpd)"
npx -y jscpd@4 --format csharp --min-lines 5 --min-tokens 50 \
    --ignore "**/Editor/**,**/Tests/**" --reporters json --output out/jscpd \
    "$SCRIPTS"/Juego* "$SCRIPTS/Comun" >/dev/null 2>&1

echo "==> 6/6 Quality gate"
python3 gate.py
