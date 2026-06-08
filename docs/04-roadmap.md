# 04 — Roadmap

> Orden de construcción por fases. Cada fase produce algo **probable** y reduce riesgo antes de la siguiente. El núcleo (`core/`) se construye headless y testeado antes de tocar UI.

## Fase 0 — Fundaciones (esqueleto)
**Meta:** repo y solución listos para programar.
- [x] Solución C# (`TaskbarTamer.sln`) con `core/` (lib) y `tests/` (xUnit). `core/` compila sin Godot.
- [x] `.gitignore` (Godot + .NET), repo git inicializado, build + test verdes (2/2).
- [x] Proyecto Godot 4 (`game/`, Godot 4.6.3 .NET) creado y enlazado a `core/`. Importa y compila en headless ✅.
- [ ] Spike de consumo: medición de RAM/CPU en idle (ver [arquitectura §5](02-arquitectura-tecnica.md#5-ventana-compacta-movible-y-consumo)). *(Ventana movible ya funciona; falta medir consumo.)*

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
- [x] Serialización del estado (`SaveData` + `SaveSerializer`, JSON, round-trip testeado) en `core/`.
- [ ] Escritura/lectura en `user://` — **pendiente**: requiere el cliente Godot (solo el I/O de archivo).

> Nota: se abstrae la fuerza del equipo como `TeamPower` (int). El cálculo real de `TeamPower` a partir de criaturas+stats llega con la Fase 1 (simulador).

## Fase 3 — Fase activa (UI)
**Meta:** el jugador gestiona y ve batallas.
- [x] Ventana compacta, sin bordes y **movible** (arrastrar para reposicionar) — `Main.cs`.
- [x] Integración Godot↔core: la pantalla usa `FarmingSimulator`/`PowerRating` reales.
- [x] Persistencia: `SaveStore` (I/O en `user://`) + `GameSession` (carga/crea, progreso offline al abrir). Verificado end-to-end.
- [x] Panel de gestión: roster, ranuras de anatomía por criatura, inventario, equipar/desequipar partes (con poder en vivo).
- [x] Inventario agrupado por tipo (×N) + **fusión en la UI** (botón "Fusionar todo", cascada).
- [x] Reclutamiento de criaturas: el farming da **esencia genética**; `CreatureFactory` genera criaturas nuevas (coste escalable). Roster multi-criatura.
- [x] Editor de formación: coloca criaturas en frontal/retaguardia/banca (máx. por línea); la batalla usa la formación guardada.
- [x] Reproductor de batalla: anima el log de eventos del simulador (barras de vida, golpes, críticos, veneno, K.O., desenlace). No recalcula.
- [x] **Arena en vivo**: auto-battler visual continuo (criaturas vs hordas) que avanza de Fase mientras el juego está abierto; daño escala con el poder del equipo. + heartbeat de farming en vivo (loot/esencia suben mientras lo ves).
  - [x] Combate por rol según formación (frontal = melee con movimiento, retaguardia = a distancia).
  - [x] Números de daño flotantes estilo Ragnarok + críticos.
  - [x] Barras de vida en aliados; los enemigos contraatacan; **derrota** → retirada al inicio del mapa.
  - [x] **Jefes cada 10 fases** + **3 mapas temáticos** que rotan; progresión más difícil.
  - [x] Efectos: destellos al golpear, anillos de impacto, poof de muerte, banners, screen shake.
- [ ] Modo compacto ↔ expandido.

## Fase 4 — Crianza y progresión a largo plazo
**Meta:** el bucle de optimización a lo largo del tiempo.
- [x] `Leveling`: XP por nivel, límite de nivel (`MaxLevel`), XP excedente descartada al tope.
- [x] `Trait` (rasgo heredable) integrado en `StatsResolver` (bonus de base permanente).
- [x] `Breeder`: retiro de campeón al máximo + herencia (rasgo desde parte equipada o rasgo previo).
- [x] Tests: progresión, tope de nivel, gate de retiro, % de herencia, descendencia más fuerte (5 tests).
- [ ] Balance de progresión — **iterativo**: requiere telemetría/jugabilidad real.

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
