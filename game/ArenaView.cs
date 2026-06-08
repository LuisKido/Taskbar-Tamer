using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using TaskbarTamer.Core.Model;
using TaskbarTamer.Core.Simulation;

namespace TaskbarTamer.Game;

/// <summary>
/// Arena en vivo: visualiza el farming como un auto-battler continuo. Las criaturas
/// del jugador (círculos) lanzan poderes a hordas de enemigos; al limpiar una oleada
/// avanza la <b>Fase</b> y los enemigos se vuelven más fuertes. El daño escala con el
/// poder del equipo. Es cosmético + progresión; la economía (loot/esencia) la lleva el
/// farming por tiempo.
/// </summary>
public partial class ArenaView : Control
{
    private const float PlayerX = 26f;
    private const float StopX = 84f;
    private const float EnemySpeed = 16f;
    private const float ProjectileSpeed = 280f;
    private const float AttackInterval = 0.5f;
    private const int WaveSize = 5;
    private const float BaseEnemyHp = 20f;

    private static readonly Color PlayerColor = new(0.45f, 0.72f, 1f);
    private static readonly Color EnemyColor = new(0.92f, 0.38f, 0.38f);
    private static readonly Color ShotColor = new(1f, 0.9f, 0.45f);
    private static readonly Color HpBackColor = new(0f, 0f, 0f, 0.5f);
    private static readonly Color HpColor = new(0.4f, 0.95f, 0.45f);

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
    private readonly List<Enemy> _enemies = new();
    private readonly List<Shot> _shots = new();
    private readonly List<DamageNumber> _damageNumbers = new();
    private float _attackTimer;
    private float _playerDamage = 4f;
    private float _critChance = 0.12f;
    private int _shooters = 1;
    private double _saveAccum;
    private bool _dirty;

    public void Begin(GameSession session)
    {
        _session = session;
        CustomMinimumSize = new Vector2(0, 132);
        RecomputeTeam();
        SpawnWave();
    }

    private void RecomputeTeam()
    {
        Setup? setup = _session.BuildPlayerSetup();
        List<Creature> fighters = setup is not null
            ? setup.All.Take(3).ToList()
            : _session.State.Roster.Take(3).ToList();

        _shooters = Math.Max(1, fighters.Count);

        float attack = 0;
        float critBp = 0;
        foreach (Creature c in fighters)
        {
            Stats s = StatsResolver.Resolve(c, SetRegistry.Empty).Stats;
            attack += s.Attack;
            critBp += s.CritChance;
        }
        _playerDamage = Math.Max(4f, attack * 0.5f);
        critBp /= _shooters;
        _critChance = Math.Clamp(0.12f + critBp / 10000f, 0f, 0.6f); // 12% base + crítico del equipo
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

        // Las criaturas atacan al enemigo más cercano por intervalos.
        _attackTimer -= dt;
        if (_attackTimer <= 0f && _enemies.Count > 0)
        {
            FireVolley();
            _attackTimer = AttackInterval;
        }

        UpdateShots(dt);
        AdvanceEnemies(dt);
        UpdateDamageNumbers(dt);

        if (_enemies.Count == 0)
        {
            _session.AdvanceStage();
            RecomputeTeam();
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

    private void FireVolley()
    {
        var players = PlayerPositions();
        for (int i = 0; i < players.Count; i++)
        {
            Enemy? target = NearestEnemy(players[i]);
            if (target is null)
                break;
            bool crit = GD.Randf() < _critChance;
            _shots.Add(new Shot
            {
                Pos = players[i],
                Target = target,
                Damage = crit ? _playerDamage * 2f : _playerDamage,
                Crit = crit,
            });
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

    private void AdvanceEnemies(float dt)
    {
        foreach (Enemy e in _enemies)
        {
            float nx = Math.Max(StopX, e.Pos.X - EnemySpeed * dt);
            e.Pos = new Vector2(nx, e.Pos.Y);
        }
    }

    private List<Vector2> PlayerPositions()
    {
        float h = Math.Max(Size.Y, 100f);
        var list = new List<Vector2>(_shooters);
        for (int i = 0; i < _shooters; i++)
            list.Add(new Vector2(PlayerX, h * (i + 1) / (_shooters + 1)));
        return list;
    }

    public override void _Draw()
    {
        // Fondo de la arena.
        DrawRect(new Rect2(Vector2.Zero, Size), new Color(0.08f, 0.08f, 0.10f));

        foreach (Vector2 p in PlayerPositions())
            DrawCircle(p, 9f, PlayerColor);

        foreach (Enemy e in _enemies)
        {
            DrawCircle(e.Pos, e.Radius, EnemyColor);
            // Barra de vida sobre el enemigo.
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
                // Escalonado: cada dígito sube un poco respecto al anterior (look RO).
                float yStagger = -MathF.Sin((i + 1) * 0.9f) * 2.5f * d.Scale;
                var pos = new Vector2(d.Pos.X + i * digitWidth, d.Pos.Y + yStagger);
                DrawString(font, pos, d.Text[i].ToString(),
                    HorizontalAlignment.Left, -1, fontSize, color);
            }
        }
    }
}
