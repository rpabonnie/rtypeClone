using System.Text.Json;
using Raylib_cs;

namespace rtypeClone.Core;

public static class AssetManager
{
    public static Texture2D BulletSheet { get; private set; }

    // Source rectangles for bullet sprites on M484BulletCollection1.png (520x361)
    public static readonly Rectangle NormalBulletSrc = new(160f, 56f, 16f, 16f);
    public static readonly Rectangle ChargedBulletSrc = new(320f, 332f, 24f, 24f);

    // Atlas system: texture + frame lookup
    private static readonly Dictionary<string, Texture2D> _atlasTextures = new();
    private static readonly Dictionary<string, Rectangle> _frameRects = new();

    public static void Load()
    {
        BulletSheet = Raylib.LoadTexture("Assets/M484BulletCollection1.png");
    }

    public static void LoadAtlas(string jsonPath)
    {
        if (!File.Exists(jsonPath)) return;

        var json = File.ReadAllText(jsonPath);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Load the atlas texture
        var imageName = root.GetProperty("meta").GetProperty("image").GetString()!;
        var dir = Path.GetDirectoryName(jsonPath) ?? "";
        var texturePath = Path.Combine(dir, imageName);
        var texture = Raylib.LoadTexture(texturePath);
        _atlasTextures[imageName] = texture;

        // Parse frame rectangles
        var frames = root.GetProperty("frames");
        foreach (var frame in frames.EnumerateObject())
        {
            float x = frame.Value.GetProperty("x").GetSingle();
            float y = frame.Value.GetProperty("y").GetSingle();
            float w = frame.Value.GetProperty("w").GetSingle();
            float h = frame.Value.GetProperty("h").GetSingle();
            _frameRects[frame.Name] = new Rectangle(x, y, w, h);
        }
    }

    public static Rectangle GetSourceRect(string frameName) =>
        _frameRects.TryGetValue(frameName, out var rect) ? rect : default;

    public static Texture2D GetAtlasTexture(string imageName) =>
        _atlasTextures.TryGetValue(imageName, out var tex) ? tex : default;

    public static bool HasFrame(string frameName) => _frameRects.ContainsKey(frameName);

    public static void Unload()
    {
        Raylib.UnloadTexture(BulletSheet);
        foreach (var tex in _atlasTextures.Values)
            Raylib.UnloadTexture(tex);
        _atlasTextures.Clear();
        _frameRects.Clear();
    }
}
