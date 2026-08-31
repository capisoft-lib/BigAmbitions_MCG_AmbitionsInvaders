using AmbitionsInvaders;

int passed = 0;
void Check(bool value, string name) { if (!value) throw new Exception(name); passed++; Console.WriteLine("PASS " + name); }
InvadersSimulation Fresh() { var s = new InvadersSimulation(37); s.Start(); return s; }
void Frames(InvadersSimulation s, int count, InvadersInput input, double dt = 1d / 120) { for (int i = 0; i < count; i++) s.Advance(dt, input); }
InvadersSimulation.Enemy Enemy(InvadersSimulation s, int index, float x, float y, int hp = 1, bool boss = false)
{
    var e = s.Enemies[index]; e.Active = true; e.X = x; e.Y = e.BaseY = y; e.Kind = 2; e.Health = e.MaxHealth = hp; e.Boss = boss; e.Age = 0; e.FireIn = 999; return e;
}
var ready = new InvadersSimulation(); ready.Advance(1, new InvadersInput(1, 1, true));
Check(ready.State == InvadersState.Ready && ready.X == 156 && ready.ShotsFired == 0, "Ready screen cannot move or fire");
var a = Fresh(); var b = Fresh();
Frames(a, 60, new InvadersInput(1, 1, true), 1d / 60); Frames(b, 144, new InvadersInput(1, 1, true), 1d / 144);
Check(Math.Abs(a.X - b.X) < .01 && Math.Abs(a.Y - b.Y) < .01 && a.ShotsFired == b.ShotsFired && a.ActiveEnemyCount == b.ActiveEnemyCount, "Movement and firing agree at 60 and 144 FPS");
var diagonal = Fresh(); Frames(diagonal, 60, new InvadersInput(1, 1, false));
Check(Math.Abs(Math.Sqrt(Math.Pow(diagonal.X - 156, 2) + Math.Pow(diagonal.Y - 266, 2)) - 155) < .02, "Diagonal movement is normalized");
var clamp = Fresh(); Frames(clamp, 240, new InvadersInput(1, 1, false));
Check(Math.Abs(clamp.X - (156 + 620 / Math.Sqrt(2))) < .1 && clamp.Y == 460, "Horizontal movement remains continuous while vertical edge is clamped");
Frames(clamp, 240, new InvadersInput(1, 1, false));
Check(clamp.X == 928 && clamp.Y == 460, "Right and top arena bounds");
Frames(clamp, 400, new InvadersInput(-1, -1, false));
Check(clamp.X >= 32 && clamp.Y >= 76, "Left and bottom arena bounds");
var invalid = Fresh(); invalid.Advance(double.NaN, default); invalid.Advance(double.PositiveInfinity, default); invalid.Advance(-1, default);
invalid.Advance(1d / 120, new InvadersInput(float.NaN, float.PositiveInfinity, false));
Check(invalid.X == 156 && invalid.Y == 266, "Nonfinite time and controls cannot corrupt coordinates");
var stall = Fresh(); stall.Advance(99, new InvadersInput(0, 0, true));
Check(stall.Time <= .251 && stall.ShotsFired <= 2, "Stalls are bounded without catch-up projectile floods");
var fire = Fresh(); Frames(fire, 60, new InvadersInput(0, 0, true));
Check(fire.ShotsFired >= 3 && fire.ShotsFired <= 4 && fire.Shots.All(s => !s.Active || (s.VX > 0 && s.VY == 0 && s.Y == fire.Y)), "Held fire produces only horizontal rightward shots");
int before = fire.ShotsFired; Frames(fire, 30, default);
Check(fire.ShotsFired == before, "Releasing fire stops new shots");
var hit = Fresh(); var target = Enemy(hit, 0, 225, hit.Y, 2); Frames(hit, 24, new InvadersInput(0, 0, true));
Check(!target.Active && hit.Kills == 1 && hit.Score == 180, "Two hits defeat armored target and credit it once");
Frames(hit, 12, new InvadersInput(0, 0, true)); Check(hit.Score == 180, "Dead enemies cannot credit a second score");
var sweep = Fresh(); Enemy(sweep, 0, 490, sweep.Y); Enemy(sweep, 1, 310, sweep.Y);
var bullet = sweep.Shots[0]; bullet.Active = true; bullet.X = 200; bullet.Y = sweep.Y; bullet.VX = 50000;
sweep.Advance(1d / 120, default);
Check(sweep.Enemies[0].Active && !sweep.Enemies[1].Active, "Swept collision hits nearest enemy despite pool order and high velocity");
var damage = Fresh(); Enemy(damage, 0, damage.X, damage.Y); Enemy(damage, 1, damage.X, damage.Y);
damage.Advance(1d / 120, default);
Check(damage.Shields == 2 && damage.HitSequence == 1, "Simultaneous contacts consume one shield during invulnerability");
Frames(damage, 200, default); Enemy(damage, 0, -40, 150); damage.Advance(1d / 120, default);
Check(damage.Shields == 1, "A missed rival damages the shield after grace expires");
Frames(damage, 200, default); Enemy(damage, 0, -40, 150); damage.Advance(1d / 120, default);
Check(damage.State == InvadersState.GameOver && damage.Shields == 0, "Three distinct hits end the round");
float stopped = damage.Time; int endedScore = damage.Score; Frames(damage, 100, new InvadersInput(1, 1, true));
Check(damage.Time == stopped && damage.Score == endedScore && damage.Shields == 0, "Game over freezes gameplay and score");
damage.Start(); Check(damage.State == InvadersState.Playing && damage.Shields == 3 && damage.Score == 0 && damage.Wave == 1 && damage.ActiveEnemyCount == 0, "Retry resets combat, health and wave");
var hostile = Fresh(); var bad = hostile.HostileShots[0]; bad.Active = true; bad.X = 165; bad.Y = hostile.Y; bad.VX = -250;
hostile.Advance(1d / 120, default); Check(hostile.Shields == 2 && !bad.Active, "Hostile projectile is consumed on player collision");
var progression = Fresh(); bool sawBoss = false;
for (int i = 0; i < 120 * 100 && !sawBoss; i++)
{
    progression.Advance(1d / 120, default);
    foreach (var e in progression.Enemies) if (e.Active) { if (e.Boss) sawBoss = true; else e.Active = false; }
}
Check(sawBoss && progression.Wave == 4, "Every fourth wave produces a boss after the regular wave");
var bossEnemy = progression.Enemies.First(e => e.Active && e.Boss); Frames(progression, 500, default);
Check(bossEnemy.X >= 790 && bossEnemy.Y >= 140 && bossEnemy.Y <= 394, "Boss stays ahead and within the arena");
Check(progression.HostileShots.Any(s => s.Active), "Boss fires a spread");
Check(progression.Enemies.Length == 24 && progression.Shots.Length == 80 && progression.HostileShots.Length == 48 && progression.Sparks.Length == 96, "Combat memory remains bounded by entity pools");
Console.WriteLine($"COMPLETE {passed} assertions");
