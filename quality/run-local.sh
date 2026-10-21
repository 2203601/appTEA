#!/usr/bin/env bash
# Pipeline de calidad completo en un solo comando. GitHub Actions corre las mismas etapas como jobs separados.
#   bash quality/run-local.sh
# Requisitos: .NET SDK 8, Node 18+ (para jscpd) y Python 3.
cd "$(dirname "$0")"
rm -rf out && mkdir -p out

bash etapas/1-compilar.sh || exit 1
bash etapas/2-smoke.sh
bash etapas/3-pruebas.sh
bash etapas/4-duplicacion.sh
bash etapas/5-gate.sh
