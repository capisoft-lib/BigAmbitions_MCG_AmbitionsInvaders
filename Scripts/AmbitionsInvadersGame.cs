using System;
using Capisoft.Lib.BaComputerGames;
using UnityEngine;
using UnityEngine.UI;

namespace AmbitionsInvaders
{
    public sealed class AmbitionsInvadersGame : ComputerGameBehaviour
    {
        public static int LiveViews { get; private set; }
        public InvadersSimulation Simulation { get; private set; }
        public override Camera Camera => _camera;
        internal Func<InvadersInput> ReadControls = ReadKeyboard;
        private Camera _camera;
        private Font _font;
        private InvadersAssets _assets;
        private RectTransform _root, _bill, _flame, _overlay;
        private RectTransform[] _stars, _skyline, _borders, _shots, _hostile, _sparks, _shieldBars;
        private EnemyView[] _enemies;
        private Text _score, _record, _wave, _title, _subtitle, _instructions, _waveBanner, _bossName;
        private RectTransform _bossBar, _bossPanel, _portraits;
        private Image _damage;
        private bool _built;
        private float _stateSeconds, _visualTime;
        private InvadersState _lastState = (InvadersState)(-1);
        private int _lastScore = -1, _lastShields = -1, _lastWave = -1;
        private long _lastRecord = -1;
        private readonly Color _ink = new Color32(12, 15, 32, 255);
        private readonly Color _cream = new Color32(230, 239, 229, 255);
        private readonly Color _mint = new Color32(188, 240, 135, 255);
        private readonly Color[] _rivalColors = {
            new Color32(239, 80, 102, 255), new Color32(250, 205, 104, 255),
            new Color32(160, 186, 244, 255), new Color32(221, 146, 86, 255), new Color32(188, 240, 135, 255)
        };

        private sealed class EnemyView { public RectTransform Root, Health; public Image Portrait; public int Kind = -1; }
        private string T(string key, string fallback) => Context.Text("invaders_" + key, fallback);

        protected override void OnInitialize()
        {
            _assets = Context.Assets as InvadersAssets;
            if (_assets == null || _assets.Count != 4) throw new InvalidOperationException("Ambitions Invaders requires its four rival sprites.");
            Simulation = new InvadersSimulation(Environment.TickCount);
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildView(); _built = true; LiveViews++; Draw();
        }

        private void BuildView()
        {
            var cameraObject = new GameObject("InvadersCamera", typeof(Camera));
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.localPosition = new Vector3(480, 270, -10);
            _camera = cameraObject.GetComponent<Camera>();
            _camera.orthographic = true; _camera.orthographicSize = 270;
            _camera.clearFlags = CameraClearFlags.SolidColor; _camera.backgroundColor = _ink;
            _camera.nearClipPlane = .1f; _camera.farClipPlane = 20; _camera.cullingMask = 1 << 5;
            _camera.allowHDR = false; _camera.allowMSAA = false;
            var canvasObject = new GameObject("InvadersCanvas", typeof(RectTransform), typeof(Canvas), typeof(RectMask2D));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = _camera;
            _root = (RectTransform)canvasObject.transform; _root.sizeDelta = new Vector2(960, 540); _root.pivot = Vector2.zero;
            Paint(_root, "Night", 0, 0, 960, 540, _ink);
            Paint(_root, "Horizon", 0, 54, 960, 163, new Color32(28, 28, 57, 255));
            for (int i = 0; i < 8; i++) Paint(_root, "GridLine", 0, 54 + i * 53, 960, 1, new Color32(27, 35, 58, 255));
            _stars = new RectTransform[48];
            for (int i = 0; i < _stars.Length; i++)
                _stars[i] = Paint(_root, "PassingLight", i * 193 % 960, 68 + i * 67 % 397, i % 4 == 0 ? 7 : 2, 2, new Color32(84, 113, 135, 255));
            _skyline = new RectTransform[14];
            for (int i = 0; i < _skyline.Length; i++)
            {
                float height = 50 + i * 47 % 109;
                var building = Paint(_root, "DistantOffice", i * 82, 54, 54, height, new Color32(19, 27, 47, 255));
                Paint(building, "Rooftop", 0, height - 2, 54, 2, new Color32(47, 68, 85, 255));
                for (int y = 0; y < (int)height / 24; y++)
                    for (int x = 0; x < 3; x++) Paint(building, "OfficeWindow", 7 + x * 16, 9 + y * 24, 4, 6, new Color32(64, 81, 88, 255));
                _skyline[i] = building;
            }
            _shots = new RectTransform[Simulation.Shots.Length];
            for (int i = 0; i < _shots.Length; i++)
            {
                _shots[i] = Paint(_root, "CashLaser", 0, 0, 22, 4, new Color32(252, 215, 117, 255));
            }
            _hostile = new RectTransform[Simulation.HostileShots.Length];
            for (int i = 0; i < _hostile.Length; i++) _hostile[i] = Paint(_root, "RivalShot", 0, 0, 10, 6, new Color32(255, 104, 144, 255));
            _enemies = new EnemyView[Simulation.Enemies.Length];
            for (int i = 0; i < _enemies.Length; i++)
            {
                var root = Paint(_root, "Rival", 0, 0, 72, 82, Color.clear);
                var portrait = Paint(root, "Portrait", 0, 8, 72, 72, Color.white).GetComponent<Image>();
                portrait.preserveAspect = true;
                Paint(root, "HealthTrack", 8, 2, 56, 3, new Color32(57, 52, 71, 255));
                var health = Paint(root, "Health", 8, 2, 56, 3, _mint);
                _enemies[i] = new EnemyView { Root = root, Portrait = portrait, Health = health };
            }
            _sparks = new RectTransform[Simulation.Sparks.Length];
            for (int i = 0; i < _sparks.Length; i++) _sparks[i] = Paint(_root, "Debris", 0, 0, 4, 4, _mint);

            // Same 48x28 code-native banknote, inset and 100 label as Flappy Ambition.
            _bill = Paint(_root, "FlyingBanknote", 156, 266, 48, 28, new Color32(190, 240, 133, 255));
            _bill.pivot = new Vector2(.5f, .5f);
            _flame = Paint(_bill, "EngineExhaust", -18, 9, 18, 10, new Color32(248, 190, 85, 255));
            Paint(_bill, "BanknoteInset", 3, 3, 42, 22, new Color32(83, 162, 104, 255));
            Label(_bill, "100", 2, 1, 44, 26, 16, new Color32(13, 30, 47, 255), TextAnchor.MiddleCenter);

            _damage = Paint(_root, "DamageFlash", 0, 54, 960, 428, Color.clear).GetComponent<Image>();
            Paint(_root, "Hud", 0, 482, 960, 58, _ink);
            Paint(_root, "HudRule", 0, 481, 960, 2, new Color32(62, 87, 103, 255));
            _score = Label(_root, "", 22, 489, 275, 44, 23, _cream);
            _record = Label(_root, "", 317, 489, 275, 44, 18, new Color32(151, 171, 181, 255));
            _wave = Label(_root, "", 606, 489, 156, 44, 20, _cream);
            Label(_root, T("shield", "SHIELD"), 791, 518, 145, 17, 12, new Color32(151, 171, 181, 255));
            _shieldBars = new RectTransform[3];
            for (int i = 0; i < 3; i++) _shieldBars[i] = Paint(_root, "ShieldCell", 791 + i * 47, 497, 38, 12, _mint);
            Paint(_root, "Footer", 0, 0, 960, 54, _ink);
            Paint(_root, "FooterRule", 0, 53, 960, 2, new Color32(62, 87, 103, 255));
            Label(_root, "AMBITIONS INVADERS", 22, 10, 295, 31, 20, _mint);
            Label(_root, T("footer", "ARROWS / WASD : MOVE    SPACE : FIRE"), 321, 10, 617, 31, 14, new Color32(151, 171, 181, 255), TextAnchor.MiddleRight);
            _waveBanner = Label(_root, "", 230, 428, 500, 42, 23, _mint, TextAnchor.MiddleCenter);
            _bossPanel = Paint(_root, "BossStatus", 300, 418, 360, 51, new Color32(12, 15, 32, 230));
            _bossName = Label(_bossPanel, "", 8, 17, 344, 28, 17, _cream, TextAnchor.MiddleCenter);
            Paint(_bossPanel, "BossTrack", 12, 9, 336, 4, new Color32(68, 42, 64, 255));
            _bossBar = Paint(_bossPanel, "BossHealth", 12, 9, 336, 4, _rivalColors[0]);

            _overlay = Paint(_root, "GameCard", 157, 97, 646, 339, new Color32(16, 20, 39, 248));
            Paint(_overlay, "CardTop", 0, 335, 646, 4, _mint);
            Label(_overlay, T("eyebrow", "A HOSTILE TAKEOVER. WITH LASERS."), 24, 295, 598, 28, 15, new Color32(151, 171, 181, 255), TextAnchor.MiddleCenter);
            _title = Label(_overlay, "AMBITIONS INVADERS", 20, 227, 606, 65, 42, _mint, TextAnchor.MiddleCenter);
            _subtitle = Label(_overlay, "", 24, 192, 598, 35, 18, _cream, TextAnchor.MiddleCenter);
            _portraits = Paint(_overlay, "RivalLineup", 33, 87, 580, 100, Color.clear);
            for (int i = 0; i < 4; i++)
            {
                var portrait = Paint(_portraits, "RivalPreview", 33 + i * 145, 21, 72, 74, Color.white).GetComponent<Image>();
                portrait.sprite = _assets[i]; portrait.preserveAspect = true;
                string name = i == 0 ? "HUANG GUO" : i == 1 ? "INGRID" : i == 2 ? "JESSICA" : "THIERRY";
                Label(_portraits, name, i * 145, 0, 140, 20, 12, _rivalColors[i], TextAnchor.MiddleCenter);
            }
            _instructions = Label(_overlay, "", 22, 17, 602, 67, 17, _cream, TextAnchor.MiddleCenter);
            _borders = new RectTransform[4];
            for (int i = 0; i < 4; i++)
            {
                _borders[i] = Paint(_root, "ScreenBorder", 0, 0, 1, 1, _ink);
                var image = _borders[i].GetComponent<Image>(); image.maskable = false; image.RecalculateClipping();
            }
            SetBorder(1); SetLayers(transform);
        }

        public override void SetScreenResolution(int width, int height)
        {
            if (!_built) return;
            _camera.orthographicSize = Math.Max(270, 480 * Math.Max(1, height) / (float)Math.Max(1, width));
            SetBorder(2 * _camera.orthographicSize / Math.Max(1, height));
        }

        protected override void OnTick(ComputerGameFrame frame)
        {
            float dt = float.IsNaN(frame.DeltaSeconds) || float.IsInfinity(frame.DeltaSeconds) ? 0 : Mathf.Clamp(frame.DeltaSeconds, 0, .25f);
            _stateSeconds += dt; _visualTime += dt;
            if (Simulation.State != InvadersState.Playing)
            {
                bool start = frame.PrimaryPressed || frame.RestartPressed || Input.GetKeyDown(KeyCode.Return);
                if (start && _stateSeconds >= .3f) { Context.BeginRound(); Simulation.Start(); _stateSeconds = 0; }
            }
            else
            {
                var previous = Simulation.State;
                var input = ReadControls();
                Simulation.Advance(dt, new InvadersInput(input.X, input.Y, input.Fire || frame.PrimaryPressed));
                if (previous == InvadersState.Playing && Simulation.State == InvadersState.GameOver)
                { Context.CompleteRound(Simulation.Score, Simulation.Wave); _stateSeconds = 0; }
            }
            Draw();
        }

        private static InvadersInput ReadKeyboard()
        {
            float x = (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) ? 1 : 0) -
                (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q) ? 1 : 0);
            float y = (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Z) ? 1 : 0) -
                (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) ? 1 : 0);
            return new InvadersInput(x, y, Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0));
        }

        private void Draw()
        {
            bool playing = Simulation.State == InvadersState.Playing;
            float scroll = playing ? Simulation.Time : _visualTime;
            for (int i = 0; i < _stars.Length; i++)
                _stars[i].anchoredPosition = new Vector2(960 - (i * 193 + scroll * (18 + i % 4 * 20)) % 980, 68 + i * 67 % 397);
            for (int i = 0; i < _skyline.Length; i++)
                _skyline[i].anchoredPosition = new Vector2((i * 82 - scroll * 14) % 1148 < -82 ? (i * 82 - scroll * 14) % 1148 + 1148 : (i * 82 - scroll * 14) % 1148, 54);
            _bill.anchoredPosition = new Vector2(Simulation.X, Simulation.Y);
            _bill.gameObject.SetActive(Simulation.Shields > 0 && (Simulation.InvulnerableFor <= 0 || (int)(_visualTime * 16) % 2 == 0));
            Position(_flame, -12 - (int)(_visualTime * 18) % 3 * 4, 9, 12 + (int)(_visualTime * 18) % 3 * 4, 10);
            for (int i = 0; i < _shots.Length; i++) MoveEntity(_shots[i], Simulation.Shots[i].Active, Simulation.Shots[i].X - 11, Simulation.Shots[i].Y - 2);
            for (int i = 0; i < _hostile.Length; i++) MoveEntity(_hostile[i], Simulation.HostileShots[i].Active, Simulation.HostileShots[i].X - 5, Simulation.HostileShots[i].Y - 3);
            InvadersSimulation.Enemy boss = null;
            for (int i = 0; i < _enemies.Length; i++)
            {
                var model = Simulation.Enemies[i]; var view = _enemies[i];
                float scale = model.Boss ? 1.9f : 1;
                MoveEntity(view.Root, model.Active, model.X - 36 * scale, model.Y - 44 * scale);
                if (!model.Active) continue;
                view.Root.localScale = Vector3.one * scale;
                if (view.Kind != model.Kind) { view.Portrait.sprite = _assets[model.Kind]; view.Kind = model.Kind; }
                view.Portrait.color = model.Flash > 0 ? new Color(1, .55f, .55f, 1) : Color.white;
                view.Health.sizeDelta = new Vector2(56f * model.Health / model.MaxHealth, 3);
                view.Health.GetComponent<Image>().color = _rivalColors[model.Kind];
                if (model.Boss) boss = model;
            }
            for (int i = 0; i < _sparks.Length; i++)
            {
                var spark = Simulation.Sparks[i]; MoveEntity(_sparks[i], spark.Active, spark.X, spark.Y);
                if (spark.Active) { Color color = _rivalColors[spark.Kind]; color.a = Mathf.Clamp01(spark.Life * 3); _sparks[i].GetComponent<Image>().color = color; }
            }
            _damage.color = new Color(1, .2f, .25f, Mathf.Clamp01((Simulation.InvulnerableFor - 1.4f) * 2) * .25f);
            _bossPanel.gameObject.SetActive(playing && boss != null);
            if (boss != null) { _bossName.text = InvadersAssets.RivalNames[boss.Kind].ToUpperInvariant(); _bossBar.sizeDelta = new Vector2(336f * boss.Health / boss.MaxHealth, 4); }
            _waveBanner.gameObject.SetActive(playing && boss == null && Simulation.WaveBannerFor > 0);
            if (_lastScore != Simulation.Score) { _score.text = T("score", "SCORE") + "  " + Simulation.Score.ToString("D6"); _lastScore = Simulation.Score; }
            if (_lastRecord != Context.HighScore) { _record.text = T("record", "RECORD") + "  " + Context.HighScore.ToString("D6"); _lastRecord = Context.HighScore; }
            if (_lastWave != Simulation.Wave) { _wave.text = T("wave", "WAVE") + "  " + Simulation.Wave.ToString("D2"); _waveBanner.text = _wave.text; _lastWave = Simulation.Wave; }
            if (_lastShields != Simulation.Shields)
            { for (int i = 0; i < 3; i++) _shieldBars[i].GetComponent<Image>().color = i < Simulation.Shields ? _mint : new Color32(48, 50, 66, 255); _lastShields = Simulation.Shields; }
            if (_lastState == Simulation.State) return;
            _overlay.gameObject.SetActive(!playing);
            bool over = Simulation.State == InvadersState.GameOver;
            _title.text = over ? T("game_over", "CAPITAL EXHAUSTED") : "AMBITIONS INVADERS";
            _subtitle.text = over ? T("score", "SCORE") + " " + Simulation.Score + "     /     " + T("wave", "WAVE") + " " + Simulation.Wave : T("tagline", "Your cash. Their faces. No negotiations.");
            _instructions.text = over ? T("retry", "SPACE / CLICK / R : RETRY") : T("start", "ARROWS / WASD : MOVE    HOLD SPACE / CLICK : FIRE\nSPACE : START    |    Don't let your rivals get past!");
            _lastState = Simulation.State;
        }

        private void SetBorder(float t)
        { Position(_borders[0], 0, 0, 960, t); Position(_borders[1], 0, 540 - t, 960, t); Position(_borders[2], 0, t, t, 540 - 2 * t); Position(_borders[3], 960 - t, t, t, 540 - 2 * t); }
        private static void MoveEntity(RectTransform rect, bool active, float x, float y)
        { if (rect.gameObject.activeSelf != active) rect.gameObject.SetActive(active); if (active) rect.anchoredPosition = new Vector2(x, y); }
        private static void Position(RectTransform rect, float x, float y, float w, float h)
        { rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h); }
        private RectTransform Paint(RectTransform parent, string name, float x, float y, float w, float h, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)obj.transform; rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero; Position(rect, x, y, w, h);
            var image = obj.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return rect;
        }
        private Text Label(RectTransform parent, string value, float x, float y, float w, float h, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var obj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var rect = (RectTransform)obj.transform; rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero; Position(rect, x, y, w, h);
            var text = obj.GetComponent<Text>(); text.font = _font; text.fontSize = size; text.fontStyle = FontStyle.Bold;
            text.color = color; text.alignment = align; text.text = value; text.raycastTarget = false; text.supportRichText = false; return text;
        }
        private static void SetLayers(Transform root) { root.gameObject.layer = 5; foreach (Transform child in root) SetLayers(child); }
        protected override void OnShutdown()
        {
            if (!_built) return;
            if (_camera != null) _camera.targetTexture = null;
            _assets = null; _built = false; LiveViews--;
            // MCG owns the hierarchy and disposes the loaded sprites after shutting the view down.
        }
    }
}
