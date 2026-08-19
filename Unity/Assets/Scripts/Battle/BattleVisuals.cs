using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            // Bench units get a view too, parked inactive off-dock, so SubUnit only ever
            // has to toggle active state + reposition rather than instantiate mid-battle.
            foreach (var unit in world.Bench) BuildUnitView(unit, startActive: false);
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
                // Scale to fill the camera's view. Uniform "cover" scale (not independent
                // x/y stretch) -- source photos vary between landscape and portrait, and a
                // non-uniform stretch visibly squashed a portrait source when one was swapped
                // in for the second battle map.
                var cam = Camera.main;
                if (cam != null && cam.orthographic)
                {
                    float worldHeight = cam.orthographicSize * 2f;
                    float worldWidth = worldHeight * cam.aspect;
                    var bounds = sr.sprite.bounds.size;
                    float scale = Mathf.Max(worldWidth / bounds.x, worldHeight / bounds.y);
                    go.transform.localScale = new Vector3(scale, scale, 1f);
                }
            }
            else
            {
                sr.sprite = PlaceholderArt.FlatSprite(new Color(0.12f, 0.12f, 0.16f));
                go.transform.localScale = new Vector3(20f, 12f, 1f);
            }
            sr.sortingOrder = -10;
        }

        void BuildUnitView(BattleUnit unit, bool startActive = true)
        {
            var go = new GameObject($"Unit_{unit.Definition.characterId}");
            go.transform.SetParent(transform, false);
            go.transform.position = startActive ? BattleLayout.UnitPosition(unit.Column) : Vector3.zero;

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

            go.SetActive(startActive);
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

        /// <summary>Reverse of MoveToStage/MoveToMelee -- tweens both back to their dock
        /// positions. Safe to call even when the target never left its dock (the melee
        /// case): tweening a unit to the position it's already at is a harmless no-op.</summary>
        public IEnumerator ReturnToDock(BattleUnit actor, BattleUnit target)
        {
            yield return TweenPair(actor, DockPosition(actor), target, DockPosition(target));
        }

        const float MeleeApproachOffset = 1.8f;

        /// <summary>Melee-flavoured alternative to MoveToStage: the target stays put and
        /// the attacker closes the distance, stopping just short on their own side (left
        /// for a player attacker, right for an enemy attacker) rather than both units
        /// jumping to generic centre-stage marks. ReturnToDock (unchanged) handles the
        /// return trip afterward -- the target never moved, so its half of that tween is
        /// a no-op.</summary>
        public IEnumerator MoveToMelee(BattleUnit attacker, BattleUnit target)
        {
            if (!_unitViews.TryGetValue(attacker, out var attackerGo)) yield break;
            if (!_unitViews.TryGetValue(target, out var targetGo)) yield break;

            float side = attacker.Faction == Game.Data.Faction.Player ? -1f : 1f;
            var approachPos = targetGo.transform.position + new Vector3(side * MeleeApproachOffset, 0f, 0f);

            float t = 0f;
            var from = attackerGo.transform.position;
            while (t < StageTweenSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / StageTweenSeconds));
                attackerGo.transform.position = Vector3.Lerp(from, approachPos, k);
                yield return null;
            }
            attackerGo.transform.position = approachPos;
        }

        /// <summary>Reposition action: two same-faction units have already swapped Column
        /// values (BattleController.Reposition) -- tween both to their new dock positions.</summary>
        public IEnumerator SwapPositions(BattleUnit a, BattleUnit b)
        {
            yield return TweenPair(a, DockPosition(a), b, DockPosition(b));
        }

        /// <summary>After Formation.Compact reassigns columns on a death, tween every
        /// surviving unit of that faction to its new dock position so the frontline
        /// shift reads clearly instead of popping.</summary>
        public IEnumerator ReflowFormation(BattleWorld world, Game.Data.Faction faction)
        {
            var movers = world.AllUnits.Where(u => u.Faction == faction && u.IsAlive).ToList();
            if (movers.Count == 0) yield break;

            float t = 0f;
            var froms = movers.Select(u => _unitViews.TryGetValue(u, out var go) ? go.transform.position : DockPosition(u)).ToList();
            while (t < StageTweenSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / StageTweenSeconds));
                for (int i = 0; i < movers.Count; i++)
                    if (_unitViews.TryGetValue(movers[i], out var go))
                        go.transform.position = Vector3.Lerp(froms[i], DockPosition(movers[i]), k);
                yield return null;
            }
            foreach (var u in movers)
                if (_unitViews.TryGetValue(u, out var go)) go.transform.position = DockPosition(u);
        }

        /// <summary>Sub-in/sub-out: outgoing steps off-field, incoming appears in the
        /// exact column it vacated. BattleController has already swapped their Column
        /// values and the World.AllUnits/Bench lists before calling this.</summary>
        public IEnumerator SwapUnitView(BattleUnit outgoing, BattleUnit incoming)
        {
            if (_unitViews.TryGetValue(outgoing, out var outGo)) outGo.SetActive(false);
            if (_unitViews.TryGetValue(incoming, out var inGo))
            {
                inGo.transform.position = DockPosition(incoming);
                inGo.SetActive(true);
                if (_unitRenderers.TryGetValue(incoming, out var inSr)) inSr.color = Color.white;
            }
            yield break;
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

        /// <summary>Immediately (no tween) snaps every unit back to its dock position and
        /// re-applies active/bench visibility -- used after Undo/Redo, which can interrupt
        /// a MoveToStage/ReturnToDock tween mid-flight when it stops the turn coroutine,
        /// and can also move units between World.AllUnits and World.Bench (undoing past a
        /// sub-in/sub-out).</summary>
        public void SnapAllToDock(BattleWorld world)
        {
            foreach (var unit in world.AllUnits)
            {
                if (!_unitViews.TryGetValue(unit, out var go)) continue;
                go.SetActive(true);
                go.transform.position = DockPosition(unit);
            }
            foreach (var unit in world.Bench)
                if (_unitViews.TryGetValue(unit, out var go)) go.SetActive(false);
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
