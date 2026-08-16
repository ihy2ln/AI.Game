using UnityEngine;
using Game.Data;

namespace Game.Battle
{
    /// <summary>Three-panel side-view layout: allies dock left, enemies dock right, and
    /// the gap between the two front ranks is the empty centre "stage" where
    /// BattleVisuals.MoveToStage/ReturnToDock actually stage a turn's action -- see
    /// BattleController.RunBattle. Column is still the only spatial axis that matters
    /// for targeting/range (TargetResolver) -- only the *presentation* X position
    /// changed from a single continuous line to two clustered docks.</summary>
    public static class BattleLayout
    {
        // DockColumnSpacing must stay comfortably above the widest expected sprite's
        // world-space width or adjacent units overlap -- confirmed visually via a real
        // build, not assumed (a 3-unit-wide sprite at 2.4 spacing physically overlapped
        // its neighbour). Units normalize to TargetUnitHeight regardless of source
        // resolution/aspect (see BattleVisuals) instead of a flat scale multiplier, so
        // a square-ish sprite at that height is ~TargetUnitHeight wide in the worst
        // case -- DockColumnSpacing leaves comfortable margin above that.
        public const float TargetUnitHeight = 3.0f;
        public const float DockColumnSpacing = 1.9f;

        // Distance of each side's front rank from screen centre -- deliberately large
        // relative to DockColumnSpacing so the reserved middle stage reads as the
        // visual focal point of the screen, not a sliver between two lineups.
        public const float DockFrontOffset = 6.2f;

        // Where an acting/targeted unit stands during its centre-stage cinematic beat
        // (BattleVisuals.MoveToStage), on its own faction's side of centre.
        public const float StageOffset = 2.2f;

        public const float GroundY = -2.1f;

        // Player columns 0(back)..2(front) cluster left of centre, front nearest centre.
        // Enemy columns 3(front)..5(back) mirror on the right, so the two front-liners
        // (2 vs 3) are still the pair closest to screen centre.
        public static float ColumnToWorldX(int column) => column <= 2
            ? -DockFrontOffset - (2 - column) * DockColumnSpacing
            : DockFrontOffset + (column - 3) * DockColumnSpacing;

        public static Vector3 UnitPosition(int column) => new(ColumnToWorldX(column), GroundY, 0f);

        public static Vector3 StagePosition(Faction faction) =>
            new(faction == Faction.Player ? -StageOffset : StageOffset, GroundY, 0f);

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
