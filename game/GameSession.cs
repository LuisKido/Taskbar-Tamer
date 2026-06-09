using System;
using System.Collections.Generic;
using System.Linq;
using TaskbarTamer.Core;
using TaskbarTamer.Core.Rng;
using TaskbarTamer.Core.Data;
using TaskbarTamer.Core.Idle;
using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Persistence;
using TaskbarTamer.Core.Simulation;

namespace TaskbarTamer.Game;

/// <summary>Zona de combate de una criatura.</summary>
public enum FormationZone
{
    Bench,
    Front,
    Back,
}

/// <summary>
/// Estado de la partida en memoria + orquestación con core/. Carga/crea el save,
/// aplica el progreso de farming offline al abrir y permite farmear bajo demanda.
/// Toda la lógica vive en core/; esta clase solo la cablea al ciclo de vida del juego.
/// </summary>
public sealed class GameSession
{
    private readonly GameConfig _config = GameConfig.Default;

    public SaveData State { get; private set; } = new();

    /// <summary>Botín obtenido en la última aplicación de progreso offline.</summary>
    public int LastOfflineLoot { get; private set; }
    public long LastOfflineXp { get; private set; }
    public long LastOfflineSeconds { get; private set; }

    public int TeamPower => PowerRating.Of(State.Roster, SetRegistry.Empty);

    public void LoadOrCreate(long nowUnix)
    {
        SaveData? loaded = SaveStore.Load();
        if (loaded is null)
        {
            State = NewGame(nowUnix);
            SaveStore.Save(State);
        }
        else
        {
            State = loaded;
        }

        if (State.Stage < 1)
            State.Stage = 1;

        EnsureFormation();
    }

    public int Stage => State.Stage;

    /// <summary>Avanza la fase de la arena. No persiste (la arena guarda con throttle).</summary>
    public void AdvanceStage() => State.Stage++;

    /// <summary>Retrocede al inicio del mapa actual (banda de 10 fases) tras una derrota.</summary>
    public void RetreatToMapStart()
    {
        int band = (State.Stage - 1) / 10;
        State.Stage = Math.Max(1, band * 10 + 1);
    }

    /// <summary>
    /// Botín garantizado al derrotar a un jefe: una parte de rareza alta que escala con
    /// la fase (Champion → BioMerge). La añade al inventario y la devuelve. No persiste.
    /// </summary>
    public Part GrantBossReward()
    {
        var rng = new DeterministicRng(State.FarmingRngState + (ulong)State.Stage * 0x9E3779B97F4A7C15UL);

        AnatomySlot[] slots = Enum.GetValues<AnatomySlot>();
        AnatomySlot slot = slots[rng.NextInt(slots.Length)];

        string[] families = { "Abisal", "Volcanica", "Espectral" };
        string family = families[rng.NextInt(families.Length)];

        Rarity rarity = State.Stage switch
        {
            < 10 => Rarity.Champion,
            < 20 => Rarity.Ultimate,
            < 30 => Rarity.Mega,
            < 40 => Rarity.BurstMode,
            _ => Rarity.BioMerge,
        };

        var ids = new IdAllocator(State.NextId);
        Part part = PartFactory.Create(ids.Next(), family, slot, rarity, _config);
        State.NextId = ids.Peek;
        State.Inventory.Add(part);
        return part;
    }

    // Migración/arranque: si no hay formación pero hay criaturas, coloca las primeras
    // en la línea frontal (hasta el máximo) para que pueda haber batalla.
    private void EnsureFormation()
    {
        if (State.FrontLine.Count > 0 || State.BackLine.Count > 0)
            return;

        foreach (Creature c in State.Roster)
        {
            if (State.FrontLine.Count >= _config.MaxLineSize)
                break;
            State.FrontLine.Add(c.Id);
        }
    }

    /// <summary>Resuelve el tiempo transcurrido desde la última sesión y añade el botín.</summary>
    public void ApplyOfflineProgress(long nowUnix)
    {
        FarmingResult result = ResolveAndAdvance(nowUnix, applyOfflineCap: true);

        LastOfflineLoot = result.Loot.Count;
        LastOfflineXp = result.XpGained;
        LastOfflineSeconds = (long)result.Encounters * _config.EncounterIntervalSeconds;

        SaveStore.Save(State);
    }

    /// <summary>
    /// Tick de farming en vivo (mientras el juego está abierto): resuelve el tiempo
    /// transcurrido desde el último tick y añade el botín. Devuelve cuántas partes cayeron.
    /// </summary>
    public int TickLiveFarming(long nowUnix)
    {
        FarmingResult result = ResolveAndAdvance(nowUnix, applyOfflineCap: false);
        if (result.Encounters > 0)
            Save();
        return result.Loot.Count;
    }

    // Resuelve el farming acumulado y avanza la marca de tiempo SOLO por el tiempo
    // realmente consumido (encuentros completos), conservando el resto para el próximo tick.
    private FarmingResult ResolveAndAdvance(long nowUnix, bool applyOfflineCap)
    {
        long elapsed = Math.Max(0, nowUnix - State.LastFarmedUnixSeconds);
        long forResolve = applyOfflineCap ? Math.Min(elapsed, _config.OfflineCapSeconds) : elapsed;

        FarmingResult result = ResolveFarming(forResolve);
        long consumed = (long)result.Encounters * _config.EncounterIntervalSeconds;

        if (applyOfflineCap && elapsed > _config.OfflineCapSeconds)
            State.LastFarmedUnixSeconds = nowUnix;     // descarta el exceso sobre el tope offline
        else
            State.LastFarmedUnixSeconds += consumed;   // conserva el tiempo sobrante

        return result;
    }

    /// <summary>Farmea un periodo concreto bajo demanda (botón de prueba). Persiste el resultado.</summary>
    public (int loot, long xp) FarmFor(long seconds)
    {
        FarmingResult result = ResolveFarming(seconds);
        SaveStore.Save(State);
        return (result.Loot.Count, result.XpGained);
    }

    /// <summary>Equipa una parte del inventario en una criatura del roster. Persiste.</summary>
    public void Equip(int creatureIndex, Part part)
    {
        if (creatureIndex < 0 || creatureIndex >= State.Roster.Count)
            return;
        if (!State.Inventory.Remove(part))
            return; // la parte debe estar en el inventario

        (Creature creature, Part? displaced) = Core.Model.Equipment.Equip(State.Roster[creatureIndex], part);
        State.Roster[creatureIndex] = creature;
        if (displaced is not null)
            State.Inventory.Add(displaced); // la desplazada vuelve al inventario

        Save();
    }

    /// <summary>Desequipa la parte de una ranura y la devuelve al inventario. Persiste.</summary>
    public void Unequip(int creatureIndex, AnatomySlot slot)
    {
        if (creatureIndex < 0 || creatureIndex >= State.Roster.Count)
            return;

        (Creature creature, Part? removed) = Core.Model.Equipment.Unequip(State.Roster[creatureIndex], slot);
        if (removed is null)
            return;

        State.Roster[creatureIndex] = creature;
        State.Inventory.Add(removed);
        Save();
    }

    /// <summary>
    /// Fusiona automáticamente todas las partes fusionables del inventario (en cascada)
    /// y persiste. Devuelve el número de fusiones realizadas.
    /// </summary>
    public int FuseAll()
    {
        var inv = new Inventory();
        inv.AddRange(State.Inventory);

        var ids = new IdAllocator(State.NextId);
        int fusions = inv.AutoFuse(ids, _config);

        if (fusions > 0)
        {
            State.Inventory.Clear();
            State.Inventory.AddRange(inv.Parts);
            State.NextId = ids.Peek;
            Save();
        }

        return fusions;
    }

    // ---------- Formación ----------

    public int MaxLine => _config.MaxLineSize;
    public int FrontCount => State.FrontLine.Count;
    public int BackCount => State.BackLine.Count;

    public FormationZone ZoneOf(long creatureId)
    {
        if (State.FrontLine.Contains(creatureId)) return FormationZone.Front;
        if (State.BackLine.Contains(creatureId)) return FormationZone.Back;
        return FormationZone.Bench;
    }

    public void PlaceFront(long creatureId)
    {
        if (ZoneOf(creatureId) == FormationZone.Front || State.FrontLine.Count >= _config.MaxLineSize)
            return;
        State.BackLine.Remove(creatureId);
        State.FrontLine.Add(creatureId);
        Save();
    }

    public void PlaceBack(long creatureId)
    {
        if (ZoneOf(creatureId) == FormationZone.Back || State.BackLine.Count >= _config.MaxLineSize)
            return;
        State.FrontLine.Remove(creatureId);
        State.BackLine.Add(creatureId);
        Save();
    }

    public void Bench(long creatureId)
    {
        bool changed = State.FrontLine.Remove(creatureId) | State.BackLine.Remove(creatureId);
        if (changed)
            Save();
    }

    /// <summary>Construye el <see cref="Setup"/> del jugador a partir de la formación guardada, o null si está vacía.</summary>
    public Setup? BuildPlayerSetup()
    {
        var byId = State.Roster.ToDictionary(c => c.Id);
        var front = State.FrontLine.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        var back = State.BackLine.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        if (front.Count == 0 && back.Count == 0)
            return null;
        return new Setup(front, back);
    }

    public void Save() => SaveStore.Save(State);

    // Resuelve farming sobre el bioma actual y vuelca el botín al estado.
    private FarmingResult ResolveFarming(long seconds)
    {
        Biome biome = Content.DefaultBiome(); // TODO: resolver por State.CurrentBiomeId
        var ids = new IdAllocator(State.NextId);

        FarmingResult result = FarmingSimulator.Resolve(
            new FarmingAssignment(TeamPower, State.FarmingRngState), biome, seconds, ids, _config);

        foreach (Part p in result.Loot)
            State.Inventory.Add(p);

        State.Essence += result.XpGained; // la XP de farming se acumula como esencia
        State.NextId = ids.Peek;
        State.FarmingRngState = result.NewRngState;
        return result;
    }

    // ---------- Colección: desbloquear criaturas (1 por habilidad) ----------

    /// <summary>Todos los arquetipos del juego (en orden de colección).</summary>
    public IReadOnlyList<Archetype> AllArchetypes => Content.Archetypes;

    /// <summary>¿Está ya en el roster esta criatura/arquetipo? (sólo hay 1 por arquetipo).</summary>
    public bool IsUnlocked(Archetype a) => State.Roster.Any(c => c.Archetype == a);

    public long UnlockCost(Archetype a) => Content.UnlockCost(a);

    public bool CanUnlock(Archetype a) => !IsUnlocked(a) && State.Essence >= Content.UnlockCost(a);

    /// <summary>Desbloquea la criatura de un arquetipo gastando esencia. Devuelve la criatura o null.</summary>
    public Creature? Unlock(Archetype a)
    {
        if (IsUnlocked(a) || State.Essence < Content.UnlockCost(a))
            return null;

        State.Essence -= Content.UnlockCost(a);

        var ids = new IdAllocator(State.NextId);
        Creature creature = Content.CreateSpecies(a, ids);
        State.NextId = ids.Peek;
        State.Roster.Add(creature);
        Save();
        return creature;
    }

    private SaveData NewGame(long nowUnix)
    {
        var ids = new IdAllocator(1);
        Creature starter = Content.StarterCreature(ids, _config);

        return new SaveData
        {
            NextId = ids.Peek,
            FarmingRngState = 0x1234_5678_9ABC_DEF0,
            CurrentBiomeId = Content.DefaultBiomeId,
            LastFarmedUnixSeconds = nowUnix,
            Roster = { starter },
            FrontLine = { starter.Id },
        };
    }
}
