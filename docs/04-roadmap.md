# 04 — Roadmap

> Orden de construcción por fases. Cada fase produce algo **probable** y reduce riesgo antes de la siguiente. El núcleo (`core/`) se construye headless y testeado antes de tocar UI.

## Fase 0 — Fundaciones (esqueleto)
**Meta:** repo y solución listos para programar.
- [x] Solución C# (`TaskbarTamer.sln`) con `core/` (lib) y `tests/` (xUnit). `core/` compila sin Godot.
- [x] `.gitignore` (Godot + .NET), repo git inicializado, build + test verdes (2/2).
- [ ] Proyecto Godot 4 (`game/`) — **diferido** hasta instalar Godot .NET; se crea desde el editor (genera sus `.csproj`).
- [ ] Spike de consumo: **ventana compacta movible + medición de RAM/CPU en idle** (ver [arquitectura §5](02-arquitectura-tecnica.md#5-ventana-compacta-movible-y-consumo)). Requiere Godot instalado. *(Riesgo bajado: la ventana movible es nativa en Godot.)*

## Fase 1 — Núcleo de simulación (determinista)
**Meta:** `Simular(setupA, setupB, semilla)` → log de eventos reproducible. *El corazón del juego.*
- [x] Modelo: `Creature`, `Setup` (frontal/retaguardia), `SetDefinition`/`SetRegistry`, `CombatKeyword`.
- [x] `StatsResolver`: stats efectivas = innato + partes + bonos de set (determinista, conmutativo).
- [x] `DeterministicRng` sembrado (de Fase 2).
- [x] `BattleSimulator`: orden por SPD (desempate total), targeting frontal→retaguardia, daño con mitigación por defensa, críticos, evasión.
- [x] Sinergias de set + estado Veneno (keyword `ApplyPoisonOnHit`, tick por ronda).
- [x] Log de eventos reproducible (`BattleEvent`) + desenlace con tope de rondas y desempate por HP.
- [x] `PowerRating`: convierte un `Setup` en el `TeamPower` (int) que consume el idle.
- [x] Tests: determinismo (mismo input → log idéntico), fórmula de daño, crit/evasión, targeting, veneno, sets (10 tests).

## Fase 2 — Loop idle + inventario
**Meta:** farming por tiempo que genera loot real.
- [x] Modelo mínimo de partes: `Stats`, `AnatomySlot`, `Part`, `PartFactory` (stats escaladas por rareza).
- [x] `DeterministicRng` (splitmix64) reanudable por estado.
- [x] Biomas + tablas de loot ponderadas; gate por poder de equipo.
- [x] `FarmingSimulator`: resolución por delta de tiempo + tope de progreso offline (función pura, determinista).
- [x] `Inventory` + fusión (3 iguales → rareza superior) con cascada (`AutoFuse`).
- [x] 27 tests verdes (determinismo, pesos, reanudación por tramos, cascada de fusión).
- [ ] Persistencia local (`user://`) — **pendiente**: requiere el cliente Godot (la serialización del estado se puede preparar en `core/` antes).

> Nota: se abstrae la fuerza del equipo como `TeamPower` (int). El cálculo real de `TeamPower` a partir de criaturas+stats llega con la Fase 1 (simulador).

## Fase 3 — Fase activa (UI)
**Meta:** el jugador gestiona y ve batallas.
- [ ] Panel principal: equipo, inventario, equipar partes.
- [ ] Reproductor de batalla: anima el log de eventos del simulador (no recalcula).
- [ ] Editor de formación (posicionamiento frontal/retaguardia).
- [ ] Widget de barra de tareas funcional (resultado del spike de Fase 0).

## Fase 4 — Crianza y progresión a largo plazo
**Meta:** el bucle de optimización a lo largo del tiempo.
- [ ] Límite de nivel + retiro de campeones.
- [ ] Herencia genética (transmitir rasgo/equipo).
- [ ] Balance de progresión.

## Fase 5 — Competitivo (servidor)
**Meta:** ranked asíncrono funcionando.
- [ ] Servidor .NET headless reusando `core/`.
- [ ] Bloqueo de setup, emparejamiento por liga, simulación asíncrona, MMR.
- [ ] Anti-cheat (servidor recalcula, valida propiedad de partes).
- [ ] Temporadas con reglas variables.

## Fase 6 — Steam y economía
**Meta:** lanzamiento.
- [ ] Integración Steamworks: logros, leaderboards, Steam Cloud.
- [ ] Steam Community Market (Steam Inventory Service) — partes como ítems.
- [ ] Pulido, onboarding, telemetría de balance.

---

## Riesgos priorizados

| Riesgo | Fase | Mitigación |
|--------|------|-----------|
| ~~Widget de taskbar/tray no nativo~~ → ventana movible | 0 | **Resuelto por diseño:** app es una ventana compacta movible (nativo en Godot). Tray queda como opcional tardío. |
| Consumo en idle demasiado alto | 0 | Medir pronto; bajar FPS/render al minimizar; liberar escenas. |
| Determinismo cliente≠servidor | 1 | Aritmética entera, RNG sembrado, tests golden. |
| Balance del meta competitivo | 4–5 | Datos de telemetría + temporadas que ajustan reglas. |
| Complejidad/coste del backend y Market | 5–6 | Diferidos al final; MVP es single-player + idle. |
