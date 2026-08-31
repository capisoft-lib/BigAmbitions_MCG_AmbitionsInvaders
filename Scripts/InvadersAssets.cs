using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Capisoft.Lib.BaComputerGames;
using UnityEngine;

namespace AmbitionsInvaders
{
    public sealed class InvadersAssets : ComputerGameAssets
    {
        public static readonly string[] RivalNames = { "Huang Guo", "Ingrid Schneider", "Jessica Johnson", "Thierry Laurent Moreau" };
        internal static readonly string[] Files = { "huang-guo.png", "ingrid-schneider.png", "jessica-johnson.png", "thierry-laurent-moreau.png" };
        public static int LiveSets { get; private set; }
        private readonly List<Texture2D> _textures = new List<Texture2D>();
        private readonly List<Sprite> _sprites = new List<Sprite>();
        private bool _disposed;
        public Sprite this[int index] => _sprites[index];
        public int Count => _sprites.Count;
        internal InvadersAssets() { LiveSets++; }

        internal void Add(byte[] bytes, string name)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            _textures.Add(texture); texture.name = name;
            if (!ImageConversion.LoadImage(texture, bytes, false) || texture.width > 4096 || texture.height > 4096)
                throw new InvalidDataException("Invalid rival image: " + name);
            texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
            var pixels = texture.GetPixels32();
            int left = texture.width, right = -1, bottom = texture.height, top = -1;
            for (int y = 0; y < texture.height; y++)
                for (int x = 0; x < texture.width; x++)
                    if (pixels[y * texture.width + x].a > 32)
                    { left = Math.Min(left, x); right = Math.Max(right, x); bottom = Math.Min(bottom, y); top = Math.Max(top, y); }
            if (right < left) throw new InvalidDataException("Empty rival image: " + name);
            // Trim transparent margins by Sprite UVs only; keep the generated PNG untouched.
            var sprite = Sprite.Create(texture, new Rect(left, bottom, right - left + 1, top - bottom + 1), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
            sprite.name = name; _sprites.Add(sprite);
        }

        public override void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var sprite in _sprites) if (sprite != null) UnityEngine.Object.Destroy(sprite);
            foreach (var texture in _textures) if (texture != null) UnityEngine.Object.Destroy(texture);
            _sprites.Clear(); _textures.Clear(); LiveSets--;
        }
    }

    public sealed class InvadersLoader : IComputerGameLoader
    {
        public async Task<ComputerGameAssets> LoadAsync(ComputerGameLoadContext context, CancellationToken cancellationToken)
        {
            var assets = new InvadersAssets();
            try
            {
                foreach (string name in InvadersAssets.Files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string path = Path.Combine(context.ModRootPath, "Art", name);
                    var info = new FileInfo(path);
                    if (!info.Exists || info.Length > 12 * 1024 * 1024) throw new InvalidDataException("Missing or oversized rival image: " + name);
                    assets.Add(File.ReadAllBytes(path), name);
                    await Task.Yield(); // Unity continuations stay on the main thread; spread uploads over frames.
                }
                cancellationToken.ThrowIfCancellationRequested();
                return assets;
            }
            catch { assets.Dispose(); throw; }
        }
    }
}
