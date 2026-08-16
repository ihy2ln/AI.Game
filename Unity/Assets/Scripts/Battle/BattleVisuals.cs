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

        public void Build(BattleWorld world)
        {
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
            go.transform.localScale = Vector3.one * BattleLayout.UnitScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = unit.Definition.pixelSprite32 != null ? unit.Definition.pixelSprite32 : PlaceholderArt.UnitFallback();
            sr.flipX = !unit.FacingRight;
            sr.sortingOrder = 10;

            _unitViews[unit] = go;
            _unitRenderers[unit] = sr;
        }

        public Vector3 GetUnitWorldPosition(BattleUnit unit) =>
            _unitViews.TryGetValue(unit, out var go) ? go.transform.position + Vector3.up * 1.6f : Vector3.zero;

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
    }
}
