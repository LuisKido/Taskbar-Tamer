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
    private const float JumpDuration = 0.5f;
    private const float JumpHeight = 28f;
    private const float JumpRadius = 46f;
    private const float KiteRange = 66f;

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

    private enum Ability { Blast, Taunt, DashDodge, Jump, VenomNova }

    private sealed class PlayerUnit
    {
        public Vector2 Pos;
        public Vector2 Facing = Vector2.Right;
        public int Order;
        public Archetype Archetype;
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
        public bool IsJump;
        public float VisualY;
        public float InvulnTimer;
        public Vector2 DashFrom;
        public Vector2 DashTo;
        public bool HasOffense;
        public bool HasShell;
        public bool HasWings;
        public bool HasTail;
        public Color Accent;
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
        public float StunTimer;
        public bool IsBoss;
    }

    private sealed class Shot
    {
        public Vector2 Pos;
        public Enemy Target = null!;
        public float Damage;
        public bool Crit;
        public Color Color;
        public float Speed = ProjectileSpeed;
        public float AoeRadius;   // > 0 = explota en área (ráfaga)
        public float DrawSize = 3f;
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

    // Tajo melee: media luna que aparece frente a la criatura y se desvanece.
    private sealed class Slash
    {
        public Vector2 Pos;
        public Vector2 Facing;
        public float Age;
        public float Life;
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
    private readonly List<Slash> _slashes = new();
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

        var eq = creature.Equipped;
        bool offense = eq.ContainsKey(AnatomySlot.Claws) || eq.ContainsKey(AnatomySlot.Fangs) || eq.ContainsKey(AnatomySlot.Stinger);
        bool shell = eq.ContainsKey(AnatomySlot.Shell) || eq.ContainsKey(AnatomySlot.Fur) || eq.ContainsKey(AnatomySlot.Scales);
        Rarity maxRarity = eq.Count > 0 ? eq.Values.Max(p => p.Rarity) : Rarity.Fresh;

        return new PlayerUnit
        {
            Archetype = creature.Archetype,
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
            HasOffense = offense,
            HasShell = shell,
            HasWings = eq.ContainsKey(AnatomySlot.Wings),
            HasTail = eq.ContainsKey(AnatomySlot.Tail),
            Accent = Labels.RarityColor(maxRarity),
        };
    }

    // La habilidad es intrínseca del arquetipo de la criatura (1 criatura por habilidad).
    private static Ability AbilityFor(Creature c) => c.Archetype switch
    {
        Archetype.Guardian => Ability.Taunt,
        Archetype.Charger => Ability.DashDodge,
        Archetype.Leaper => Ability.Jump,
        Archetype.Venomous => Ability.VenomNova,
        _ => Ability.Blast, // Bruiser
    };

    // El color del ataque es la firma del arquetipo de la criatura.
    private static Color AttackColorFor(Creature c) => c.Archetype switch
    {
        Archetype.Venomous => VenomColor,                  // verde
        Archetype.Bruiser => FangColor,                    // rojo
        Archetype.Charger => new Color(0.4f, 0.95f, 0.95f),// cian
        Archetype.Leaper => new Color(0.7f, 0.5f, 1f),     // morado
        _ => DefaultShotColor,                             // Guardian (dorado)
    };

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
        UpdateSlashes(dt);
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
        _enemies.Clear(); // ¡importante! limpia la horda que te venció (si no, se acumulan)
        _shots.Clear();
        BuildUnits();     // revive a tope al reiniciar el mapa
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
            if (u.InvulnTimer > 0f) u.InvulnTimer -= dt;
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
                // Retaguardia: mantiene distancia. Si un enemigo está cerca, kitea (se aleja,
                // curvando al centro cerca de los bordes). Si está lejos, NO avanza (se queda
                // donde está, p.ej. tras esquivar). Solo vuelve a su sitio si NO quedan enemigos.
                if (target is not null)
                {
                    if (u.Pos.DistanceTo(target.Pos) < KiteRange)
                        u.Pos = ClampToArena(u.Pos + KiteDir(u.Pos, target.Pos) * 62f * dt);
                }
                else
                {
                    u.Pos = u.Pos.MoveToward(home, MeleeSpeed * 0.6f * dt);
                }

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
                    SpawnSlash(u.Pos + u.Facing * u.Radius, u.Facing, u.ShotColor); // tajo frontal
                    HitEnemy(target, crit ? u.Damage * 2f : u.Damage, crit, u.ShotColor,
                        knockFrom: u.Pos, knockback: 11f, stun: 0.18f);
                    u.AttackTimer = PlayerAttackInterval;
                }
            }
        }
    }

    private void UpdateDash(PlayerUnit u, float dt)
    {
        u.DashTime -= dt;
        float dur = u.IsJump ? JumpDuration : DashDuration;
        float t = Math.Clamp(1f - u.DashTime / dur, 0f, 1f);
        u.Facing = Dir(u.DashFrom, u.DashTo);
        u.Pos = u.DashFrom.Lerp(u.DashTo, t);

        if (u.IsJump)
            u.VisualY = -MathF.Sin(t * MathF.PI) * JumpHeight; // arco del salto
        else
            SpawnTrail(u.Pos, u.ShotColor);

        if (u.DashTime <= 0f)
        {
            u.VisualY = 0f;
            float radius = u.IsJump ? JumpRadius : 34f;
            float mult = u.IsJump ? 1.5f : 1f;
            foreach (Enemy e in EnemiesInRadius(u.Pos, radius))
                HitEnemy(e, u.Damage * mult, false, u.ShotColor, knockFrom: u.Pos, knockback: u.IsJump ? 14f : 10f, stun: 0.2f);
            SpawnRing(u.Pos, u.ShotColor, radius, 0.3f);
            SpawnParticles(u.Pos, u.IsJump ? 18 : 12, u.ShotColor, 120f);
            SpawnShake(u.IsJump ? 4f : 2f);
            u.IsJump = false;
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

            case Ability.DashDodge:
                // Evasivo: se aleja del enemigo más cercano (o del grueso de la horda).
                Enemy? threat = NearestEnemy(u.Pos);
                Vector2 away = threat is not null ? Dir(threat.Pos, u.Pos) : Dir(EnemyCentroid(), u.Pos);
                u.DashFrom = u.Pos;
                u.DashTo = ClampToArena(u.Pos + away * 82f);
                u.DashTime = DashDuration;
                u.IsJump = false;
                u.InvulnTimer = DashDuration + 0.9f; // invulnerable mientras esquiva
                SpawnFloater(u.Pos, "¡Esquiva!", u.ShotColor, 12);
                break;

            case Ability.Jump:
                u.DashFrom = u.Pos;
                u.DashTo = ClampToArena(EnemyCentroid());
                u.DashTime = JumpDuration;
                u.IsJump = true;
                SpawnFloater(u.Pos, "¡Salto!", u.ShotColor, 12);
                break;

            case Ability.VenomNova:
                foreach (Enemy e in EnemiesInRadius(u.Pos, 48f))
                    HitEnemy(e, u.Damage * 1.1f, false, VenomColor, knockFrom: u.Pos, knockback: 8f, stun: 0.16f);
                SpawnRing(u.Pos, VenomColor, 48f, 0.35f);
                SpawnParticles(u.Pos, 16, VenomColor, 90f);
                SpawnFloater(u.Pos, "¡Toxina!", VenomColor, 12);
                break;

            default: // Blast: proyectil pesado que estalla en área
                Enemy? blastTarget = NearestEnemy(u.Pos);
                if (blastTarget is not null)
                {
                    _shots.Add(new Shot
                    {
                        Pos = u.Pos,
                        Target = blastTarget,
                        Damage = u.Damage * 2.5f,
                        Color = u.ShotColor,
                        Speed = 175f,
                        AoeRadius = 42f,
                        DrawSize = 8f,
                    });
                    SpawnParticles(u.Pos, 8, u.ShotColor, 50f);
                    SpawnFloater(u.Pos, "¡Ráfaga!", u.ShotColor, 13);
                }
                break;
        }
    }

    private void ExplodeBlast(Shot s)
    {
        foreach (Enemy e in EnemiesInRadius(s.Pos, s.AoeRadius))
            HitEnemy(e, s.Damage, false, s.Color, knockFrom: s.Pos, knockback: 16f, stun: 0.22f);
        SpawnRing(s.Pos, s.Color, s.AoeRadius, 0.35f);
        SpawnParticles(s.Pos, 18, s.Color, 120f);
        SpawnShake(4f);
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

    // Dirección de kiteo: alejarse del enemigo, pero tirar hacia el centro cuanto más
    // cerca se está de un borde, para no quedar acorralado contra la pared.
    private Vector2 KiteDir(Vector2 pos, Vector2 enemyPos)
    {
        Vector2 away = Dir(enemyPos, pos);
        const float margin = 36f;
        float distEdge = Math.Min(Math.Min(pos.X, Size.X - pos.X), Math.Min(pos.Y, Size.Y - pos.Y));
        float edge = Math.Clamp(1f - distEdge / margin, 0f, 1f);
        Vector2 toCenter = Dir(pos, Size / 2f);
        Vector2 dir = away + toCenter * (edge * 1.5f);
        return dir.LengthSquared() > 0.0001f ? dir.Normalized() : toCenter;
    }

    private void UpdateEnemies(float dt)
    {
        foreach (Enemy e in _enemies)
        {
            if (e.HitFlash > 0f) e.HitFlash -= dt;
            if (e.StunTimer > 0f) { e.StunTimer -= dt; continue; } // aturdido: detenido

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

    private void HitEnemy(Enemy target, float dmg, bool crit, Color color,
        Vector2 knockFrom = default, float knockback = 0f, float stun = 0f)
    {
        target.Hp -= dmg;
        target.HitFlash = 0.12f;

        // Empuje + aturdimiento (los jefes apenas se inmutan).
        float resist = target.IsBoss ? 0.2f : 1f;
        if (stun > 0f)
            target.StunTimer = MathF.Max(target.StunTimer, stun * resist);
        if (knockback > 0f)
            target.Pos = ClampToArena(target.Pos + Dir(knockFrom, target.Pos) * knockback * resist);
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
        if (target.InvulnTimer > 0f) // está esquivando: ignora el golpe
        {
            SpawnFloater(target.Pos, "esquiva", new Color(0.6f, 0.9f, 1f), 10);
            return;
        }

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
                if (s.AoeRadius > 0f) ExplodeBlast(s); // la ráfaga estalla aunque el objetivo ya no esté
                _shots.RemoveAt(i);
                continue;
            }

            s.Pos = s.Pos.MoveToward(s.Target.Pos, s.Speed * dt);
            SpawnTrail(s.Pos, s.Color);
            if (s.Pos.DistanceTo(s.Target.Pos) <= s.Target.Radius + 2f)
            {
                if (s.AoeRadius > 0f)
                    ExplodeBlast(s);
                else
                    HitEnemy(s.Target, s.Damage, s.Crit, s.Color, knockFrom: s.Pos, knockback: 6f, stun: 0.12f);
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

    private void SpawnSlash(Vector2 pos, Vector2 facing, Color color)
        => _slashes.Add(new Slash { Pos = pos, Facing = facing, Age = 0f, Life = 0.18f, Color = color });

    private void UpdateSlashes(float dt)
    {
        for (int i = _slashes.Count - 1; i >= 0; i--)
        {
            _slashes[i].Age += dt;
            if (_slashes[i].Age >= _slashes[i].Life)
                _slashes.RemoveAt(i);
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

            // Sombra en el suelo cuando salta.
            if (u.VisualY < -1f)
                DrawCircle(u.Pos, u.Radius * 0.7f, new Color(0f, 0f, 0f, 0.3f));

            Vector2 dp = new Vector2(u.Pos.X, u.Pos.Y + u.VisualY);
            Color body = u.HitFlash > 0f ? new Color(1f, 0.7f, 0.7f) : u.Color;
            DrawAlly(dp, u.Radius, body, u.Facing, u.Archetype);
            DrawEquipment(dp, u.Radius, u, u.Facing);
            if (u.InvulnTimer > 0f) // escudo de esquiva
            {
                float a = 0.4f + 0.3f * (0.5f + 0.5f * MathF.Sin(_pulseT * 12f));
                DrawArc(dp, u.Radius + 5f, 0f, Mathf.Tau, 22, new Color(0.5f, 0.9f, 1f, a), 2f);
            }
            if (u.Role == Role.Ranged)
                DrawArc(dp, u.Radius + 2f, 0f, Mathf.Tau, 20, new Color(1f, 1f, 1f, 0.4f), 1.5f);
            if (u.AbilityCooldown <= 0f) // habilidad lista: brillo pulsante
            {
                Color glow = AbilityColor(u);
                glow.A = 0.4f + 0.3f * (0.5f + 0.5f * MathF.Sin(_pulseT * 6f));
                DrawArc(dp, u.Radius + 4f, 0f, Mathf.Tau, 22, glow, 2f);
            }
            DrawBar(new Vector2(dp.X - 11f, dp.Y + u.Radius + 3f), 22f, u.Hp / u.MaxHp, AllyHpColor);
        }

        // Enemigos (miran a la izquierda).
        Enemy? boss = null;
        foreach (Enemy e in _enemies)
        {
            if (e.IsBoss) boss = e;
            Color baseCol = e.IsBoss ? Darken(map.EnemyColor) : map.EnemyColor;
            Color body = e.HitFlash > 0f ? Colors.White : baseCol;
            DrawEnemy(e.Pos, e.Radius, body, e.Facing, e.IsBoss);
            if (!e.IsBoss)
                DrawBar(new Vector2(e.Pos.X - 9f, e.Pos.Y - e.Radius - 6f), 18f, e.Hp / e.MaxHp, EnemyHpColor);
        }

        foreach (Shot s in _shots)
        {
            if (s.DrawSize > 5f) // glow de la ráfaga
            {
                Color halo = s.Color;
                halo.A = 0.35f;
                DrawCircle(s.Pos, s.DrawSize * 1.7f, halo);
            }
            DrawCircle(s.Pos, s.DrawSize, s.Color);
        }

        foreach (Ring r in _rings)
        {
            float t = r.Age / r.Life;
            Color c = r.Color;
            c.A = Math.Clamp(1f - t, 0f, 1f);
            DrawArc(r.Pos, r.MaxRadius * t, 0f, Mathf.Tau, 24, c, 2f);
        }

        // Tajos melee: media luna que barre frente a la criatura.
        foreach (Slash sl in _slashes)
        {
            float t = sl.Age / sl.Life;
            Color c = sl.Color;
            c.A = Math.Clamp(1f - t, 0f, 1f);
            float ang = Mathf.Atan2(sl.Facing.Y, sl.Facing.X);
            DrawArc(sl.Pos, 9f + 7f * t, ang - 1.1f, ang + 1.1f, 12, c, 3.5f);
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

    private void Tri(Vector2 a, Vector2 b, Vector2 c, Color col) => DrawColoredPolygon(new[] { a, b, c }, col);

    private static Color Feat(Color body) => new(body.R * 0.6f, body.G * 0.6f, body.B * 0.6f);

    private void DrawEyes(Vector2 pos, float r, Vector2 f, bool enemy)
    {
        Vector2 perp = new Vector2(-f.Y, f.X);
        float eyeR = r * 0.24f;
        Vector2 c = pos + f * (r * 0.30f);
        Vector2 a = c + perp * (r * 0.30f);
        Vector2 b = c - perp * (r * 0.30f);
        DrawCircle(a, eyeR, Colors.White);
        DrawCircle(b, eyeR, Colors.White);
        Color pupil = enemy ? new Color(0.85f, 0.12f, 0.12f) : new Color(0.1f, 0.1f, 0.15f);
        Vector2 po = f * (eyeR * 0.45f);
        DrawCircle(a + po, eyeR * 0.5f, pupil);
        DrawCircle(b + po, eyeR * 0.5f, pupil);
    }

    private void DrawSpike(Vector2 basePos, Vector2 dir, float length, float halfWidth, Color col)
    {
        Vector2 d = dir.LengthSquared() > 0.0001f ? dir.Normalized() : Vector2.Up;
        Vector2 p = new Vector2(-d.Y, d.X);
        Tri(basePos + p * halfWidth, basePos - p * halfWidth, basePos + d * length, col);
    }

    // Silueta propia de cada especie (sobre el círculo base, para que el equipo encaje encima).
    private void DrawAlly(Vector2 pos, float r, Color body, Vector2 facing, Archetype arch)
    {
        Vector2 f = facing.LengthSquared() > 0.001f ? facing.Normalized() : Vector2.Right;
        Vector2 perp = new Vector2(-f.Y, f.X);
        Vector2 up = new Vector2(0f, -1f), down = new Vector2(0f, 1f);
        Color feat = Feat(body);

        switch (arch)
        {
            case Archetype.Charger: // esbelto: morro hacia adelante
                Tri(pos + perp * (r * 0.55f), pos - perp * (r * 0.55f), pos + f * (r * 1.6f), body);
                DrawCircle(pos, r, body);
                break;

            case Archetype.Bruiser: // dos cuernos
                DrawCircle(pos, r, body);
                DrawSpike(pos + perp * (r * 0.5f) + up * (r * 0.45f), up + perp * 0.3f, r * 0.8f, 2f, feat);
                DrawSpike(pos - perp * (r * 0.5f) + up * (r * 0.45f), up - perp * 0.3f, r * 0.8f, 2f, feat);
                break;

            case Archetype.Leaper: // patas traseras (resorte)
                DrawSpike(pos + perp * (r * 0.45f) + down * (r * 0.4f), down + perp * 0.4f, r * 0.85f, 2.5f, feat);
                DrawSpike(pos - perp * (r * 0.45f) + down * (r * 0.4f), down - perp * 0.4f, r * 0.85f, 2.5f, feat);
                DrawCircle(pos, r, body);
                break;

            case Archetype.Venomous: // antena + glándulas
                DrawCircle(pos, r, body);
                Vector2 a0 = pos + up * r, a1 = a0 + up * (r * 0.7f);
                DrawLine(a0, a1, feat, 2f);
                DrawCircle(a1, r * 0.2f, VenomColor);
                DrawCircle(pos + perp * (r * 0.4f) + down * (r * 0.2f), r * 0.16f, VenomColor);
                DrawCircle(pos - perp * (r * 0.4f) + down * (r * 0.2f), r * 0.16f, VenomColor);
                break;

            default: // Guardian: robusto con visera
                DrawCircle(pos, r, body);
                DrawLine(pos + perp * (r * 0.75f) + up * (r * 0.12f), pos - perp * (r * 0.75f) + up * (r * 0.12f), feat, 3.5f);
                break;
        }

        float ow = arch == Archetype.Guardian ? 2.5f : 1.5f;
        DrawArc(pos, r, 0f, Mathf.Tau, 22, new Color(0f, 0f, 0f, 0.35f), ow);
        DrawEyes(pos, r, f, enemy: false);
    }

    // Enemigo: blob con pinchos; el jefe añade cuernos.
    private void DrawEnemy(Vector2 pos, float r, Color body, Vector2 facing, bool boss)
    {
        Vector2 f = facing.LengthSquared() > 0.001f ? facing.Normalized() : Vector2.Left;
        Color feat = Feat(body);

        DrawCircle(pos, r, body);
        int spikes = boss ? 9 : 6;
        float len = boss ? 7f : 4f;
        for (int k = 0; k < spikes; k++)
        {
            float ang = k * Mathf.Tau / spikes;
            Vector2 d = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            DrawSpike(pos + d * r, d, len, 2f, feat);
        }
        if (boss)
        {
            Vector2 up = new Vector2(0f, -1f), perp = new Vector2(-f.Y, f.X);
            DrawSpike(pos + perp * (r * 0.5f) + up * (r * 0.55f), up + perp * 0.3f, r * 1.0f, 3f, feat);
            DrawSpike(pos - perp * (r * 0.5f) + up * (r * 0.55f), up - perp * 0.3f, r * 1.0f, 3f, feat);
        }
        DrawArc(pos, r, 0f, Mathf.Tau, 22, new Color(0f, 0f, 0f, 0.35f), 1.5f);
        DrawEyes(pos, r, f, enemy: true);
    }

    // Dibuja la anatomía equipada sobre la criatura (para que equipar se note), tintada
    // por la rareza más alta equipada.
    private void DrawEquipment(Vector2 pos, float r, PlayerUnit u, Vector2 facing)
    {
        Vector2 f = facing.LengthSquared() > 0.001f ? facing.Normalized() : Vector2.Right;
        Vector2 perp = new(-f.Y, f.X);
        Color a = u.Accent;

        // Caparazón/pelaje: arco grueso en la espalda.
        if (u.HasShell)
        {
            float back = MathF.Atan2(-f.Y, -f.X);
            DrawArc(pos, r + 2.5f, back - 0.95f, back + 0.95f, 14, a, 3f);
        }

        // Cola: línea que sale por detrás.
        if (u.HasTail)
            DrawLine(pos - f * r, pos - f * (r + 9f), a, 2.5f);

        // Garras/colmillos: 3 pinchos al frente.
        if (u.HasOffense)
        {
            for (int k = -1; k <= 1; k++)
            {
                Vector2 off = perp * (k * r * 0.45f);
                DrawLine(pos + f * (r * 0.8f) + off, pos + f * (r + 6f) + off, a, 2f);
            }
        }

        // Alas: dos triángulos arriba.
        if (u.HasWings)
        {
            Vector2 top = pos + new Vector2(0f, -r * 0.55f);
            DrawColoredPolygon(new[] { top, top + perp * 9f + new Vector2(0f, -3f), top + perp * 3f + new Vector2(0f, -8f) }, a);
            DrawColoredPolygon(new[] { top, top - perp * 9f + new Vector2(0f, -3f), top - perp * 3f + new Vector2(0f, -8f) }, a);
        }
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
        _ => u.ShotColor, // Dash, Jump, Cleave
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
