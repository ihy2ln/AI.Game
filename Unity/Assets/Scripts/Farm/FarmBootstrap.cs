using UnityEngine;

namespace Game.Farm
{
    /// <summary>
    /// Boots the starter farm at runtime — no Inspector wiring required.
    /// Attach to an empty GameObject in Farm.unity (or let the editor builder create it).
    /// </summary>
    public class FarmBootstrap : MonoBehaviour
    {
        void Start() => Boot();

        [ContextMenu("Boot Farm")]
        public void Boot()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }

            var world = new FarmWorld();

            var camGo = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
            var cam = camGo.GetComponent<Camera>() ?? camGo.AddComponent<Camera>();
            if (camGo.GetComponent<AudioListener>() == null) camGo.AddComponent<AudioListener>();
            cam.tag = "MainCamera";

            var visualsGo = new GameObject("FarmVisuals");
            visualsGo.transform.SetParent(transform, false);
            var visuals = visualsGo.AddComponent<FarmVisuals>();
            visuals.Build(world);
            FarmIso.ApplyIsometricCamera(cam, visuals.MapCenter, 5.0f);

            var ctrlGo = new GameObject("FarmController");
            ctrlGo.transform.SetParent(transform, false);
            var ctrl = ctrlGo.AddComponent<FarmController>();
            ctrl.Init(world, visuals, cam);

            var hudGo = new GameObject("FarmHud");
            hudGo.transform.SetParent(transform, false);
            hudGo.AddComponent<FarmHud>().Init(ctrl);

            Debug.Log("[AI.Game] Farm starter 4×4 booted (Brown Dust 2 / Stardew-Rune Factory clearing).");
        }
    }
}
