#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Game.Battle;

namespace Game.EditorTools
{
    public static class BattleSceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Battle.unity";

        [MenuItem("AI.Game/Battle/Create Battle Scene")]
        public static void CreateBattleScene()
        {
            // Convenience: build/refresh the data assets first so a fresh clone can go
            // straight from "open project" to "press Play" in one menu click.
            BattleAssetBuilder.Build();

            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrap = new GameObject("BattleBootstrap");
            bootstrap.AddComponent<BattleBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AI.Game] Created {ScenePath}. Press Play to run the auto-battle.");
        }

        [MenuItem("AI.Game/Battle/Open Battle Scene")]
        public static void OpenBattleScene()
        {
            if (!File.Exists(ScenePath)) CreateBattleScene();
            EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
#endif
