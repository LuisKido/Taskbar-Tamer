using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Simulation;

namespace TaskbarTamer.Game;

/// <summary>
/// Arena en vivo: auto-battler continuo que visualiza el progreso. Las criaturas
/// frontales atacan melee y las de retaguardia a distancia. Los enemigos contraatacan
/// y las criaturas pueden caer; si caen todas, te retiras al inicio del mapa. Cada 10
/// fases aparece un jefe y se cambia de mapa (3 mapas que rotan). Incluye efectos:
/// destellos, anillos de impacto, números de daño (estilo RO) y banners.
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
    private const int MapBand = 10; // fases por mapa

    private static readonly Color RangedColor = new(0.45f, 0.95f, 0.85f);
    private static readonly Color MeleeColor = new(0.45f, 0.72f, 1f);
    private static readonly Color ShotColor = new(1f, 0.9f, 0.45f);
    private static readonly Color HpBackColor = new(0f, 0f, 0f, 0.55f);
    private static readonly Color EnemyHpColor = new(0.95f, 0.45f, 0.45f);
    private static readonly Color AllyHpColor = new(0.45f, 0.9f, 0.6f);

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

    private sealed class PlayerUnit
    {
        public Vector2 Pos;
        public Role Role;
        public int LaneIndex;
        public int LaneCount;
        public float Damage;
        public float CritChance;
        public float AttackTimer;
        public Color Color;
        public float Radius;
        public float MaxHp;
        public float Hp;
        public float HitFlash;
        public bool Downed => Hp <= 0f;
    }

    private sealed class Enemy
    {
        public Vector2 Pos;
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

    private sealed class Ring
    {
        public Vector2 Pos;
        public float Age;
        public float Life;
        public float MaxRadius;
        public Color Color;
    }

    /// <summary>Se emite al cambiar de fase, para que el panel principal refresque etiquetas.</summary>
    public event Action? StageAdvanced;

    private GameSession _session = null!;
    private readonly List<PlayerUnit> _units = new();
    private readonly List<Enemy> _enemies = new();
    private readonly List<Shot> _shots = new();
    private readonly List<DamageNumber> _damageNumbers = new();
    private readonly List<Ring> _rings = new();
    private double _saveAccum;
    private bool _dirty;
    private string _banner = "";
    private float _bannerTimer;
    private float _shake;

    private MapDef CurrentMap => Maps[((_session.Stage - 1) / MapBand) % Maps.Length];
    private bool IsBossStage => _session.Stage % MapBand == 0;

    public void Begin(GameSession session)
    {
        _session = session;
        CustomMinimumSize = new Vector2(0, 150);
        BuildUnits();
        SpawnWave();
    }

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
            Radius = role == Role.Melee ? 9f : 8f,
            MaxHp = maxHp,
            Hp = maxHp,
        };
    }

    private Vector2 HomeFor(PlayerUnit u)
    {
        float h = Math.Max(Size.Y, 100f);
        float x = u.Role == Role.Ranged ? RangedX : MeleeX;
        return new Vector2(x, h * (u.LaneIndex + 1) / (u.LaneCount + 1));
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
                Pos = new Vector2(w - 30f, h / 2f),
                Hp = bossHp,
                MaxHp = bossHp,
                Radius = 20f,
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
                Pos = new Vector2(w - 14f + i * 18f + GD.Randf() * 16f, 16f + GD.Randf() * (h - 32f)),
                Hp = hp,
                MaxHp = hp,
                Radius = 7f + GD.Randf() * 3f,
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
        UpdateRings(dt);

        if (_bannerTimer > 0f) _bannerTimer -= dt;
        if (_shake > 0f) _shake = Math.Max(0f, _shake - dt * 18f);

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
        BuildUnits();   // cura al equipo al avanzar
        SpawnWave();
        _dirty = true;
        StageAdvanced?.Invoke();
    }

    private void Retreat()
    {
        _session.RetreatToMapStart();
        ShowBanner("💀 ¡DERROTA! Te retiras al inicio del mapa");
        SpawnShake(7f);
        BuildUnits();
        SpawnWave();
        _dirty = true;
        StageAdvanced?.Invoke();
    }

    private void UpdateUnits(float dt)
    {
        foreach (PlayerUnit u in _units)
        {
            if (u.HitFlash > 0f) u.HitFlash -= dt;
            if (u.Downed) continue;

            Vector2 home = HomeFor(u);
            Enemy? target = NearestEnemy(u.Pos);

            if (u.Role == Role.Ranged)
            {
                u.Pos = home;
                u.AttackTimer -= dt;
                if (target is not null && u.AttackTimer <= 0f)
                {
                    bool crit = GD.Randf() < u.CritChance;
                    _shots.Add(new Shot { Pos = u.Pos, Target = target, Damage = crit ? u.Damage * 2f : u.Damage, Crit = crit });
                    u.AttackTimer = PlayerAttackInterval;
                }
                continue;
            }

            if (target is null)
            {
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
                    HitEnemy(target, crit ? u.Damage * 2f : u.Damage, crit);
                    u.AttackTimer = PlayerAttackInterval;
                }
            }
        }
    }

    private void UpdateEnemies(float dt)
    {
        foreach (Enemy e in _enemies)
        {
            if (e.HitFlash > 0f) e.HitFlash -= dt;

            PlayerUnit? target = NearestAlivePlayer(e.Pos);
            if (target is null)
                continue;

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

    private void HitEnemy(Enemy target, float dmg, bool crit)
    {
        target.Hp -= dmg;
        target.HitFlash = 0.12f;
        SpawnDamageNumber(target.Pos, (int)MathF.Round(dmg), crit, crit ? new Color(1f, 0.78f, 0.2f) : Colors.White);
        SpawnRing(target.Pos, new Color(1f, 1f, 1f, 0.8f), 10f, 0.2f);
        if (target.Hp <= 0f)
        {
            SpawnRing(target.Pos, EnemyHpColor, target.IsBoss ? 34f : 18f, 0.4f);
            if (target.IsBoss) SpawnShake(5f);
            _enemies.Remove(target);
        }
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
            if (s.Pos.DistanceTo(s.Target.Pos) <= s.Target.Radius + 2f)
            {
                HitEnemy(s.Target, s.Damage, s.Crit);
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

        // Aliados.
        foreach (PlayerUnit u in _units)
        {
            if (u.Downed) continue;
            Color col = u.HitFlash > 0f ? new Color(1f, 0.6f, 0.6f) : u.Color;
            DrawCircle(u.Pos, u.Radius, col);
            if (u.Role == Role.Ranged)
                DrawArc(u.Pos, u.Radius + 2f, 0f, Mathf.Tau, 20, new Color(1f, 1f, 1f, 0.5f), 1.5f);
            DrawBar(new Vector2(u.Pos.X - 10f, u.Pos.Y + u.Radius + 3f), 20f, u.Hp / u.MaxHp, AllyHpColor);
        }

        // Enemigos.
        foreach (Enemy e in _enemies)
        {
            Color baseCol = e.IsBoss ? Darken(map.EnemyColor) : map.EnemyColor;
            Color col = e.HitFlash > 0f ? Colors.White : baseCol;
            DrawCircle(e.Pos, e.Radius, col);
            float barW = e.IsBoss ? 44f : 18f;
            DrawBar(new Vector2(e.Pos.X - barW / 2f, e.Pos.Y - e.Radius - 6f), barW, e.Hp / e.MaxHp, EnemyHpColor);
        }

        foreach (Shot s in _shots)
            DrawCircle(s.Pos, 3f, ShotColor);

        foreach (Ring r in _rings)
        {
            float t = r.Age / r.Life;
            Color c = r.Color;
            c.A = Math.Clamp(1f - t, 0f, 1f);
            DrawArc(r.Pos, r.MaxRadius * t, 0f, Mathf.Tau, 24, c, 2f);
        }

        Font font = GetThemeDefaultFont();
        DrawDamageNumbers(font);

        DrawString(font, new Vector2(8, 18), $"Fase {_session.Stage}", HorizontalAlignment.Left, -1, 14, new Color(1f, 1f, 1f, 0.9f));
        DrawString(font, new Vector2(8, 33), map.Name, HorizontalAlignment.Left, -1, 11, new Color(1f, 1f, 1f, 0.5f));

        if (_bannerTimer > 0f)
        {
            float a = Math.Clamp(_bannerTimer / 2.5f, 0f, 1f);
            DrawString(font, new Vector2(0, Size.Y / 2f), _banner, HorizontalAlignment.Center, Size.X, 16, new Color(1f, 0.95f, 0.7f, a));
        }
    }

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
