# Taskbar Tamer

**Idle / Auto-Battler Táctico de Colección de Criaturas** para PC (Steam), operando como una app compacta de bajo consumo que el usuario coloca donde quiera en su escritorio.

Gestionas un equipo de criaturas que farmean en segundo plano mientras usas tu PC. Recolectas **mutaciones genéticas y partes biológicas** para equipar a tus bestias, armas la composición perfecta y escalas en ligas competitivas de batallas automatizadas asíncronas.

---

## Estado del proyecto

🟡 **Pre-producción.** Definiendo diseño técnico y arquitectura. Sin código todavía.

## Stack

- **Motor:** Godot 4
- **Lenguaje principal:** C# (.NET) — ver decisión en [docs/02-arquitectura-tecnica.md](docs/02-arquitectura-tecnica.md)
- **Plataforma:** Windows (Steam)
- **Backend competitivo:** servidor .NET dedicado que comparte el núcleo de simulación con el cliente

## Documentación

| Doc | Contenido |
|-----|-----------|
| [01 — Documento de diseño](docs/01-documento-diseno.md) | El "qué": pitch, bucle de juego, sistemas, competitivo |
| [02 — Arquitectura técnica](docs/02-arquitectura-tecnica.md) | El "cómo": módulos, determinismo, widget, Steam, servidor |
| [03 — Modelo de datos](docs/03-modelo-datos.md) | Esquemas: criaturas, partes, sets, estadísticas, rarezas |
| [04 — Roadmap](docs/04-roadmap.md) | Fases de desarrollo y orden de construcción |

## Principio rector

> **Bajo consumo.** El juego corre 24/7 como una ventana compacta que el usuario arrastra y coloca donde quiera (esquina, segundo monitor, etc.). Cada decisión técnica se mide contra el coste de RAM/CPU en estado inactivo. El farming idle NO simula combate en tiempo real: acumula tiempo y resuelve por tablas de botín.
