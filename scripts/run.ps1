# Lanza el cliente de Taskbar Tamer con Godot .NET 4.6.
# Uso:
#   $env:GODOT = "C:\ruta\Godot_v4.6.x-stable_mono_win64.exe"; ./scripts/run.ps1
#   ./scripts/run.ps1 -Godot "C:\ruta\Godot_...mono...exe"
param([string]$Godot = $env:GODOT)

if (-not $Godot) {
    Write-Error "Define la ruta al ejecutable de Godot .NET en la variable GODOT, o pasala con -Godot. Descarga la edicion .NET en https://godotengine.org/download"
    exit 1
}
if (-not (Test-Path $Godot)) {
    Write-Error "No existe el ejecutable de Godot en: $Godot"
    exit 1
}

$proj = Join-Path $PSScriptRoot ".." | Join-Path -ChildPath "game"
& $Godot --path $proj
