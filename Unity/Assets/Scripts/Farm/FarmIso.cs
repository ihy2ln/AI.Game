using UnityEngine;

namespace Game.Farm
{
    /// <summary>Isometric helpers. Grid (x,y) → world; camera uses classic BD2/FFT angle.</summary>
    public static class FarmIso
    {
        public const float TileSize = 1.2f;
        public const float TileHeight = 0.18f;

        public static Vector3 GridToWorld(int x, int y, float yOffset = 0f)
        {
            // Flat-top isometric diamond layout in XZ
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

        public static void ApplyIsometricCamera(Camera cam, Vector3 lookAt, float size = 5.2f)
        {
            cam.orthographic = true;
            cam.orthographicSize = size;
            cam.transform.rotation = Quaternion.Euler(30f, 45f, 0f);
            cam.transform.position = lookAt + cam.transform.rotation * new Vector3(0f, 0f, -20f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.1f, 0.16f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 80f;
        }
    }
}
