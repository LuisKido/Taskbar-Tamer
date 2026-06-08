using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Simulation;

namespace TaskbarTamer.Game;

/// <summary>
/// Arena en vivo: visualiza el farming como un auto-battler continuo. Las criaturas
/// del jugador luchan según su posición en la formación: las <b>frontales atacan
/// melee</b> (cargan hacia el enemigo y golpean de cerca) y las de <b>retaguardia
/// atacan a distancia</b> (disparan poderes). Al limpiar una oleada avanza la Fase
/// y los enemigos escalan. Cosmético + progresión; la economía la lleva el farming
/// por tiempo.
/// </summary>
public partial class ArenaView : Control
{
    private const float RangedX = 22f;
    private const float MeleeX = 64f;
    private const float StopX = 92f;
    private const float EnemySpeed = 15f;
    private const float ProjectileSpeed = 280f;
    private const float MeleeSpeed = 78f;
    private const float AttackInterval = 0.55f;
    private const int WaveSize = 5;
    private const float BaseEnemyHp = 20f;

    private static readonly Color MeleeColor = new(0.45f, 0.72f, 1f);
    private static readonly Color RangedColor = new(0.45f, 0.95f, 0.85f);
    private static readonly Color EnemyColor = new(0.92f, 0.38f, 0.38f);
    private static readonly Color ShotColor = new(1f, 0.9f, 0.45f);
    private static readonly Color HpBackColor = new(0f, 0f, 0f, 0.5f);
    private static readonly Color HpColor = new(0.4f, 0.95f, 0.45f);

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
    }

    private sealed class Enemy
    {
        public Vector2 Pos;
        public float Hp;
        public float MaxHp;
        public float Radius;
    }

    private sealed class Shot
    {
        public Vector2 Pos;
        public Enemy Target = null!;
        public float Damage;
        public bool Crit;
    }

    // Número de daño flotante estilo Ragnarok Online: salta sobre el objetivo,
    // describe un arco hacia arriba y se desvanece. Los críticos son más grandes.
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

    /// <summary>Se emite al avanzar de fase, para que el panel principal refresque etiquetas.</summary>
    public event Action? StageAdvanced;

    private GameSession _session = null!;
    private readonly List<PlayerUnit> _units = new();
    private readonly List<Enemy> _enemies = new();
    private readonly List<Shot> _shots = new();
    private readonly List<DamageNumber> _damageNumbers = new();
    private double _saveAccum;
    private bool _dirty;

    public void Begin(GameSession session)
    {
        _session = session;
        CustomMinimumSize = new Vector2(0, 132);
        BuildUnits();
        SpawnWave();
    }

    // Construye las unidades a partir de la formación: frontal → melee, retaguardia → distancia.
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
        return new PlayerUnit
        {
            Role = role,
            LaneIndex = laneIndex,
            LaneCount = laneCount,
            Damage = Math.Max(4f, s.Attack * 0.5f),
            CritChance = Math.Clamp(0.12f + s.CritChance / 10000f, 0f, 0.6f),
            AttackTimer = GD.Randf() * AttackInterval,
            Color = role == Role.Melee ? MeleeColor : RangedColor,
            Radius = role == Role.Melee ? 9f : 8f,
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
        float hp = BaseEnemyHp * (1f + (_session.Stage - 1) * 0.18f);

        for (int i = 0; i < WaveSize; i++)
        {
            _enemies.Add(new Enemy
            {
                Pos = new Vector2(w - 14f + i * 18f + GD.Randf() * 16f, 16f + GD.Randf() * (h - 32f)),
                Hp = hp,
                MaxHp = hp,
                Radius = 7f + GD.Randf() * 3f,
            });
        }
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree())
            return;

        float dt = (float)delta;

        UpdateUnits(dt);
        UpdateShots(dt);
        AdvanceEnemies(dt);
        UpdateDamageNumbers(dt);

        if (_enemies.Count == 0)
        {
            _session.AdvanceStage();
            BuildUnits();
            SpawnWave();
            _dirty = true;
            StageAdvanced?.Invoke();
        }

        // Guardado con throttle para no escribir el save cada oleada.
        _saveAccum += delta;
        if (_dirty && _saveAccum > 3.0)
        {
            _session.Save();
            _dirty = false;
            _saveAccum = 0;
        }

        QueueRedraw();
    }

    private void UpdateUnits(float dt)
    {
        foreach (PlayerUnit u in _units)
        {
            Vector2 home = HomeFor(u);
            Enemy? target = NearestEnemy(u.Pos);

            if (u.Role == Role.Ranged)
            {
                u.Pos = home; // la retaguardia se mantiene en su sitio
                u.AttackTimer -= dt;
                if (target is not null && u.AttackTimer <= 0f)
                {
                    bool crit = GD.Randf() < u.CritChance;
                    _shots.Add(new Shot
                    {
                        Pos = u.Pos,
                        Target = target,
                        Damage = crit ? u.Damage * 2f : u.Damage,
                        Crit = crit,
                    });
                    u.AttackTimer = AttackInterval;
                }
                continue;
            }

            // Melee: carga hacia el enemigo y golpea de cerca; si no hay, vuelve a casa.
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
                    float dmg = crit ? u.Damage * 2f : u.Damage;
                    target.Hp -= dmg;
                    SpawnDamageNumber(target.Pos, (int)MathF.Round(dmg), crit);
                    if (target.Hp <= 0f)
                        _enemies.Remove(target);
                    u.AttackTimer = AttackInterval;
                }
            }
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
                s.Target.Hp -= s.Damage;
                SpawnDamageNumber(s.Target.Pos, (int)MathF.Round(s.Damage), s.Crit);
                if (s.Target.Hp <= 0f)
                    _enemies.Remove(s.Target);
                _shots.RemoveAt(i);
            }
        }
    }

    private void AdvanceEnemies(float dt)
    {
        foreach (Enemy e in _enemies)
        {
            float nx = Math.Max(StopX, e.Pos.X - EnemySpeed * dt);
            e.Pos = new Vector2(nx, e.Pos.Y);
        }
    }

    private void SpawnDamageNumber(Vector2 at, int amount, bool crit)
    {
        _damageNumbers.Add(new DamageNumber
        {
            Pos = at + new Vector2((GD.Randf() - 0.5f) * 6f, -10f),
            Vel = new Vector2((GD.Randf() - 0.5f) * 26f, -46f), // salta hacia arriba
            Age = 0f,
            Life = crit ? 0.85f : 0.7f,
            Text = amount.ToString(),
            Color = crit ? new Color(1f, 0.78f, 0.2f) : new Color(1f, 1f, 1f),
            Scale = crit ? 1.6f : 1f,
        });
    }

    private void UpdateDamageNumbers(float dt)
    {
        for (int i = _damageNumbers.Count - 1; i >= 0; i--)
        {
            DamageNumber d = _damageNumbers[i];
            d.Pos += d.Vel * dt;
            d.Vel = new Vector2(d.Vel.X, d.Vel.Y + 90f * dt); // gravedad: arco hacia arriba y caída
            d.Age += dt;
            if (d.Age >= d.Life)
                _damageNumbers.RemoveAt(i);
        }
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.08f, 0.08f, 0.10f));

        foreach (PlayerUnit u in _units)
        {
            DrawCircle(u.Pos, u.Radius, u.Color);
            // Anillo en las unidades a distancia para distinguirlas de las melee.
            if (u.Role == Role.Ranged)
                DrawArc(u.Pos, u.Radius + 2f, 0f, Mathf.Tau, 20, new Color(1f, 1f, 1f, 0.5f), 1.5f);
        }

        foreach (Enemy e in _enemies)
        {
            DrawCircle(e.Pos, e.Radius, EnemyColor);
            float frac = Math.Clamp(e.Hp / e.MaxHp, 0f, 1f);
            var barPos = new Vector2(e.Pos.X - 9f, e.Pos.Y - e.Radius - 6f);
            DrawRect(new Rect2(barPos, new Vector2(18f, 3f)), HpBackColor);
            DrawRect(new Rect2(barPos, new Vector2(18f * frac, 3f)), HpColor);
        }

        foreach (Shot s in _shots)
            DrawCircle(s.Pos, 3f, ShotColor);

        Font font = GetThemeDefaultFont();
        DrawDamageNumbers(font);

        DrawString(font, new Vector2(8, 18), $"Fase {_session.Stage}",
            HorizontalAlignment.Left, -1, 14, new Color(1f, 1f, 1f, 0.85f));
    }

    // Dibuja cada número con sus dígitos escalonados verticalmente (estilo Ragnarok),
    // desvaneciéndose con la edad.
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
                var pos = new Vector2(d.Pos.X + i * digitWidth, d.Pos.Y + yStagger);
                DrawString(font, pos, d.Text[i].ToString(),
                    HorizontalAlignment.Left, -1, fontSize, color);
            }
        }
    }
}
