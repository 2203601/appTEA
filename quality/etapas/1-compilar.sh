#!/usr/bin/env bash
# Etapa 1: compilación + análisis estático (SonarAnalyzer). Genera out/analisis.sarif
source "$(dirname "$0")/comun.sh"
echo "==> Etapa 1: compilación + análisis estático"
dotnet restore Juegos.Logica/Juegos.Logica.csproj >/dev/null || exit 1
dotnet build Juegos.Logica/Juegos.Logica.csproj -c Release --no-restore --no-incremental \
    -p:ErrorLog="$OUT/analisis.sarif" >/dev/null || { echo "Falló la compilación"; exit 1; }
echo "Compilación OK. Análisis estático: out/analisis.sarif"
