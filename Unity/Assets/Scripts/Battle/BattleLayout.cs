using UnityEngine;

namespace Game.Battle
{
    /// <summary>Side-view (Darkest Dungeon / Slay the Spire style) layout constants.
    /// Column is the only spatial axis that matters -- lane is always 0.</summary>
    public static class BattleLayout
    {
        // UnitScale must stay comfortably below ColumnSpacing or adjacent units'
        // sprite quads overlap -- confirmed visually via a real build, not assumed
        // (a 3-unit-wide sprite at 2.4 spacing physically overlapped its neighbour).
        public const float ColumnSpacing = 2.8f;
        public const float UnitScale = 2f;
        public const float GroundY = -1.6f;

        // Centered between column 2 (player front) and column 3 (enemy front), so the
        // two front-liners face off just left/right of screen centre.
        public static float ColumnToWorldX(int column) => (column - 2.5f) * ColumnSpacing;

        public static Vector3 UnitPosition(int column) => new(ColumnToWorldX(column), GroundY, 0f);

        public static void ApplyBattleCamera(Camera cam)
        {
            cam.orthographic = true;
            cam.orthographicSize = 5.2f;
            cam.transform.position = new Vector3(0f, -0.2f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 40f;
            cam.allowMSAA = false;
        }
    }
}
