using UnityEngine;

namespace Game.Battle
{
    /// <summary>Side-view (Darkest Dungeon / Slay the Spire style) layout constants.
    /// Column is the only spatial axis that matters -- lane is always 0.</summary>
    public static class BattleLayout
    {
        // ColumnSpacing must stay comfortably above the widest expected sprite's
        // world-space width or adjacent units overlap -- confirmed visually via a real
        // build, not assumed (a 3-unit-wide sprite at 2.4 spacing physically overlapped
        // its neighbour). Units now normalize to TargetUnitHeight regardless of source
        // resolution/aspect (see BattleVisuals) instead of a flat scale multiplier, so
        // a square-ish sprite at that height is ~TargetUnitHeight wide in the worst
        // case -- ColumnSpacing leaves comfortable margin above that.
        public const float TargetUnitHeight = 3.0f;
        public const float ColumnSpacing = 3.6f;
        public const float GroundY = -2.1f;

        // Centered between column 2 (player front) and column 3 (enemy front), so the
        // two front-liners face off just left/right of screen centre.
        public static float ColumnToWorldX(int column) => (column - 2.5f) * ColumnSpacing;

        public static Vector3 UnitPosition(int column) => new(ColumnToWorldX(column), GroundY, 0f);

        public static void ApplyBattleCamera(Camera cam)
        {
            cam.orthographic = true;
            cam.orthographicSize = 6.2f;
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
