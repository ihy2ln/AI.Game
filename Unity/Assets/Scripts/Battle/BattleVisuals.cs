using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>Sprites for the side-view battle: background + one SpriteRenderer per
    /// unit. Mirrors FarmVisuals' role (build once from a world, then sync).</summary>
    public class BattleVisuals : MonoBehaviour
    {
        readonly Dictionary<BattleUnit, GameObject> _unitViews = new();
        readonly Dictionary<BattleUnit, SpriteRenderer> _unitRenderers = new();
        Game.Data.MapDefinition _map;

        public void Build(BattleWorld world)
        {
            _map = world.Map;
            BuildBackground(world);
            foreach (var unit in world.AllUnits) BuildUnitView(unit);
        }

        void BuildBackground(BattleWorld world)
        {
            var bgSprite = world.Map != null ? world.Map.backgroundSprite : null;
            var go = new GameObject("Background");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(0f, 0f, 5f); // behind units
            var sr = go.AddComponent<SpriteRenderer>();
            if (bgSprite != null)
            {
                sr.sprite = bgSprite;
                // Scale the 1920x1080 background sprite to fill the camera's view.
                var cam = Camera.main;
                if (cam != null && cam.orthographic)
                {
                    float worldHeight = cam.orthographicSize * 2f;
                    float worldWidth = worldHeight * cam.aspect;
                    var bounds = sr.sprite.bounds.size;
                    go.transform.localScale = new Vector3(worldWidth / bounds.x, worldHeight / bounds.y, 1f);
                }
            }
            else
            {
                sr.sprite = PlaceholderArt.FlatSprite(new Color(0.12f, 0.12f, 0.16f));
                go.transform.localScale = new Vector3(20f, 12f, 1f);
            }
            sr.sortingOrder = -10;
        }

        void BuildUnitView(BattleUnit unit)
        {
            var go = new GameObject($"Unit_{unit.Definition.characterId}");
            go.transform.SetParent(transform, false);
            go.transform.position = BattleLayout.UnitPosition(unit.Column);

            var sr = go.AddComponent<SpriteRenderer>();
            var def = unit.Definition;
            sr.sprite = def.battleSprite != null ? def.battleSprite
                : def.pixelSprite32 != null ? def.pixelSprite32
                : PlaceholderArt.UnitFallback();
            sr.flipX = !unit.FacingRight;
            sr.sortingOrder = 10;

            // Normalize by world-space height regardless of source resolution/aspect --
            // a fixed scale multiplier overlapped neighbouring columns the moment art
            // with a different native size was swapped in (confirmed via a real build).
            float height = Mathf.Max(sr.sprite.bounds.size.y, 0.01f);
            float scale = BattleLayout.TargetUnitHeight / height;
            go.transform.localScale = Vector3.one * scale;

            _unitViews[unit] = go;
            _unitRenderers[unit] = sr;
        }

        /// <summary>Screen-space hit test for manual targeting -- no colliders needed,
        /// just checks each unit's SpriteRenderer world bounds against the click point
        /// projected onto the units' z-plane.</summary>
        public bool TryGetUnitAtScreenPoint(Vector3 screenPos, Camera cam, out BattleUnit unit)
        {
            float distanceToUnitPlane = -cam.transform.position.z; // units sit at z=0
            var world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distanceToUnitPlane));
            foreach (var kv in _unitRenderers)
            {
                if (kv.Value.bounds.Contains(new Vector3(world.x, world.y, 0f)))
                {
                    unit = kv.Key;
                    return true;
                }
            }
            unit = null;
            return false;
        }

        public Vector3 GetUnitWorldPosition(BattleUnit unit) =>
            _unitViews.TryGetValue(unit, out var go) ? go.transform.position + Vector3.up * 1.6f : Vector3.zero;

        /// <summary>A unit's resting position in its side dock -- the "return to" point
        /// after a centre-stage cinematic beat, and the source of truth BattleLayout
        /// itself uses when a unit's view is first built.</summary>
        public Vector3 DockPosition(BattleUnit unit) => BattleLayout.UnitPosition(unit.Column);

        const float StageTweenSeconds = 0.25f;

        /// <summary>Tweens the acting unit and its target from their docks onto the
        /// centre stage (each on its own faction's side) for a turn's cinematic beat.
        /// Both may already be off-dock (harmless no-op tween if so). No-op for a unit
        /// with no view (e.g. missing/never-built).</summary>
        public IEnumerator MoveToStage(BattleUnit actor, BattleUnit target)
        {
            yield return TweenPair(actor, BattleLayout.StagePosition(actor.Faction),
                target, BattleLayout.StagePosition(target.Faction));
        }

        /// <summary>Reverse of MoveToStage -- tweens both back to their dock positions.</summary>
        public IEnumerator ReturnToDock(BattleUnit actor, BattleUnit target)
        {
            yield return TweenPair(actor, DockPosition(actor), target, DockPosition(target));
        }

        IEnumerator TweenPair(BattleUnit unitA, Vector3 toA, BattleUnit unitB, Vector3 toB)
        {
            _unitViews.TryGetValue(unitA, out var goA);
            _unitViews.TryGetValue(unitB, out var goB);
            if (goA == null && goB == null) yield break;

            var fromA = goA != null ? goA.transform.position : toA;
            var fromB = goB != null ? goB.transform.position : toB;

            float t = 0f;
            while (t < StageTweenSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / StageTweenSeconds));
                if (goA != null) goA.transform.position = Vector3.Lerp(fromA, toA, k);
                if (goB != null) goB.transform.position = Vector3.Lerp(fromB, toB, k);
                yield return null;
            }
            if (goA != null) goA.transform.position = toA;
            if (goB != null) goB.transform.position = toB;
        }

        /// <summary>Re-applies every unit's dead/alive tint from current HP -- needed
        /// after BattleHistory.Restore snapshots HP back onto units outside the normal
        /// ApplyDamage path (undo can revive a unit SyncDefeated already greyed out).</summary>
        public void SyncAll(BattleWorld world)
        {
            foreach (var unit in world.AllUnits)
            {
                if (!_unitRenderers.TryGetValue(unit, out var sr)) continue;
                sr.color = unit.IsAlive ? Color.white : new Color(0.35f, 0.35f, 0.35f, 0.6f);
            }
        }

        public void FlashHit(BattleUnit unit)
        {
            if (_unitRenderers.TryGetValue(unit, out var sr)) StartCoroutine(FlashRoutine(sr));
        }

        System.Collections.IEnumerator FlashRoutine(SpriteRenderer sr)
        {
            var original = sr.color;
            sr.color = Color.red;
            yield return new WaitForSeconds(0.12f);
            if (sr != null) sr.color = original;
        }

        public void SyncDefeated(BattleUnit unit)
        {
            if (!unit.IsAlive && _unitRenderers.TryGetValue(unit, out var sr))
                sr.color = new Color(0.35f, 0.35f, 0.35f, 0.6f);
        }

        const float FxWorldHeight = 1.8f;
        const float FxFrameSeconds = 0.045f;

        public void PlayImpactFx(BattleUnit target)
        {
            if (_map == null || _map.fxImpactSheet == null || _map.fxImpactFrameRects.Count == 0) return;
            if (!_unitViews.TryGetValue(target, out var targetGo)) return;
            StartCoroutine(ImpactFxRoutine(targetGo.transform.position + Vector3.up * 0.6f));
        }

        System.Collections.IEnumerator ImpactFxRoutine(Vector3 worldPos)
        {
            var tex = _map.fxImpactSheet.texture;
            var go = new GameObject("ImpactFx");
            go.transform.SetParent(transform, false);
            go.transform.position = worldPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 20;

            foreach (var rect in _map.fxImpactFrameRects)
            {
                var pixelRect = new Rect(rect.x, tex.height - rect.y - rect.w, rect.z, rect.w);
                var frameSprite = Sprite.Create(tex, pixelRect, new Vector2(0.5f, 0.5f), 100f);
                sr.sprite = frameSprite;
                float s = FxWorldHeight / Mathf.Max(frameSprite.bounds.size.y, 0.01f);
                go.transform.localScale = Vector3.one * s;
                yield return new WaitForSeconds(FxFrameSeconds);
            }
            Destroy(go);
        }
    }
}
