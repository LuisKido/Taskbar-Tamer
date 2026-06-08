# 02 — Arquitectura Técnica

> El **cómo**. Decisiones de ingeniería para implementar el diseño de [01-documento-diseno.md](01-documento-diseno.md).

## 1. Decisiones de arquitectura (ADR resumido)

| # | Decisión | Razón | Alternativa descartada |
|---|----------|-------|------------------------|
| 1 | **Motor: Godot 4** | UI/animación de batallas y exportación a Steam sencillas. | Unity (licencia/peso), .NET nativo (UI de juego pobre). |
| 2 | **Lenguaje: C# (.NET)** | El núcleo de simulación debe ser determinista, testeable (xUnit) y **compartirse con el servidor**. | GDScript (no comparte con servidor, sin tooling de tests maduro). |
| 3 | **Núcleo de simulación sin dependencias de Godot** | Un proyecto C# puro (`core/`) que compila igual en cliente y servidor headless. | Lógica acoplada a nodos de Godot (no reutilizable en servidor). |
| 4 | **Determinismo por aritmética entera + RNG sembrado** | Floats no son reproducibles entre máquinas/CPU. Cliente y servidor deben coincidir bit a bit. | Floats con física del motor. |
| 5 | **Idle por acumulación de tiempo, no simulación en vivo** | Bajo consumo: nada corre cada frame en segundo plano. | Tick de combate real en idle (mata el objetivo de consumo). |

> ⚠️ **Riesgo principal a validar pronto (spike):** Godot no ofrece de forma nativa un *widget acoplado a la barra de tareas* ni un icono de bandeja del sistema. Ver §5. Es lo primero a prototipar técnicamente antes de comprometernos.

## 2. Estructura de la solución

```
Taskbar Tamer/
├── core/                  # Librería C# pura — SIN referencias a Godot
│   ├── Model/             # Criatura, Parte, Set, Stats, Inventario
│   ├── Simulation/        # Auto-battler determinista (turnos, targeting, estados)
│   ├── Idle/              # Resolución de farming por tiempo + tablas de loot
│   ├── Breeding/          # Herencia genética
│   ├── Rng/               # PRNG sembrado (xorshift/PCG, determinista)
│   └── Data/              # Definiciones de contenido (especies, sets, biomas) en JSON
├── game/                  # Proyecto Godot 4 (cliente)
│   ├── Scenes/            # Escenas: MainPanel, Battle, Inventory, TaskbarWidget
│   ├── UI/                # Scripts de UI (C#)
│   ├── Platform/          # Integración SO: ventana acoplada, tray, autostart
│   └── Steam/             # Wrapper de Steamworks (logros, market, cloud saves)
├── server/                # (Fase posterior) Servidor .NET headless — referencia a core/
│   └── RankedSim/         # Resuelve batallas asíncronas, ligas, temporadas
├── tests/                 # xUnit sobre core/ (determinismo, balance, regresión)
└── docs/
```

**Regla de oro:** `game/` y `server/` **dependen de** `core/`, nunca al revés. `core/` no conoce Godot ni red. Esto permite ejecutar exactamente la misma batalla en el cliente (preview) y en el servidor (oficial).

## 3. Determinismo de la simulación

El ranked asíncrono exige que `Simular(setupA, setupB, semilla)` dé **siempre** el mismo resultado en cualquier máquina.

Reglas:

1. **Sin floats en la resolución de combate.** Stats y daño en enteros (o punto fijo). Porcentajes como enteros sobre base 10000 (ej. crit 15% = 1500).
2. **RNG sembrado y explícito.** Un único `DeterministicRng` por batalla, sembrado con `(matchId)`. Nada de `Random` global ni `System.Random` sin semilla.
3. **Orden de iteración fijo.** Las colas de turnos y resolución de empates usan criterios totalmente ordenados (ej. SPD, luego índice de slot, luego id). Nunca dependas del orden de un `Dictionary`/`HashSet`.
4. **Sin tiempo real ni hilos en el núcleo.** La simulación es una función pura: estado de entrada → log de eventos + resultado.
5. **Salida = log de eventos.** El simulador produce una lista ordenada de eventos (`AttackEvent`, `StatusApplied`, `Death`…). El cliente **reproduce** ese log con animaciones; no recalcula la batalla.

> Tests de regresión: un set de batallas "golden" con resultado fijado. Cualquier cambio que rompa el determinismo o el balance hace fallar el test.

## 4. Fase idle (progreso por tiempo)

Para garantizar bajo consumo, el farming **no** ejecuta combates frame a frame.

- Cada equipo en un bioma tiene una **tasa de progreso** (XP/h, loot/h, peso por rareza) derivada de sus stats vs. el bioma.
- Al abrir el juego o al hacer tick periódico, se calcula `delta = ahora - últimaActualización` y se resuelve el botín acumulado con el `DeterministicRng` (o uno casual, el idle no es competitivo).
- **Frecuencia de tick en background:** baja (ej. cada 30–60 s) solo para actualizar el widget; el cálculo real es por delta de tiempo, así que dormir más tiempo no pierde progreso.
- **Progreso offline:** al lanzar el juego se resuelve todo el tiempo transcurrido desde el cierre (con tope configurable).

## 5. Widget de barra de tareas y consumo (riesgo a validar)

Godot no tiene API nativa de "widget de taskbar" ni de system tray. Plan:

- **Opción A (recomendada a investigar primero):** ventana Godot **borderless, always-on-top, sin barra de tareas propia**, anclada cerca de la bandeja del sistema, mostrando estado compacto (criaturas, loot reciente). + un **icono de bandeja** mediante un *GDExtension* nativo (C++/C#) o un proceso ayudante nativo ligero.
- **Opción B:** proceso ayudante nativo en C#/.NET (WinForms `NotifyIcon` o Win32) que vive en la bandeja y solo lanza/mensajea con el proceso Godot pesado, que permanece **suspendido/oculto** hasta abrir la fase activa.

Medidas de bajo consumo en idle:
- Bajar `Engine.MaxFps` y `Engine.TargetFps` a 1–5 cuando está minimizado; pausar el render.
- Liberar escenas pesadas (batalla, inventario) cuando no están visibles.
- Objetivo orientativo: **< 80–100 MB RAM y ~0% CPU** en estado inactivo. A medir en el spike.

## 6. Persistencia

- **Local:** save en JSON/binario en `user://`. Autosave por delta de tiempo y al cerrar.
- **Steam Cloud:** sincroniza el save local entre máquinas.
- **Autoridad competitiva:** el estado que cuenta para el ranked (setup bloqueado) vive en el **servidor**. El save local es para progreso PvE/idle; el servidor valida el setup para evitar trampas.

## 7. Integración Steam

Vía **GodotSteam** (GDExtension) o wrapper propio sobre Steamworks.

- Logros, estadísticas, leaderboards de temporada.
- Steam Cloud (saves).
- **Steam Community Market:** las partes como ítems de inventario Steam (Steam Inventory Service). Requiere backend que firme transacciones. Es **fase tardía**, no MVP.

## 8. Servidor competitivo (fase posterior)

- **.NET headless** que referencia `core/` → misma simulación que el cliente.
- Recibe setups bloqueados, empareja por liga, simula batallas asíncronas, actualiza MMR/ligas.
- Stack a decidir más adelante (ASP.NET minimal API + base de datos). Fuera de alcance del MVP.

## 9. Anti-cheat / integridad

- Cliente envía **setup + acciones**, nunca el resultado. El servidor recalcula.
- Validación de que las partes/criaturas del setup pertenecen legítimamente a la cuenta.
- Determinismo permite detectar discrepancias cliente-servidor (posible cheat o bug).
