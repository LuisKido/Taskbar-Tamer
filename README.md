# Taskbar Tamer

**Idle / Auto-Battler Táctico de Colección de Criaturas** para PC (Steam), operando como una app compacta de bajo consumo que el usuario coloca donde quiera en su escritorio.

Gestionas un equipo de criaturas que farmean en segundo plano mientras usas tu PC. Recolectas **mutaciones genéticas y partes biológicas** para equipar a tus bestias, armas la composición perfecta y escalas en ligas competitivas de batallas automatizadas asíncronas.

---

## Estado del proyecto

🟢 **Prototipo jugable.** El núcleo (`core/`, C# puro) está completo y testeado, y el cliente Godot (`game/`) ya es jugable: arena en vivo (auto-battler con habilidades, jefes, mapas, efectos), idle/farming con progreso offline, inventario + fusión, colección/desbloqueo de criaturas, formación, equipamiento y opciones. Arte pixel-art generado por código.

## Stack

- **Motor:** Godot **4.6** — edición **.NET / Mono** (ejecuta C#).
- **Lenguaje:** C# (.NET 8). El núcleo de simulación (`core/`) es C# puro sin dependencias de Godot.
- **Plataforma objetivo:** Windows (Steam).

---

## Requisitos

| Herramienta | Versión | Para qué |
|-------------|---------|----------|
| **.NET SDK** | **8.0 o superior** | Compilar/testear `core/` y `tests/`, y compilar el cliente. |
| **Godot Engine — .NET** | **4.6.x** (build "mono"/.NET) | Abrir y ejecutar el cliente `game/`. Descárgalo en [godotengine.org/download](https://godotengine.org/download) (elige la edición **.NET**). |
| **git** | cualquiera | Clonar el repo. |

> ⚠️ Debe ser la edición **.NET** de Godot (la build estándar NO ejecuta C#). La primera vez, Godot descarga `Godot.NET.Sdk` desde NuGet → necesita conexión a internet.

---

## Cómo ejecutar

### 1. Clonar

```bash
git clone <url-del-repo>
cd "Taskbar Tamer"
```

### 2. Compilar y testear el núcleo (solo necesita el .NET SDK)

```bash
dotnet test
```

Esto compila `core/` (la lógica del juego) y corre la batería de tests. Útil para verificar el entorno sin Godot.

### 3. Ejecutar el juego (necesita Godot .NET)

**Opción A — Editor de Godot (recomendado la primera vez):**

1. Abre el ejecutable de **Godot .NET 4.6**.
2. **Import** → selecciona `game/project.godot`.
3. Godot importa y compila el C# (la primera vez tarda; descarga NuGet). Pulsa **▶ (F5)** para jugar.

**Opción B — Línea de comandos:**

Define la ruta a tu ejecutable de Godot y usa los scripts:

```powershell
# Windows (PowerShell)
$env:GODOT = "C:\ruta\a\Godot_v4.6.x-stable_mono_win64.exe"
./scripts/run.ps1
```

```bash
# Linux / macOS / Git Bash
export GODOT="/ruta/a/Godot_v4.6.x-stable_mono"
./scripts/run.sh
```

O directamente: `<godot> --path game`.

> Build sin abrir el juego: `<godot> --headless --path game --build-solutions --quit`.
> Verificación fiable de compilación del cliente: `dotnet build game/TaskbarTamer.Game.csproj`.

### Dónde se guarda la partida

El save vive en la carpeta `user://` de Godot:

- **Windows:** `%APPDATA%\Godot\app_userdata\Taskbar Tamer\savegame.json`
- **Linux:** `~/.local/share/godot/app_userdata/Taskbar Tamer/savegame.json`
- **macOS:** `~/Library/Application Support/Godot/app_userdata/Taskbar Tamer/savegame.json`

Borra ese archivo para empezar una partida limpia.

---

## Estructura del repo

```
core/    Lógica del juego en C# PURO (sin Godot): simulación, idle, modelo, persistencia.
game/    Cliente Godot 4.6 (.NET). Referencia a core/. UI, arena, sprites por código.
tests/   Pruebas xUnit sobre core/ (deterministas).
docs/    Diseño: GDD, arquitectura, modelo de datos, roadmap, economía.
scripts/ Ayudas para ejecutar (run.ps1 / run.sh).
```

`game/` y `core/` dependen de `core/`, nunca al revés: la misma simulación corre en cliente y (futuro) servidor.

## Documentación

| Doc | Contenido |
|-----|-----------|
| [01 — Documento de diseño](docs/01-documento-diseno.md) | El "qué": pitch, bucle de juego, sistemas, competitivo |
| [02 — Arquitectura técnica](docs/02-arquitectura-tecnica.md) | El "cómo": módulos, determinismo, widget, Steam, servidor |
| [03 — Modelo de datos](docs/03-modelo-datos.md) | Esquemas: criaturas, partes, sets, estadísticas, rarezas |
| [04 — Roadmap](docs/04-roadmap.md) | Fases de desarrollo y orden de construcción |
| [05 — Ideas / evolución](docs/05-ideas-evolucion.md) | Backlog de mecánicas de evolución (tema Digimon) |
| [06 — Economía / mercado](docs/06-economia-mercado.md) | Drops por semilla, escasez, y Steam Market sano |

## Principio rector

> **Bajo consumo.** El juego corre 24/7 como una ventana compacta que el usuario arrastra y coloca donde quiera (esquina, segundo monitor, etc.). Cada decisión técnica se mide contra el coste de RAM/CPU en estado inactivo. El farming idle NO simula combate en tiempo real: acumula tiempo y resuelve por tablas de botín.
