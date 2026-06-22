#!/usr/bin/env bash
# Lanza el cliente de Taskbar Tamer con Godot .NET 4.6.
# Uso:
#   export GODOT="/ruta/Godot_v4.6.x-stable_mono"; ./scripts/run.sh
#   ./scripts/run.sh /ruta/Godot_...mono
set -euo pipefail

GODOT="${GODOT:-${1:-}}"
if [ -z "$GODOT" ]; then
  echo "Define GODOT con la ruta al ejecutable de Godot .NET, o pasala como argumento." >&2
  echo "Descarga la edicion .NET en https://godotengine.org/download" >&2
  exit 1
fi

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
"$GODOT" --path "$DIR/game"
