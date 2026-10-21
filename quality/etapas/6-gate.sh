#!/usr/bin/env bash
# Etapa 6: quality gate. Lee los reportes de out/ y rechaza si no se cumplen los umbrales.
source "$(dirname "$0")/comun.sh"
echo "==> Etapa 6: quality gate"
python3 "$QUALITY/gate.py"
