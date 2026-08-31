using System;

namespace AmbitionsInvaders
{
    public enum InvadersState { Ready, Playing, GameOver }

    public readonly struct InvadersInput
    {
        public readonly float X, Y;
        public readonly bool Fire;
        public InvadersInput(float x, float y, bool fire) { X = x; Y = y; Fire = fire; }
    }

    // Engine-independent, bounded and fixed-step: no Unity time, input or allocations in combat.
    public sealed class InvadersSimulation
    {
        public const float Width = 960, Height = 540, Bottom = 54, Top = 482;
        public const float ShipHalfWidth = 24, ShipHalfHeight = 14, ShipSpeed = 310;
        public const double StepSeconds = 1.0 / 120.0;
        public sealed class Enemy
        {
            public bool Active, Boss;
            public int Kind, Health, MaxHealth;
            public float X, Y, BaseY, Age, FireIn, Flash;
            public float Radius => Boss ? 55 : 27;
        }
        public sealed class Shot { public bool Active; public float X, Y, VX, VY; }
        public sealed class Spark { public bool Active; public float X, Y, VX, VY, Life; public int Kind; }

        public readonly Enemy[] Enemies = CreatePool<Enemy>(24);
        public readonly Shot[] Shots = CreatePool<Shot>(80);
        public readonly Shot[] HostileShots = CreatePool<Shot>(48);
        public readonly Spark[] Sparks = CreatePool<Spark>(96);
        public InvadersState State { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public int Shields { get; private set; }
        public int Score { get; private set; }
        public int Wave { get; private set; }
        public int Kills { get; private set; }
        public float Time { get; private set; }
        public float InvulnerableFor { get; private set; }
        public float WaveBannerFor { get; private set; }
        public int ShotsFired { get; private set; }
        public int HitSequence { get; private set; }
        public int ActiveEnemyCount { get { int n = 0; foreach (var e in Enemies) if (e.Active) n++; return n; } }
        private readonly int _seed;
        private Random _random;
        private double _accumulator;
        private float _fireIn, _spawnIn, _waveIn;
        private int _spawned;
        private bool _bossSpawned;

        private static T[] CreatePool<T>(int count) where T : new()
        { var pool = new T[count]; for (int i = 0; i < count; i++) pool[i] = new T(); return pool; }

        public InvadersSimulation(int seed = 1) { _seed = seed; Reset(); }

        public void Reset()
        {
            _random = new Random(_seed); State = InvadersState.Ready;
            X = 156; Y = 266; Shields = 3; Score = 0; Wave = 1; Kills = 0; Time = 0;
            InvulnerableFor = 0; WaveBannerFor = 2; ShotsFired = 0; HitSequence = 0;
            _accumulator = 0; _fireIn = 0; _spawnIn = .7f; _spawned = 0; _waveIn = -1; _bossSpawned = false;
            foreach (var enemy in Enemies) enemy.Active = false;
            foreach (var shot in Shots) shot.Active = false;
            foreach (var shot in HostileShots) shot.Active = false;
            foreach (var spark in Sparks) spark.Active = false;
        }

        public void Start()
        {
            if (State == InvadersState.Playing) return;
            Reset(); State = InvadersState.Playing;
        }

        public void Advance(double seconds, InvadersInput input)
        {
            if (State != InvadersState.Playing || double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) return;
            _accumulator += Math.Min(seconds, .25);
            while (_accumulator + 1e-12 >= StepSeconds && State == InvadersState.Playing)
            { Step((float)StepSeconds, input); _accumulator -= StepSeconds; }
        }

        private void Step(float dt, InvadersInput input)
        {
            Time += dt; InvulnerableFor = Math.Max(0, InvulnerableFor - dt); WaveBannerFor = Math.Max(0, WaveBannerFor - dt);
            float dx = FiniteAxis(input.X), dy = FiniteAxis(input.Y);
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length > 1) { dx /= length; dy /= length; }
            X = Clamp(X + dx * ShipSpeed * dt, 32, Width - 32);
            Y = Clamp(Y + dy * ShipSpeed * dt, Bottom + 22, Top - 22);
            _fireIn = Math.Max(0, _fireIn - dt);
            if (input.Fire && _fireIn <= 0)
            {
                if (Launch(Shots, X + ShipHalfWidth, Y, 740, 0)) ShotsFired++;
                _fireIn = .15f;
            }

            UpdateWave(dt);
            foreach (var enemy in Enemies)
            {
                if (!enemy.Active) continue;
                enemy.Age += dt; enemy.Flash = Math.Max(0, enemy.Flash - dt);
                float speed = 108 + Math.Min(140, (Wave - 1) * 9) + enemy.Kind * 12;
                if (enemy.Boss)
                {
                    enemy.X = Math.Max(790, enemy.X - 80 * dt);
                    enemy.Y = 268 + (float)Math.Sin(enemy.Age * .9f) * 125;
                }
                else
                {
                    enemy.X -= speed * dt;
                    float amplitude = enemy.Kind == 0 ? 10 : enemy.Kind == 1 ? 28 : enemy.Kind == 2 ? 55 : 36;
                    enemy.Y = Clamp(enemy.BaseY + (float)Math.Sin(enemy.Age * (1.4 + enemy.Kind * .45)) * amplitude, Bottom + 38, Top - 38);
                }
                if (enemy.X < -enemy.Radius)
                { enemy.Active = false; Damage(); if (State == InvadersState.GameOver) return; continue; }
                if (Math.Abs(X - enemy.X) < ShipHalfWidth + enemy.Radius * .78f && Math.Abs(Y - enemy.Y) < ShipHalfHeight + enemy.Radius * .78f)
                {
                    if (!enemy.Boss) { enemy.Active = false; Burst(enemy.X, enemy.Y, enemy.Kind); }
                    Damage(); if (State == InvadersState.GameOver) return;
                }
                if (!enemy.Active || enemy.X > 920 || enemy.X < X + 70 || (!enemy.Boss && Wave < 2)) continue;
                enemy.FireIn -= dt;
                if (enemy.FireIn <= 0)
                {
                    float vy = Clamp((Y - enemy.Y) * .6f, -105, 105);
                    Launch(HostileShots, enemy.X - enemy.Radius, enemy.Y, -215 - Math.Min(Wave * 6, 120), vy);
                    if (enemy.Boss)
                    {
                        Launch(HostileShots, enemy.X - enemy.Radius, enemy.Y - 16, -245, vy - 65);
                        Launch(HostileShots, enemy.X - enemy.Radius, enemy.Y + 16, -245, vy + 65);
                    }
                    enemy.FireIn = enemy.Boss ? .9f : Math.Max(1.15f, 3.5f - Wave * .12f);
                }
            }
            foreach (var shot in Shots)
            {
                if (!shot.Active) continue;
                float from = shot.X; shot.X += shot.VX * dt;
                Enemy nearest = null;
                foreach (var enemy in Enemies)
                {
                    if (!enemy.Active || !SweptHit(from, shot.X, shot.Y, enemy.X, enemy.Y, enemy.Radius * .83f, enemy.Radius * .85f)) continue;
                    if (nearest == null || enemy.X < nearest.X) nearest = enemy;
                }
                if (nearest != null)
                {
                    shot.Active = false; nearest.Health--; nearest.Flash = .09f;
                    if (nearest.Health <= 0)
                    {
                        nearest.Active = false; Kills++;
                        Score += nearest.Boss ? 2000 + Wave * 100 : 100 + nearest.Kind * 40;
                        Burst(nearest.X, nearest.Y, nearest.Kind);
                    }
                }
                if (shot.X > Width + 30) shot.Active = false;
            }
            foreach (var shot in HostileShots)
            {
                if (!shot.Active) continue;
                float from = shot.X; shot.X += shot.VX * dt; shot.Y += shot.VY * dt;
                if (SweptHit(from, shot.X, shot.Y, X, Y, ShipHalfWidth, ShipHalfHeight + 3))
                { shot.Active = false; Damage(); if (State == InvadersState.GameOver) return; }
                if (shot.X < -30 || shot.Y < Bottom || shot.Y > Top) shot.Active = false;
            }
            foreach (var spark in Sparks)
            {
                if (!spark.Active) continue;
                spark.X += spark.VX * dt; spark.Y += spark.VY * dt; spark.Life -= dt;
                if (spark.Life <= 0) spark.Active = false;
            }
        }

        private void UpdateWave(float dt)
        {
            if (_waveIn >= 0)
            {
                _waveIn -= dt;
                if (_waveIn <= 0)
                { Wave++; _spawned = 0; _bossSpawned = false; _spawnIn = .5f; _waveIn = -1; WaveBannerFor = 2; }
                return;
            }
            int quota = 8 + Math.Min(8, Wave * 2);
            _spawnIn -= dt;
            if (_spawned < quota && _spawnIn <= 0)
            {
                int kind = (_spawned + Wave - 1) % 4;
                float lane = 118 + (_spawned % 5) * 74;
                if (Spawn(kind, lane, false)) _spawned++;
                _spawnIn = Math.Max(.5f, 1.25f - Wave * .05f);
            }
            if (_spawned < quota || ActiveEnemyCount != 0) return;
            if (Wave % 4 == 0 && !_bossSpawned)
            { _bossSpawned = Spawn((Wave / 4 - 1) % 4, 270, true); WaveBannerFor = 2; return; }
            _waveIn = 1.4f;
        }

        private bool Spawn(int kind, float lane, bool boss)
        {
            foreach (var enemy in Enemies)
            {
                if (enemy.Active) continue;
                enemy.Active = true; enemy.Kind = kind; enemy.Boss = boss;
                enemy.X = Width + (boss ? 58 : 32); enemy.Y = enemy.BaseY = lane;
                enemy.Age = 0; enemy.Flash = 0; enemy.FireIn = 1.5f + (float)_random.NextDouble();
                enemy.Health = enemy.MaxHealth = boss ? 24 + Wave * 2 : kind == 0 ? 3 : kind == 1 ? 2 : 1;
                return true;
            }
            return false;
        }

        private static bool Launch(Shot[] pool, float x, float y, float vx, float vy)
        {
            foreach (var shot in pool)
            { if (shot.Active) continue; shot.Active = true; shot.X = x; shot.Y = y; shot.VX = vx; shot.VY = vy; return true; }
            return false;
        }

        private void Damage()
        {
            if (InvulnerableFor > 0 || State != InvadersState.Playing) return;
            Shields--; HitSequence++; InvulnerableFor = 1.6f; Burst(X, Y, 4);
            if (Shields <= 0) State = InvadersState.GameOver;
        }

        private void Burst(float x, float y, int kind)
        {
            int count = 0;
            foreach (var spark in Sparks)
            {
                if (spark.Active) continue;
                double angle = _random.NextDouble() * Math.PI * 2;
                float speed = 50 + (float)_random.NextDouble() * 120;
                spark.Active = true; spark.X = x; spark.Y = y; spark.Kind = kind;
                spark.VX = (float)Math.Cos(angle) * speed; spark.VY = (float)Math.Sin(angle) * speed;
                spark.Life = .25f + (float)_random.NextDouble() * .35f;
                if (++count == 12) break;
            }
        }

        private static bool SweptHit(float a, float b, float y, float x, float cy, float halfX, float halfY)
            => Math.Max(a, b) + 9 >= x - halfX && Math.Min(a, b) - 3 <= x + halfX && Math.Abs(y - cy) <= halfY;
        private static float FiniteAxis(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0 : Clamp(value, -1, 1);
        private static float Clamp(float value, float min, float max) => Math.Max(min, Math.Min(max, value));
    }
}
