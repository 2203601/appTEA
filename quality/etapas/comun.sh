# Variables compartidas por las etapas del pipeline (se carga con `source`).
set -uo pipefail
QUALITY="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REPO="$(cd "$QUALITY/.." && pwd)"
SCRIPTS="$REPO/My project/Assets/_Scripts"
OUT="$QUALITY/out"
mkdir -p "$OUT"
cd "$QUALITY"
