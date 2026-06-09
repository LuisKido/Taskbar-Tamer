using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Simulation;

namespace TaskbarTamer.Game;

/// <summary>
/// Arena en vivo: auto-battler continuo con criaturas estilo "sprite", combate por rol
/// (frontal melee / retaguardia a distancia), jefes cada 10 fases, 3 mapas que rotan,
/// derrota con retirada, y efectos (partículas, anillos, destellos, números de daño,
/// banner y barra de jefe). El color del ataque depende de la anatomía ofensiva.
/// </summary>
public partial class ArenaView : Control
{
    private const float RangedX = 22f;
    private const float MeleeX = 60f;
    private const float ProjectileSpeed = 280f;
    private const float MeleeSpeed = 80f;
    private const float EnemySpeed = 26f;
    private const float PlayerAttackInterval = 0.55f;
    private const float EnemyAttackInterval = 0.9f;
    private const int WaveSize = 5;
    private const float BaseEnemyHp = 22f;
    private const int MapBand = 10;
    private const float WaveHealFraction = 0.35f;
    private const float AbilityInterval = 6f;
    private const float TauntDuration = 2.5f;
    private const float DashDuration = 0.22f;
    private const float KiteRange = 42f;

    private static readonly Color RangedColor = new(0.45f, 0.95f, 0.85f);
    private static readonly Color MeleeColor = new(0.45f, 0.72f, 1f);
    private static readonly Color HpBackColor = new(0f, 0f, 0f, 0.55f);
    private static readonly Color EnemyHpColor = new(0.95f, 0.45f, 0.45f);
    private static readonly Color AllyHpColor = new(0.45f, 0.9f, 0.6f);
    private static readonly Color DefaultShotColor = new(1f, 0.9f, 0.45f);
    private static readonly Color VenomColor = new(0.5f, 0.95f, 0.35f);
    private static readonly Color FangColor = new(1f, 0.5f, 0.35f);
    private static readonly Color ClawColor = new(1f, 0.85f, 0.35f);

    private sealed class MapDef
    {
        public required string Name;
        public required Color Background;
        public required Color EnemyColor;
        public required string BossName;
    }

    private static readonly MapDef[] Maps =
    {
        new() { Name = "Bosque Abisal", Background = new Color(0.06f, 0.10f, 0.08f), EnemyColor = new Color(0.55f, 0.85f, 0.4f), BossName = "Devorador Abisal" },
        new() { Name = "Cavernas de Magma", Background = new Color(0.12f, 0.07f, 0.05f), EnemyColor = new Color(0.95f, 0.5f, 0.25f), BossName = "Coloso de Magma" },
        new() { Name = "Tundra Espectral", Background = new Color(0.07f, 0.09f, 0.14f), EnemyColor = new Color(0.6f, 0.72f, 1f), BossName = "Heraldo Glacial" },
    };

    private enum Role { Melee, Ranged }

    private enum Ability { Cleave, Taunt, Dash, VenomNova }

    private sealed class PlayerUnit
    {
        public Vector2 Pos;
        public Vector2 Facing = Vector2.Right;
        public int Order;
        public Role Role;
        public int LaneIndex;
        public int LaneCount;
        public float Damage;
        public float CritChance;
        public float AttackTimer;
        public Color Color;
        public Color ShotColor;
        public float Radius;
        public float MaxHp;
        public float Hp;
        public float HitFlash;
        public Ability Ability;
        public float AbilityCooldown;
        public float DashTime;
        public Vector2 DashFrom;
        public Vector2 DashTo;
        public bool Downed => Hp <= 0f;
    }

    private sealed class Enemy
    {
        public Vector2 Pos;
        public Vector2 Facing = Vector2.Left;
        public float Hp;
        public float MaxHp;
        public float Radius;
        public float Damage;
        public float AttackTimer;
        public float HitFlash;
        public bool IsBoss;
    }

    private sealed class Shot
    {
        public Vector2 Pos;
        public Enemy Target = null!;
        public float Damage;
        public bool Crit;
        public Color Color;
    }

    private sealed class DamageNumber
    {
        public Vector2 Pos;
        public Vector2 Vel;
        public float Age;
        public float Life;
        public string Text = "";
        public Color Color;
        public float Scale;
    }

    private sealed class Floater
    {
        public Vector2 Pos;
        public float Age;
        public float Life;
        public string Text = "";
        public Color Color;
        public int Size;
    }

    private sealed class Ring
    {
        public Vector2 Pos;
        public float Age;
        public float Life;
        public float MaxRadius;
        public Color Color;
    }

    private sealed class Particle
    {
        public Vector2 Pos;
        public Vector2 Vel;
        public float Age;
        public float Life;
        public float Size;
        public Color Color;
    }

    /// <summary>Se emite al cambiar de fase, para que el panel principal refresque etiquetas.</summary>
    public event Action? StageAdvanced;

    private GameSession _session = null!;
    private readonly List<PlayerUnit> _units = new();
    private readonly List<Enemy> _enemies = new();
    private readonly List<Shot> _shots = new();
    private readonly List<DamageNumber> _damageNumbers = new();
    private readonly List<Floater> _floaters = new();
    private readonly List<Ring> _rings = new();
    private readonly List<Particle> _particles = new();
    private double _saveAccum;
    private bool _dirty;
    private string _banner = "";
    private float _bannerTimer;
    private float _shake;
    private float _pulseT;
    private PlayerUnit? _tauntUnit;
    private float _tauntTimer;

    private MapDef CurrentMap => Maps[((_session.Stage - 1) / MapBand) % Maps.Length];
    private bool IsBossStage => _session.Stage % MapBand == 0;

    public void Begin(GameSession session)
    {
        _session = session;
        CustomMinimumSize = new Vector2(0, 150);
        BuildUnits();
        SpawnWave();
    }

    /// <summary>Reconstruye las unidades (tras cambiar equipo/formación/roster). Cura a tope.</summary>
    public void RebuildUnits() => BuildUnits();

    // ---------- Construcción ----------

    private void BuildUnits()
    {
        _units.Clear();
        Setup? setup = _session.BuildPlayerSetup();

        List<Creature> front = (setup?.FrontLine ?? (IReadOnlyList<Creature>)_session.State.Roster).Take(3).ToList();
        List<Creature> back = (setup?.BackLine ?? Array.Empty<Creature>()).Take(3).ToList();

        for (int i = 0; i < front.Count; i++)
            _units.Add(MakeUnit(front[i], Role.Melee, i, front.Count));
        for (int i = 0; i < back.Count; i++)
            _units.Add(MakeUnit(back[i], Role.Ranged, i, back.Count));

        if (_units.Count == 0 && _session.State.Roster.Count > 0)
            _units.Add(MakeUnit(_session.State.Roster[0], Role.Melee, 0, 1));

        for (int i = 0; i < _units.Count; i++)
            _units[i].Order = i;
        foreach (PlayerUnit u in _units)
            u.Pos = HomeFor(u);
    }

    private PlayerUnit MakeUnit(Creature creature, Role role, int laneIndex, int laneCount)
    {
        Stats s = StatsResolver.Resolve(creature, SetRegistry.Empty).Stats;
        float maxHp = Math.Max(40f, s.MaxHp);
        return new PlayerUnit
        {
            Role = role,
            LaneIndex = laneIndex,
            LaneCount = laneCount,
            Damage = Math.Max(4f, s.Attack * 0.5f),
            CritChance = Math.Clamp(0.12f + s.CritChance / 10000f, 0f, 0.6f),
            AttackTimer = GD.Randf() * PlayerAttackInterval,
            Color = role == Role.Melee ? MeleeColor : RangedColor,
            ShotColor = AttackColorFor(creature),
            Radius = role == Role.Melee ? 10f : 9f,
            MaxHp = maxHp,
            Hp = maxHp,
            Ability = AbilityFor(creature),
            AbilityCooldown = AbilityInterval * (0.4f + GD.Randf() * 0.6f),
        };
    }

    // La habilidad activa depende de la anatomía equipada (decide la táctica del equipo).
    private static Ability AbilityFor(Creature c)
    {
        if (c.Equipped.ContainsKey(AnatomySlot.Shell) || c.Equipped.ContainsKey(AnatomySlot.Fur) || c.Equipped.ContainsKey(AnatomySlot.Scales))
            return Ability.Taunt;
        if (c.Equipped.ContainsKey(AnatomySlot.Wings) || c.Equipped.ContainsKey(AnatomySlot.Tail))
            return Ability.Dash;
        if (c.Equipped.ContainsKey(AnatomySlot.Glands) || c.Equipped.ContainsKey(AnatomySlot.Stinger))
            return Ability.VenomNova;
        return Ability.Cleave;
    }

    // El color del ataque depende de la anatomía ofensiva equipada (tipo de daño).
    private static Color AttackColorFor(Creature c)
    {
        if (c.Equipped.ContainsKey(AnatomySlot.Stinger) || c.Equipped.ContainsKey(AnatomySlot.Glands))
            return VenomColor; // veneno
        if (c.Equipped.ContainsKey(AnatomySlot.Fangs))
            return FangColor;  // físico (colmillos)
        if (c.Equipped.ContainsKey(AnatomySlot.Claws))
            return ClawColor;  // corte (garras)
        return DefaultShotColor;
    }

    // Las criaturas se agrupan en el centro de la arena (en columna vertical). Las melee
    // salen a interceptar y vuelven; las de distancia se quedan aquí disparando.
    private Vector2 HomeFor(PlayerUnit u)
    {
        float w = Math.Max(Size.X, 200f);
        float h = Math.Max(Size.Y, 100f);
        int n = Math.Max(1, _units.Count);
        float spacing = Math.Min(22f, (h - 26f) / n);
        float cx = w * 0.45f;
        float cy = h * 0.5f + (u.Order - (n - 1) / 2f) * spacing;
        return new Vector2(cx, cy);
    }

    // Punto de aparición en un borde aleatorio (los enemigos llegan de distintos lados).
    private Vector2 RandomEdgeSpawn(float w, float h)
    {
        int side = (int)(GD.Randf() * 4f) % 4;
        return side switch
        {
            0 => new Vector2(-12f, GD.Randf() * h),
            1 => new Vector2(w + 12f, GD.Randf() * h),
            2 => new Vector2(GD.Randf() * w, -12f),
            _ => new Vector2(GD.Randf() * w, h + 12f),
        };
    }

    private void SpawnWave()
    {
        _shots.Clear();
        float w = Math.Max(Size.X, 200f);
        float h = Math.Max(Size.Y, 100f);
        int stage = _session.Stage;

        if (IsBossStage)
        {
            float bossHp = BaseEnemyHp * (10f + stage * 1.6f);
            _enemies.Add(new Enemy
            {
                Pos = new Vector2(w + 20f, h / 2f),
                Hp = bossHp,
                MaxHp = bossHp,
                Radius = 22f,
                Damage = 6f + stage * 1.1f,
                AttackTimer = EnemyAttackInterval,
                IsBoss = true,
            });
            ShowBanner($"👹 ¡JEFE: {CurrentMap.BossName}!");
            return;
        }

        float hp = BaseEnemyHp * (1f + (stage - 1) * 0.35f);
        float dmg = 3f + stage * 0.8f;
        for (int i = 0; i < WaveSize; i++)
        {
            _enemies.Add(new Enemy
            {
                Pos = RandomEdgeSpawn(w, h),
                Hp = hp,
                MaxHp = hp,
                Radius = 8f + GD.Randf() * 3f,
                Damage = dmg,
                AttackTimer = GD.Randf() * EnemyAttackInterval,
            });
        }
    }

    // ---------- Bucle ----------

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree())
            return;

        float dt = (float)delta;

        UpdateUnits(dt);
        UpdateEnemies(dt);
        UpdateShots(dt);
        UpdateDamageNumbers(dt);
        UpdateFloaters(dt);
        UpdateRings(dt);
        UpdateParticles(dt);

        if (_bannerTimer > 0f) _bannerTimer -= dt;
        if (_shake > 0f) _shake = Math.Max(0f, _shake - dt * 18f);
        _pulseT += dt;
        if (_tauntTimer > 0f) _tauntTimer -= dt;
        if (_tauntUnit is { Downed: true }) _tauntUnit = null;

        if (_units.Count > 0 && _units.All(u => u.Downed))
            Retreat();
        else if (_enemies.Count == 0)
            AdvanceWave();

        _saveAccum += delta;
        if (_dirty && _saveAccum > 3.0)
        {
            _session.Save();
            _dirty = false;
            _saveAccum = 0;
        }

        QueueRedraw();
    }

    private void AdvanceWave()
    {
        _session.AdvanceStage();
        HealUnits(WaveHealFraction); // curación parcial: el desgaste importa
        ResetPositions();            // las criaturas vuelven a su sitio al cambiar de fase

        long reward = 2 + _session.Stage / 2;
        _session.State.Essence += reward;
        SpawnFloater(new Vector2(Size.X / 2f, 34f), $"+{reward} esencia", new Color(0.6f, 1f, 0.6f), 13);

        SpawnWave();
        _dirty = true;
        StageAdvanced?.Invoke();
    }

    private void Retreat()
    {
        _session.RetreatToMapStart();
        ShowBanner("💀 ¡DERROTA! Te retiras al inicio del mapa");
        SpawnShake(7f);
        BuildUnits(); // revive a tope al reiniciar el mapa
        SpawnWave();
        _dirty = true;
        StageAdvanced?.Invoke();
    }

    private void ResetPositions()
    {
        foreach (PlayerUnit u in _units)
            u.Pos = HomeFor(u);
    }

    private void HealUnits(float fraction)
    {
        foreach (PlayerUnit u in _units)
        {
            float baseHp = u.Downed ? 0f : u.Hp;
            u.Hp = Math.Min(u.MaxHp, baseHp + u.MaxHp * fraction);
        }
    }

    private void UpdateUnits(float dt)
    {
        foreach (PlayerUnit u in _units)
        {
            if (u.HitFlash > 0f) u.HitFlash -= dt;
            if (u.Downed) continue;

            // Embestida en curso: ignora el comportamiento normal mientras se desliza.
            if (u.DashTime > 0f)
            {
                UpdateDash(u, dt);
                continue;
            }

            // Habilidad activa (cooldown).
            u.AbilityCooldown -= dt;
            if (u.AbilityCooldown <= 0f && _enemies.Count > 0)
            {
                ExecuteAbility(u);
                u.AbilityCooldown = AbilityInterval;
                if (u.DashTime > 0f) continue; // la embestida empieza este mismo frame
            }

            Vector2 home = HomeFor(u);
            Enemy? target = NearestEnemy(u.Pos);
            if (target is not null)
                u.Facing = Dir(u.Pos, target.Pos);

            if (u.Role == Role.Ranged)
            {
                // Kiting: si un enemigo se acerca demasiado, retrocede sin dejar de disparar.
                if (target is not null && u.Pos.DistanceTo(target.Pos) < KiteRange)
                    u.Pos = ClampToArena(u.Pos + Dir(target.Pos, u.Pos) * 58f * dt);
                else
                    u.Pos = u.Pos.MoveToward(home, MeleeSpeed * 0.6f * dt);

                u.AttackTimer -= dt;
                if (target is not null && u.AttackTimer <= 0f)
                {
                    bool crit = GD.Randf() < u.CritChance;
                    _shots.Add(new Shot { Pos = u.Pos, Target = target, Damage = crit ? u.Damage * 2f : u.Damage, Crit = crit, Color = u.ShotColor });
                    SpawnParticles(u.Pos, 4, u.ShotColor, 40f);
                    u.AttackTimer = PlayerAttackInterval;
                }
                continue;
            }

            // Melee.
            if (target is null)
            {
                u.Facing = Dir(u.Pos, home);
                u.Pos = u.Pos.MoveToward(home, MeleeSpeed * dt);
                continue;
            }

            float range = u.Radius + target.Radius + 3f;
            if (u.Pos.DistanceTo(target.Pos) > range)
            {
                u.Pos = u.Pos.MoveToward(target.Pos, MeleeSpeed * dt);
            }
            else
            {
                u.AttackTimer -= dt;
                if (u.AttackTimer <= 0f)
                {
                    bool crit = GD.Randf() < u.CritChance;
                    HitEnemy(target, crit ? u.Damage * 2f : u.Damage, crit, u.ShotColor);
                    u.AttackTimer = PlayerAttackInterval;
                }
            }
        }
    }

    private void UpdateDash(PlayerUnit u, float dt)
    {
        u.DashTime -= dt;
        float t = Math.Clamp(1f - u.DashTime / DashDuration, 0f, 1f);
        u.Facing = Dir(u.DashFrom, u.DashTo);
        u.Pos = u.DashFrom.Lerp(u.DashTo, t);
        SpawnTrail(u.Pos, u.ShotColor);

        if (u.DashTime <= 0f)
        {
            foreach (Enemy e in EnemiesInRadius(u.Pos, 34f))
                HitEnemy(e, u.Damage, false, u.ShotColor);
            SpawnRing(u.Pos, u.ShotColor, 34f, 0.3f);
            SpawnParticles(u.Pos, 12, u.ShotColor, 120f);
            SpawnShake(2f);
        }
    }

    private void ExecuteAbility(PlayerUnit u)
    {
        switch (u.Ability)
        {
            case Ability.Taunt:
                _tauntUnit = u;
                _tauntTimer = TauntDuration;
                foreach (Enemy e in _enemies)
                    if (e.Pos.DistanceTo(u.Pos) < 78f)
                        e.Pos = e.Pos.MoveToward(u.Pos, 46f);
                SpawnRing(u.Pos, new Color(0.5f, 0.7f, 1f), 78f, 0.4f);
                SpawnFloater(u.Pos, "¡Provocar!", new Color(0.6f, 0.8f, 1f), 12);
                break;

            case Ability.Dash:
                u.DashFrom = u.Pos;
                u.DashTo = ClampToArena(EnemyCentroid());
                u.DashTime = DashDuration;
                SpawnFloater(u.Pos, "¡Embestida!", u.ShotColor, 12);
                break;

            case Ability.VenomNova:
                foreach (Enemy e in EnemiesInRadius(u.Pos, 48f))
                    HitEnemy(e, u.Damage * 1.1f, false, VenomColor);
                SpawnRing(u.Pos, VenomColor, 48f, 0.35f);
                SpawnParticles(u.Pos, 16, VenomColor, 90f);
                SpawnFloater(u.Pos, "¡Toxina!", VenomColor, 12);
                break;

            default: // Cleave
                foreach (Enemy e in EnemiesInRadius(u.Pos, 40f))
                    HitEnemy(e, u.Damage * 0.8f, false, u.ShotColor);
                SpawnRing(u.Pos, u.ShotColor, 40f, 0.25f);
                SpawnFloater(u.Pos, "¡Tajo!", u.ShotColor, 12);
                break;
        }
    }

    private List<Enemy> EnemiesInRadius(Vector2 center, float radius)
    {
        float r2 = radius * radius;
        var list = new List<Enemy>();
        foreach (Enemy e in _enemies)
            if (e.Pos.DistanceSquaredTo(center) <= r2)
                list.Add(e);
        return list;
    }

    private Vector2 EnemyCentroid()
    {
        if (_enemies.Count == 0)
            return Size / 2f;
        Vector2 sum = Vector2.Zero;
        foreach (Enemy e in _enemies)
            sum += e.Pos;
        return sum / _enemies.Count;
    }

    private Vector2 ClampToArena(Vector2 p)
        => new(Math.Clamp(p.X, 8f, Math.Max(9f, Size.X - 8f)), Math.Clamp(p.Y, 8f, Math.Max(9f, Size.Y - 8f)));

    private void UpdateEnemies(float dt)
    {
        foreach (Enemy e in _enemies)
        {
            if (e.HitFlash > 0f) e.HitFlash -= dt;

            // Si hay provocación activa, los enemigos van a por el provocador.
            PlayerUnit? target = (_tauntTimer > 0f && _tauntUnit is { Downed: false })
                ? _tauntUnit
                : NearestAlivePlayer(e.Pos);
            if (target is null)
                continue;
            e.Facing = Dir(e.Pos, target.Pos);

            float range = e.Radius + target.Radius + 3f;
            if (e.Pos.DistanceTo(target.Pos) > range)
            {
                e.Pos = e.Pos.MoveToward(target.Pos, EnemySpeed * dt);
            }
            else
            {
                e.AttackTimer -= dt;
                if (e.AttackTimer <= 0f)
                {
                    HitPlayer(target, e.Damage, e.IsBoss);
                    e.AttackTimer = EnemyAttackInterval;
                }
            }
        }
    }

    private void HitEnemy(Enemy target, float dmg, bool crit, Color color)
    {
        target.Hp -= dmg;
        target.HitFlash = 0.12f;
        SpawnDamageNumber(target.Pos, (int)MathF.Round(dmg), crit, crit ? new Color(1f, 0.78f, 0.2f) : Colors.White);
        SpawnRing(target.Pos, color, 10f, 0.2f);
        SpawnParticles(target.Pos, crit ? 8 : 5, color, 70f);
        if (target.Hp <= 0f)
        {
            SpawnRing(target.Pos, EnemyHpColor, target.IsBoss ? 36f : 18f, 0.4f);
            SpawnParticles(target.Pos, target.IsBoss ? 22 : 8, CurrentMap.EnemyColor, 110f);
            if (target.IsBoss)
                OnBossDefeated(target);
            _enemies.Remove(target);
        }
    }

    private void OnBossDefeated(Enemy boss)
    {
        SpawnShake(7f);
        Part reward = _session.GrantBossReward();
        ShowBanner($"🎁 ¡Botín de jefe: {Labels.Rarity(reward.Rarity)} {Labels.Slot(reward.Slot)}!");
        SpawnFloater(boss.Pos, $"¡{Labels.Rarity(reward.Rarity)}!", new Color(1f, 0.82f, 0.3f), 15);
        _dirty = true;
    }

    private void HitPlayer(PlayerUnit target, float dmg, bool boss)
    {
        target.Hp -= dmg;
        target.HitFlash = 0.14f;
        SpawnDamageNumber(target.Pos, (int)MathF.Round(dmg), false, new Color(1f, 0.4f, 0.4f));
        SpawnRing(target.Pos, new Color(1f, 0.4f, 0.4f, 0.8f), 9f, 0.2f);
        if (boss) SpawnShake(3f);
        if (target.Hp <= 0f)
        {
            target.Hp = 0f;
            SpawnRing(target.Pos, target.Color, 20f, 0.45f);
            SpawnParticles(target.Pos, 10, target.Color, 90f);
            SpawnShake(2f);
        }
    }

    private Enemy? NearestEnemy(Vector2 from)
    {
        Enemy? best = null;
        float bestDist = float.MaxValue;
        foreach (Enemy e in _enemies)
        {
            float d = from.DistanceSquaredTo(e.Pos);
            if (d < bestDist) { bestDist = d; best = e; }
        }
        return best;
    }

    private PlayerUnit? NearestAlivePlayer(Vector2 from)
    {
        PlayerUnit? best = null;
        float bestDist = float.MaxValue;
        foreach (PlayerUnit u in _units)
        {
            if (u.Downed) continue;
            float d = from.DistanceSquaredTo(u.Pos);
            if (d < bestDist) { bestDist = d; best = u; }
        }
        return best;
    }

    private void UpdateShots(float dt)
    {
        for (int i = _shots.Count - 1; i >= 0; i--)
        {
            Shot s = _shots[i];
            if (!_enemies.Contains(s.Target))
            {
                _shots.RemoveAt(i);
                continue;
            }

            s.Pos = s.Pos.MoveToward(s.Target.Pos, ProjectileSpeed * dt);
            SpawnTrail(s.Pos, s.Color);
            if (s.Pos.DistanceTo(s.Target.Pos) <= s.Target.Radius + 2f)
            {
                HitEnemy(s.Target, s.Damage, s.Crit, s.Color);
                _shots.RemoveAt(i);
            }
        }
    }

    // ---------- Efectos ----------

    private void SpawnDamageNumber(Vector2 at, int amount, bool crit, Color color)
    {
        _damageNumbers.Add(new DamageNumber
        {
            Pos = at + new Vector2((GD.Randf() - 0.5f) * 6f, -10f),
            Vel = new Vector2((GD.Randf() - 0.5f) * 26f, -46f),
            Age = 0f,
            Life = crit ? 0.85f : 0.7f,
            Text = amount.ToString(),
            Color = color,
            Scale = crit ? 1.6f : 1f,
        });
    }

    private void UpdateDamageNumbers(float dt)
    {
        for (int i = _damageNumbers.Count - 1; i >= 0; i--)
        {
            DamageNumber d = _damageNumbers[i];
            d.Pos += d.Vel * dt;
            d.Vel = new Vector2(d.Vel.X, d.Vel.Y + 90f * dt);
            d.Age += dt;
            if (d.Age >= d.Life)
                _damageNumbers.RemoveAt(i);
        }
    }

    private void SpawnFloater(Vector2 at, string text, Color color, int size)
        => _floaters.Add(new Floater { Pos = at, Age = 0f, Life = 1.3f, Text = text, Color = color, Size = size });

    private void UpdateFloaters(float dt)
    {
        for (int i = _floaters.Count - 1; i >= 0; i--)
        {
            _floaters[i].Pos += new Vector2(0f, -18f * dt);
            _floaters[i].Age += dt;
            if (_floaters[i].Age >= _floaters[i].Life)
                _floaters.RemoveAt(i);
        }
    }

    private void SpawnRing(Vector2 pos, Color color, float maxRadius, float life)
        => _rings.Add(new Ring { Pos = pos, Color = color, MaxRadius = maxRadius, Life = life });

    private void UpdateRings(float dt)
    {
        for (int i = _rings.Count - 1; i >= 0; i--)
        {
            _rings[i].Age += dt;
            if (_rings[i].Age >= _rings[i].Life)
                _rings.RemoveAt(i);
        }
    }

    private void SpawnParticles(Vector2 at, int count, Color color, float speed)
    {
        for (int i = 0; i < count; i++)
        {
            float ang = GD.Randf() * Mathf.Tau;
            float spd = speed * (0.4f + GD.Randf() * 0.6f);
            _particles.Add(new Particle
            {
                Pos = at,
                Vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd,
                Age = 0f,
                Life = 0.25f + GD.Randf() * 0.2f,
                Size = 2f + GD.Randf() * 1.5f,
                Color = color,
            });
        }
    }

    private void SpawnTrail(Vector2 at, Color color)
    {
        _particles.Add(new Particle
        {
            Pos = at,
            Vel = new Vector2((GD.Randf() - 0.5f) * 12f, (GD.Randf() - 0.5f) * 12f),
            Age = 0f,
            Life = 0.18f,
            Size = 2f,
            Color = color,
        });
    }

    private void UpdateParticles(float dt)
    {
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            Particle p = _particles[i];
            p.Pos += p.Vel * dt;
            p.Vel *= MathF.Max(0f, 1f - 5f * dt); // rozamiento
            p.Age += dt;
            if (p.Age >= p.Life)
                _particles.RemoveAt(i);
        }
    }

    private void ShowBanner(string text)
    {
        _banner = text;
        _bannerTimer = 2.5f;
    }

    private void SpawnShake(float amount) => _shake = Math.Max(_shake, amount);

    // ---------- Dibujo ----------

    public override void _Draw()
    {
        Vector2 shakeOffset = _shake > 0.05f
            ? new Vector2((GD.Randf() - 0.5f) * _shake, (GD.Randf() - 0.5f) * _shake)
            : Vector2.Zero;
        DrawSetTransform(shakeOffset, 0f, Vector2.One);

        MapDef map = CurrentMap;
        DrawRect(new Rect2(Vector2.Zero, Size), map.Background);

        // Partículas (debajo de las criaturas).
        foreach (Particle p in _particles)
        {
            float t = p.Age / p.Life;
            Color c = p.Color;
            c.A = Math.Clamp(1f - t, 0f, 1f);
            DrawCircle(p.Pos, p.Size * (1f - t * 0.5f), c);
        }

        // Aliados (miran a la derecha).
        foreach (PlayerUnit u in _units)
        {
            if (u.Downed) continue;
            Color body = u.HitFlash > 0f ? new Color(1f, 0.7f, 0.7f) : u.Color;
            DrawCreature(u.Pos, u.Radius, body, u.Facing, enemy: false);
            if (u.Role == Role.Ranged)
                DrawArc(u.Pos, u.Radius + 2f, 0f, Mathf.Tau, 20, new Color(1f, 1f, 1f, 0.4f), 1.5f);
            if (u.AbilityCooldown <= 0f) // habilidad lista: brillo pulsante
            {
                Color glow = AbilityColor(u);
                glow.A = 0.4f + 0.3f * (0.5f + 0.5f * MathF.Sin(_pulseT * 6f));
                DrawArc(u.Pos, u.Radius + 4f, 0f, Mathf.Tau, 22, glow, 2f);
            }
            DrawBar(new Vector2(u.Pos.X - 11f, u.Pos.Y + u.Radius + 3f), 22f, u.Hp / u.MaxHp, AllyHpColor);
        }

        // Enemigos (miran a la izquierda).
        Enemy? boss = null;
        foreach (Enemy e in _enemies)
        {
            if (e.IsBoss) boss = e;
            Color baseCol = e.IsBoss ? Darken(map.EnemyColor) : map.EnemyColor;
            Color body = e.HitFlash > 0f ? Colors.White : baseCol;
            DrawCreature(e.Pos, e.Radius, body, e.Facing, enemy: true);
            if (!e.IsBoss)
                DrawBar(new Vector2(e.Pos.X - 9f, e.Pos.Y - e.Radius - 6f), 18f, e.Hp / e.MaxHp, EnemyHpColor);
        }

        foreach (Shot s in _shots)
            DrawCircle(s.Pos, 3f, s.Color);

        foreach (Ring r in _rings)
        {
            float t = r.Age / r.Life;
            Color c = r.Color;
            c.A = Math.Clamp(1f - t, 0f, 1f);
            DrawArc(r.Pos, r.MaxRadius * t, 0f, Mathf.Tau, 24, c, 2f);
        }

        Font font = GetThemeDefaultFont();
        DrawDamageNumbers(font);

        foreach (Floater f in _floaters)
        {
            Color c = f.Color;
            c.A = Math.Clamp(1f - f.Age / f.Life, 0f, 1f);
            DrawString(font, f.Pos, f.Text, HorizontalAlignment.Center, 0, f.Size, c);
        }

        // HUD: fase + mapa.
        DrawString(font, new Vector2(8, 18), $"Fase {_session.Stage}", HorizontalAlignment.Left, -1, 14, new Color(1f, 1f, 1f, 0.9f));
        DrawString(font, new Vector2(8, 33), map.Name, HorizontalAlignment.Left, -1, 11, new Color(1f, 1f, 1f, 0.5f));

        // Barra de jefe arriba.
        if (boss is not null)
            DrawBossBar(font, boss, map);

        if (_bannerTimer > 0f)
        {
            float a = Math.Clamp(_bannerTimer / 2.5f, 0f, 1f);
            DrawString(font, new Vector2(0, Size.Y / 2f), _banner, HorizontalAlignment.Center, Size.X, 16, new Color(1f, 0.95f, 0.7f, a));
        }
    }

    private void DrawBossBar(Font font, Enemy boss, MapDef map)
    {
        float margin = 12f;
        float y = 8f;
        float w = Size.X - margin * 2f;
        DrawRect(new Rect2(new Vector2(margin, y), new Vector2(w, 8f)), HpBackColor);
        DrawRect(new Rect2(new Vector2(margin, y), new Vector2(w * Math.Clamp(boss.Hp / boss.MaxHp, 0f, 1f), 8f)), new Color(0.95f, 0.3f, 0.3f));
        DrawString(font, new Vector2(margin, y + 22f), $"👹 {map.BossName}", HorizontalAlignment.Left, w, 12, new Color(1f, 0.85f, 0.85f, 0.9f));
    }

    // "Sprite" simple: cuerpo + contorno + ojos que miran hacia el objetivo (facing).
    private void DrawCreature(Vector2 pos, float r, Color body, Vector2 facing, bool enemy)
    {
        DrawCircle(pos, r, body);
        DrawArc(pos, r, 0f, Mathf.Tau, 22, new Color(0f, 0f, 0f, 0.35f), 1.5f);

        Vector2 f = facing.LengthSquared() > 0.001f ? facing.Normalized() : (enemy ? Vector2.Left : Vector2.Right);
        Vector2 perp = new Vector2(-f.Y, f.X);
        float eyeR = r * 0.24f;
        Vector2 eyeCenter = pos + f * (r * 0.30f);
        Vector2 eyeA = eyeCenter + perp * (r * 0.30f);
        Vector2 eyeB = eyeCenter - perp * (r * 0.30f);
        DrawCircle(eyeA, eyeR, Colors.White);
        DrawCircle(eyeB, eyeR, Colors.White);

        Color pupil = enemy ? new Color(0.8f, 0.1f, 0.1f) : new Color(0.1f, 0.1f, 0.15f);
        Vector2 pupilOff = f * (eyeR * 0.45f);
        DrawCircle(eyeA + pupilOff, eyeR * 0.5f, pupil);
        DrawCircle(eyeB + pupilOff, eyeR * 0.5f, pupil);
    }

    private static Vector2 Dir(Vector2 from, Vector2 to)
    {
        Vector2 d = to - from;
        return d.LengthSquared() > 0.0001f ? d.Normalized() : Vector2.Right;
    }

    private static Color AbilityColor(PlayerUnit u) => u.Ability switch
    {
        Ability.Taunt => new Color(0.5f, 0.7f, 1f),
        Ability.VenomNova => VenomColor,
        _ => u.ShotColor,
    };

    private void DrawBar(Vector2 pos, float width, float frac, Color fill)
    {
        frac = Math.Clamp(frac, 0f, 1f);
        DrawRect(new Rect2(pos, new Vector2(width, 3f)), HpBackColor);
        DrawRect(new Rect2(pos, new Vector2(width * frac, 3f)), fill);
    }

    private void DrawDamageNumbers(Font font)
    {
        foreach (DamageNumber d in _damageNumbers)
        {
            float alpha = Math.Clamp(1f - d.Age / d.Life, 0f, 1f);
            Color color = d.Color;
            color.A = alpha;

            int fontSize = (int)(13f * d.Scale);
            float digitWidth = 8f * d.Scale;
            for (int i = 0; i < d.Text.Length; i++)
            {
                float yStagger = -MathF.Sin((i + 1) * 0.9f) * 2.5f * d.Scale;
                DrawString(font, new Vector2(d.Pos.X + i * digitWidth, d.Pos.Y + yStagger),
                    d.Text[i].ToString(), HorizontalAlignment.Left, -1, fontSize, color);
            }
        }
    }

    private static Color Darken(Color c) => new(c.R * 0.7f, c.G * 0.7f, c.B * 0.7f);
}
