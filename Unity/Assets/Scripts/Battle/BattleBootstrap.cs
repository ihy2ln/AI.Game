using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// Boots the battle scene at runtime -- no Inspector wiring required. Mirrors
    /// FarmBootstrap's pattern exactly. Attach to an empty GameObject in Battle.unity
    /// (or let AI.Game > Battle > Create Battle Scene create it).
    /// </summary>
    public class BattleBootstrap : MonoBehaviour
    {
        void Start() => Boot();

        [ContextMenu("Boot Battle")]
        public void Boot()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            var camGo = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
            var cam = camGo.GetComponent<Camera>() ?? camGo.AddComponent<Camera>();
            if (camGo.GetComponent<AudioListener>() == null) camGo.AddComponent<AudioListener>();
            camGo.tag = "MainCamera";
            BattleLayout.ApplyBattleCamera(cam);

            var world = new BattleWorld();

            var visualsGo = new GameObject("BattleVisuals");
            visualsGo.transform.SetParent(transform, false);
            var visuals = visualsGo.AddComponent<BattleVisuals>();
            visuals.Build(world);

            var ctrlGo = new GameObject("BattleController");
            ctrlGo.transform.SetParent(transform, false);
            var ctrl = ctrlGo.AddComponent<BattleController>();
            ctrl.OnRestartRequested += Boot;
            ctrl.Init(world, visuals);

            var hudGo = new GameObject("BattleHud");
            hudGo.transform.SetParent(transform, false);
            hudGo.AddComponent<BattleHud>().Init(ctrl, cam);

            Debug.Log(world.LoadedOk
                ? "[AI.Game] Battle booted (side-view auto-battle vertical slice)."
                : "[AI.Game] Battle boot failed to load data -- run AI.Game > Battle > Build Assets From Manifest.");
        }
    }
}
