# 04 — Roadmap

> Orden de construcción por fases. Cada fase produce algo **probable** y reduce riesgo antes de la siguiente. El núcleo (`core/`) se construye headless y testeado antes de tocar UI.

## Fase 0 — Fundaciones (esqueleto)
**Meta:** repo y solución listos para programar.
- [ ] Proyecto Godot 4 + solución C# con `core/`, `game/`, `tests/`.
- [ ] `.gitignore` (Godot + .NET), `core/` compila sin Godot, xUnit corre.
- [ ] Spike de riesgo: **widget de barra de tareas + tray + consumo en idle** (ver [arquitectura §5](02-arquitectura-tecnica.md#5-widget-de-barra-de-tareas-y-consumo-riesgo-a-validar)). Decidir Opción A vs B con datos reales de RAM/CPU.

## Fase 1 — Núcleo de simulación (determinista)
**Meta:** `Simular(setupA, setupB, semilla)` → log de eventos reproducible. *El corazón del juego.*
- [ ] Modelo de datos (`Stats`, `Part`, `Creature`, `Setup`) — ver [03-modelo-datos.md](03-modelo-datos.md).
- [ ] `DeterministicRng` sembrado.
- [ ] Resolución de combate: orden por SPD, targeting frontal/retaguardia, daño, críticos, evasión.
- [ ] Sinergias de set y estados (veneno, etc.).
- [ ] Tests: determinismo (misma entrada → mismo log) + batallas "golden".

## Fase 2 — Loop idle + inventario
**Meta:** farming por tiempo que genera loot real.
- [ ] Biomas, tablas de loot, tasa de progreso por equipo.
- [ ] Resolución por delta de tiempo + progreso offline.
- [ ] Inventario y fusión de partes.
- [ ] Persistencia local (`user://`).

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
| Widget de taskbar/tray no nativo en Godot | 0 | Spike temprano; fallback a proceso ayudante nativo (Opción B). |
| Consumo en idle demasiado alto | 0 | Medir pronto; bajar FPS/render al minimizar; liberar escenas. |
| Determinismo cliente≠servidor | 1 | Aritmética entera, RNG sembrado, tests golden. |
| Balance del meta competitivo | 4–5 | Datos de telemetría + temporadas que ajustan reglas. |
| Complejidad/coste del backend y Market | 5–6 | Diferidos al final; MVP es single-player + idle. |
