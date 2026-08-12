using UnityEngine;

namespace Game.Farm
{
    /// <summary>2.5D isometric helpers tuned for a tiny HD-pixel aesthetic pass.</summary>
    public static class FarmIso
    {
        public const float TileSize = 1.4f;
        public const float TileHeight = 0.22f;

        public static Vector3 GridToWorld(int x, int y, float yOffset = 0f)
        {
            var wx = (x - y) * (TileSize * 0.5f);
            var wz = (x + y) * (TileSize * 0.5f);
            return new Vector3(wx, yOffset, wz);
        }

        public static Vector2Int WorldToGrid(Vector3 world)
        {
            var a = world.x / (TileSize * 0.5f);
            var b = world.z / (TileSize * 0.5f);
            var x = Mathf.RoundToInt((a + b) * 0.5f);
            var y = Mathf.RoundToInt((b - a) * 0.5f);
            return new Vector2Int(x, y);
        }

        public static void ApplyAestheticCamera(Camera cam, Vector3 lookAt)
        {
            cam.orthographic = true;
            // Tight frame for 2×2 so pixels / silhouettes read clearly
            cam.orthographicSize = 2.55f;
            cam.transform.rotation = Quaternion.Euler(33f, 45f, 0f);
            cam.transform.position = lookAt + cam.transform.rotation * new Vector3(0f, 0.15f, -18f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            // Soft dusk sky — anime/JRPG field vibe
            cam.backgroundColor = new Color(0.16f, 0.14f, 0.28f);
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 60f;
            cam.allowMSAA = false; // keep edges chunky like HD pixel
        }

        /// <summary>
        /// Wide exploration framing for the larger forest map (roadmap item 3).
        /// Same fixed yaw as the Home/diorama camera above — only pitch and
        /// zoom change, so switching modes never reorients the player's
        /// mental map. Not wired up yet; nothing calls this until the larger
        /// map exists. See Assets/Art/README.md for the Home vs Overworld spec.
        /// </summary>
        public static void ApplyOverworldCamera(Camera cam, Vector3 lookAt)
        {
            cam.orthographic = true;
            cam.orthographicSize = 10f;
            cam.transform.rotation = Quaternion.Euler(60f, 45f, 0f);
            cam.transform.position = lookAt + cam.transform.rotation * new Vector3(0f, 0.15f, -18f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            // Cool forest-day sky, distinct from Home's dusk purple
            cam.backgroundColor = new Color(0.20f, 0.30f, 0.26f);
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 80f;
            cam.allowMSAA = false;
        }
    }

    /// <summary>Point-filtered pixel textures for HD-pixel material look.</summary>
    public static class FarmPixelArt
    {
        public static Material MakePixelMat(Color a, Color b, int size = 16, float checker = 0.35f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                name = "PixTex"
            };

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var band = (y / 2) % 2 == 0;
                var check = ((x / 2) + (y / 2)) % 2 == 0;
                var c = Color.Lerp(a, b, check ? checker : 0f);
                if (band) c = Color.Lerp(c, a, 0.15f);
                // 1px outline dither on edges of texel clusters
                if (x == 0 || y == 0) c *= 0.82f;
                tex.SetPixel(x, y, c);
            }
            tex.Apply(false, true);

            // Unlit reads closer to painted pixel sprites under ortho light
            var shader = Shader.Find("Unlit/Texture") ?? Shader.Find("Standard");
            var m = new Material(shader);
            m.mainTexture = tex;
            if (m.HasProperty("_Color")) m.color = Color.white;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0f);
            return m;
        }

        public static Material MakeFlatPixel(Color c)
        {
            return MakePixelMat(c, c * 0.78f, 8, 0.25f);
        }

        /// <summary>Chibi anime face sprite (billboard) — tiny procedural HD pixel.</summary>
        public static Texture2D MakeChibiFace(int size = 32)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var clear = new Color(0, 0, 0, 0);
            var skin = new Color(0.96f, 0.82f, 0.72f);
            var skinShade = new Color(0.86f, 0.68f, 0.58f);
            var hair = new Color(0.22f, 0.14f, 0.12f);
            var hairHi = new Color(0.38f, 0.24f, 0.18f);
            var eye = new Color(0.12f, 0.1f, 0.18f);
            var blush = new Color(1f, 0.55f, 0.55f, 0.55f);
            var outline = new Color(0.08f, 0.06f, 0.1f);

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
                tex.SetPixel(x, y, clear);

            void Disc(int cx, int cy, int r, Color col)
            {
                for (var y = -r; y <= r; y++)
                for (var x = -r; x <= r; x++)
                    if (x * x + y * y <= r * r)
                    {
                        var px = cx + x;
                        var py = cy + y;
                        if (px >= 0 && py >= 0 && px < size && py < size)
                            tex.SetPixel(px, py, col);
                    }
            }

            // outline then fill
            Disc(size / 2, size / 2 - 1, 11, outline);
            Disc(size / 2, size / 2 - 1, 10, skin);
            Disc(size / 2 - 3, size / 2 - 3, 3, skinShade);

            // hair bangs
            for (var x = 6; x <= 25; x++)
            for (var y = 18; y <= 28; y++)
            {
                if ((x - 16) * (x - 16) + (y - 22) * (y - 22) < 90)
                    tex.SetPixel(x, y, (x + y) % 3 == 0 ? hairHi : hair);
            }

            // eyes
            Disc(11, 13, 2, eye);
            Disc(21, 13, 2, eye);
            tex.SetPixel(12, 14, Color.white);
            tex.SetPixel(22, 14, Color.white);

            // blush
            Disc(9, 10, 1, blush);
            Disc(23, 10, 1, blush);

            tex.Apply(false, true);
            return tex;
        }
    }
}
