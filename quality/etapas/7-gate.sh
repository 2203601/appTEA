#!/usr/bin/env bash
# Etapa 7: quality gate. Lee los reportes de out/ y rechaza si no se cumplen los umbrales.
source "$(dirname "$0")/comun.sh"
echo "==> Etapa 7: quality gate"
python3 "$QUALITY/gate.py"
