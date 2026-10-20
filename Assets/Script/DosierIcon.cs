using UnityEngine;

/// <summary>Creates a small runtime briefcase glyph for the HUD when no sprite is assigned.</summary>
public static class DosierIcon
{
    private static Sprite cached;

    public static Sprite Create()
    {
        if (cached != null) return cached;
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color clear = new Color(0, 0, 0, 0);
        Color ink = new Color(0.96f, 0.78f, 0.28f, 1f);
        Color dark = new Color(0.12f, 0.14f, 0.17f, 1f);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
        RectFill(pixels, size, 11, 13, 53, 46, ink);
        RectFill(pixels, size, 23, 47, 41, 55, ink);
        RectFill(pixels, size, 25, 43, 39, 48, dark);
        RectFill(pixels, size, 29, 18, 35, 41, dark);
        RectFill(pixels, size, 15, 18, 49, 23, dark);
        texture.SetPixels(pixels);
        texture.Apply();
        cached = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return cached;
    }

    private static void RectFill(Color[] pixels, int size, int left, int bottom, int right, int top, Color color)
    {
        for (int y = bottom; y <= top; y++)
            for (int x = left; x <= right; x++) pixels[y * size + x] = color;
    }
}
