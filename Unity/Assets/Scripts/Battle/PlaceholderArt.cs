using UnityEngine;

namespace Game.Battle
{
    /// <summary>Runtime flat-colour fallback for any missing generated asset, so a
    /// battle never fails to render just because art hasn't been generated/imported
    /// yet ("never let missing art block code" -- see the vertical-slice brief).</summary>
    public static class PlaceholderArt
    {
        static Sprite _cached;

        public static Sprite FlatSprite(Color color)
        {
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        }

        public static Sprite UnitFallback() => _cached ??= FlatSprite(new Color(0.6f, 0.6f, 0.65f));
    }
}
