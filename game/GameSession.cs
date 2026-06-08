using System;
using TaskbarTamer.Core;
using TaskbarTamer.Core.Data;
using TaskbarTamer.Core.Idle;
using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Persistence;
using TaskbarTamer.Core.Simulation;

namespace TaskbarTamer.Game;

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
    }

    /// <summary>Resuelve el tiempo transcurrido desde la última sesión y añade el botín.</summary>
    public void ApplyOfflineProgress(long nowUnix)
    {
        long elapsed = Math.Max(0, nowUnix - State.LastFarmedUnixSeconds);
        FarmingResult result = ResolveFarming(elapsed);

        LastOfflineLoot = result.Loot.Count;
        LastOfflineXp = result.XpGained;
        LastOfflineSeconds = elapsed;

        State.LastFarmedUnixSeconds = nowUnix;
        SaveStore.Save(State);
    }

    /// <summary>Farmea un periodo concreto bajo demanda (botón de prueba). Persiste el resultado.</summary>
    public (int loot, long xp) FarmFor(long seconds)
    {
        FarmingResult result = ResolveFarming(seconds);
        SaveStore.Save(State);
        return (result.Loot.Count, result.XpGained);
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

        State.NextId = ids.Peek;
        State.FarmingRngState = result.NewRngState;
        return result;
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
        };
    }
}
