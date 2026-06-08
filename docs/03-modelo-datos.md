# 03 — Modelo de Datos

> Esquemas conceptuales del núcleo (`core/Model/`). Notación orientativa en C#. Todos los valores de combate son **enteros** por determinismo (ver [arquitectura §3](02-arquitectura-tecnica.md#3-determinismo-de-la-simulación)).

## 1. Estadísticas (`Stats`)

Todos los porcentajes se expresan como enteros sobre base **10000** (ej. 1500 = 15%).

| Stat | Tipo | Notas |
|------|------|-------|
| `MaxHp` | int | Salud máxima |
| `Attack` | int | Daño base |
| `Defense` | int | Mitigación plana / escalada |
| `Speed` | int | Determina orden de turno |
| `CritChance` | int (bp) | Prob. de crítico (base 10000) |
| `CritDamage` | int (bp) | Multiplicador de crítico |
| `Evasion` | int (bp) | Prob. de esquivar |
| `Resistances` | map<DamageType,int> | Resistencia por tipo de daño |
| `StatusPower` | int (bp) | Potencia/prob. de aplicar estados |

```csharp
public readonly record struct Stats(
    int MaxHp, int Attack, int Defense, int Speed,
    int CritChance, int CritDamage, int Evasion,
    int StatusPower /* + Resistances map */);
```

## 2. Rareza (`Rarity`)

`Común → PocoComún → Raro → Épico → Legendario`. Afecta a magnitud de stats, número de afijos y peso en tablas de loot.

## 3. Parte biológica (`Part`)

Reemplaza al equipo clásico. Ocupa una **ranura de anatomía**.

```csharp
public sealed class Part {
    public PartId Id;
    public AnatomySlot Slot;        // ver §4
    public FamilyId Family;         // para sinergias de set (ej. "BestiaAbisal")
    public Rarity Rarity;
    public Stats BaseStats;         // contribución a las stats de la criatura
    public List<Affix> Affixes;     // bonus extra según rareza
    public int FusionLevel;         // mejora por fusión de duplicados
    public DamageType? DamageType;  // solo partes ofensivas
}
```

## 4. Ranuras de anatomía (`AnatomySlot`)

Agrupadas en tres categorías de diseño:

| Categoría | Ranuras | Aporta principalmente |
|-----------|---------|-----------------------|
| **Ofensiva** | `Claws`, `Fangs`, `Stinger` | Attack, CritChance/Damage, DamageType |
| **Defensiva** | `Shell`, `Fur`, `Scales` | MaxHp, Defense, Resistances |
| **Utilidad** | `Wings`, `Tail`, `Glands` | Speed, Evasion, pasivas de estado |

> El número exacto de ranuras por criatura es ajuste de diseño (propuesta inicial: 1 por ranura listada = hasta 9). A balancear.

## 5. Sinergias de set (`SetBonus`)

Bonos por número de partes de la misma `Family` equipadas.

```csharp
public sealed class SetDefinition {
    public FamilyId Family;
    public Dictionary<int, List<Effect>> Thresholds; // 2 → [..], 3 → [..], 4 → [..]
}
```

Ej. "Bestia Abisal": 2 piezas → +10% Resistencia; 3 piezas → aplica Veneno al atacar.

## 6. Criatura (`Creature`)

```csharp
public sealed class Creature {
    public CreatureId Id;
    public SpeciesId Species;        // plantilla base de stats y ranuras
    public int Level;
    public int MaxLevel;             // límite → habilita retiro/herencia
    public Stats InnateBase;         // base de especie + rasgos heredados
    public Dictionary<AnatomySlot, PartId> Equipped;
    public List<Trait> InheritedTraits;  // de herencia genética
}
```

`StatsEfectivas = InnateBase + Σ(partes equipadas) + Σ(bonos de set) + rasgos`.

## 7. Formación de combate (`Formation` / `Setup`)

```csharp
public sealed class Setup {
    public Creature[] FrontLine;  // absorben daño
    public Creature[] BackLine;   // daño / estados
    // El tamaño de líneas es ajuste de diseño (propuesta: 3 + 3)
}
```

El **posicionamiento** afecta a reglas de targeting (frontal recibe primero, retaguardia protegida hasta que cae la línea frontal — regla a definir en el simulador).

## 8. Herencia genética (`Breeding`)

Al retirar una criatura en `MaxLevel`, transmite **un** rasgo o equipamiento como `Trait` permanente (estadística base) a la siguiente generación.

```csharp
public sealed class Trait {
    public TraitId Id;
    public Stats StatBonus;       // contribución base permanente
    public Origin Origin;         // de qué campeón/parte proviene
}
```

## 9. Loot e inventario (idle)

```csharp
public sealed class Biome {
    public BiomeId Id;
    public List<LootEntry> LootTable;  // peso por rareza/familia
    public int Difficulty;             // vs. stats del equipo → tasa de progreso
}

public sealed class LootEntry {
    public FamilyId Family;
    public Rarity Rarity;
    public int Weight;   // peso para selección ponderada (RNG)
}
```

El inventario acumula `Part`s. La **fusión** consume duplicados para subir `FusionLevel`/rareza.

## 10. Tipos de daño y estados (`DamageType`, `StatusEffect`)

- **DamageType:** físico, veneno, etc. (lista a definir; interactúa con `Resistances`).
- **StatusEffect:** veneno (daño por turno), ralentización, etc. Aplicados según `StatusPower` vs. resistencia objetivo.

> Las listas concretas de tipos, estados, familias y especies son **contenido** y vivirán en `core/Data/*.json`, no hardcodeadas en el modelo.
