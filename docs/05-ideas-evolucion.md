# 05 — Ideas / Backlog de evolución (tema Digimon)

> Banco de vocabulario y mecánicas para añadir **profundidad** al sistema de evolución/rareza
> más adelante. Nada de esto está implementado todavía; es material de diseño guardado.

## Listado original (lluvia de ideas del usuario)

```
evo
digi
super
matrix
perfect
warp
deg
special
dark
armor
dna
blast
mode
slide
biomerge
shining
pseudo
bio-hybrid
burs
X-evo
death
Spirit
Fusión
Unified
ancient
```

## Cómo podríamos usarlo (interpretación, no definitivo)

El listado mezcla **etapas**, **métodos de evolución** y **atributos/formas especiales** del universo Digimon.
Posibles ejes de profundidad futura:

### a) Etapas de evolución (ya en uso como rarezas de partes)
Hoy las 8 rarezas son etapas: `Fresh → In-Training → Rookie → Champion → Ultimate → Mega → Burst Mode → BioMerge`.
Términos relacionados del listado: `perfect`, `super`, `burs(t)`, `biomerge`, `warp`, `ancient`.

### b) Métodos de evolución de **criaturas** (no implementado)
Una criatura podría "evolucionar" a una forma superior según el método, cada uno con condiciones y bonos distintos:
- **Warp / Slide** — saltar etapas / cambiar entre formas.
- **DNA / BioMerge / Fusión / Unified** — fusionar dos criaturas en una superior (estilo Jogress/Xros).
- **Armor (`armor`)** — evolución con un "objeto/parte" especial (encaja con el equipamiento biológico del juego).
- **Spirit** — evolución por "esencia" (encaja con nuestra esencia genética).
- **X-evo (`X-evo`)** — variante alternativa (X-Antibody): rama de stats distinta.
- **Mode Change (`mode`)** — formas temporales en combate (¿ultimate de arena?).
- **Pseudo / Bio-Hybrid (`pseudo`, `bio-hybrid`)** — híbridos parciales.

### c) Atributos / familias (no implementado)
- `dark`, `shining`, `death`, `ancient`, `special`, `matrix`, `blast`, `digi` — posibles **atributos elementales/temáticos**
  o **familias de set** que otorguen sinergias (ver sets en docs/01 §3).

## Ganchos de implementación cuando se retome
- **Evolución de criatura** se apoyaría en: `Creature.Level/MaxLevel` (ya existe), rareza de partes equipadas,
  y posiblemente consumir esencia o una segunda criatura (DNA/Fusión).
- **Atributos/familias** se apoyarían en `SetDefinition`/`SetRegistry` (ya existen en `core/`).
- `deg` (de-digivolve / degeneración) podría ser un mecanismo de "reciclar" criaturas devolviendo material.
