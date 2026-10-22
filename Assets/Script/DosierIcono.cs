using UnityEngine;

/// <summary>Creates a small runtime briefcase glyph for the HUD when no sprite is assigned.</summary>
public static class DosierIcono
{
    private static Sprite almacenado;

    public static Sprite Crear()
    {
        if (almacenado != null) return almacenado;
        const int tamano = 64;
        Texture2D texture = new Texture2D(tamano, tamano, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color transparente = new Color(0, 0, 0, 0);
        Color tinta = new Color(0.96f, 0.78f, 0.28f, 1f);
        Color oscuro = new Color(0.12f, 0.14f, 0.17f, 1f);
        Color[] pixeles = new Color[tamano * tamano];
        for (int i = 0; i < pixeles.Length; i++) pixeles[i] = transparente;
        RellenarRectangulo(pixeles, tamano, 11, 13, 53, 46, tinta);
        RellenarRectangulo(pixeles, tamano, 23, 47, 41, 55, tinta);
        RellenarRectangulo(pixeles, tamano, 25, 43, 39, 48, oscuro);
        RellenarRectangulo(pixeles, tamano, 29, 18, 35, 41, oscuro);
        RellenarRectangulo(pixeles, tamano, 15, 18, 49, 23, oscuro);
        texture.SetPixels(pixeles);
        texture.Apply();
        almacenado = Sprite.Create(texture, new Rect(0, 0, tamano, tamano), new Vector2(0.5f, 0.5f), 100f);
        return almacenado;
    }

    private static void RellenarRectangulo(Color[] pixeles, int tamano, int izquierda, int abajo, int derecha, int arriba, Color color)
    {
        for (int y = abajo; y <= arriba; y++)
            for (int x = izquierda; x <= derecha; x++) pixeles[y * tamano + x] = color;
    }
}
