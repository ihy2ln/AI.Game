using System.Collections.Generic;
using UnityEngine;

namespace Game.Farm
{
    /// <summary>
    /// Procedural BD2-inspired farm visuals: sunset light, grid tiles, dense obstacles.
    /// Uses Built-in RP primitives so the project stays code-first.
    /// </summary>
    public class FarmVisuals : MonoBehaviour
    {
        FarmWorld _world;
        readonly Dictionary<Vector2Int, GameObject> _obstacleViews = new();
        readonly Dictionary<Vector2Int, GameObject> _tileViews = new();
        GameObject _playerView;
        GameObject _hover;
        Material _matGrassA, _matGrassB, _matTilled, _matWood, _matLeaf, _matAutumn, _matRock, _matPlayer, _matBadge;

        public void Build(FarmWorld world)
        {
            _world = world;
            BuildMaterials();
            BuildAtmosphere();
            BuildFringe();
            BuildTiles();
            BuildObstacles();
            BuildPlayer();
            BuildHover();
        }

        void BuildMaterials()
        {
            _matGrassA = Mat(new Color(0.18f, 0.38f, 0.24f));
            _matGrassB = Mat(new Color(0.14f, 0.32f, 0.20f));
            _matTilled = Mat(new Color(0.42f, 0.28f, 0.18f));
            _matWood = Mat(new Color(0.32f, 0.20f, 0.12f));
            _matLeaf = Mat(new Color(0.16f, 0.38f, 0.20f));
            _matAutumn = Mat(new Color(0.78f, 0.48f, 0.16f));
            _matRock = Mat(new Color(0.42f, 0.34f, 0.30f));
            _matPlayer = Mat(new Color(0.26f, 0.40f, 0.62f));
            _matBadge = Mat(new Color(0.08f, 0.09f, 0.11f));
        }

        static Material Mat(Color c)
        {
            var m = new Material(Shader.Find("Standard"));
            m.color = c;
            m.SetFloat("_Glossiness", 0.15f);
            return m;
        }

        void BuildAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.55f, 0.32f, 0.22f);
            RenderSettings.fogDensity = 0.035f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.35f, 0.45f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.32f, 0.22f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.1f, 0.08f);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.72f, 0.42f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(28f, -35f, 0f);
            sun.transform.SetParent(transform, false);

            var fill = new GameObject("Fill").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.35f, 0.45f, 0.7f);
            fill.intensity = 0.35f;
            fill.transform.rotation = Quaternion.Euler(50f, 140f, 0f);
            fill.transform.SetParent(transform, false);
        }

        void BuildFringe()
        {
            var root = new GameObject("ForestFringe").transform;
            root.SetParent(transform, false);
            var center = FarmIso.GridToWorld((_world.Width - 1) / 2, (_world.Height - 1) / 2);
            for (var i = 0; i < 18; i++)
            {
                var ang = (i / 18f) * Mathf.PI * 2f;
                var dist = 4.2f + (i % 3) * 0.35f;
                var p = center + new Vector3(Mathf.Cos(ang) * dist * 1.15f, 0f, Mathf.Sin(ang) * dist * 0.75f);
                var bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bush.name = "FringeBush";
                bush.transform.SetParent(root, false);
                bush.transform.position = p + Vector3.up * 0.35f;
                bush.transform.localScale = new Vector3(1.2f, 0.7f, 1.0f) * (0.8f + (i % 4) * 0.12f);
                bush.GetComponent<Renderer>().sharedMaterial = _matLeaf;
                Object.Destroy(bush.GetComponent<Collider>());
            }
        }

        void BuildTiles()
        {
            var root = new GameObject("Tiles").transform;
            root.SetParent(transform, false);
            for (var y = 0; y < _world.Height; y++)
            for (var x = 0; x < _world.Width; x++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Tile_{x}_{y}";
                go.transform.SetParent(root, false);
                go.transform.position = FarmIso.GridToWorld(x, y, FarmIso.TileHeight * 0.5f);
                go.transform.localScale = new Vector3(FarmIso.TileSize * 0.92f, FarmIso.TileHeight, FarmIso.TileSize * 0.92f);
                var soil = _world.GetSoil(x, y);
                var mat = soil == FarmSoilKind.Tilled
                    ? _matTilled
                    : ((x + y) % 2 == 0 ? _matGrassA : _matGrassB);
                go.GetComponent<Renderer>().sharedMaterial = mat;
                // Keep collider for click picking
                _tileViews[new Vector2Int(x, y)] = go;

                // Grid outline (BD2 tactical feel)
                var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Grid";
                line.transform.SetParent(go.transform, false);
                line.transform.localPosition = new Vector3(0f, 0.51f, 0f);
                line.transform.localScale = new Vector3(1.02f, 0.02f, 1.02f);
                var lr = line.GetComponent<Renderer>();
                lr.sharedMaterial = Mat(new Color(1f, 1f, 1f, 1f));
                lr.sharedMaterial.color = new Color(1f, 1f, 1f, 0.15f);
                Object.Destroy(line.GetComponent<Collider>());
            }
        }

        void BuildObstacles()
        {
            var root = new GameObject("Obstacles").transform;
            root.SetParent(transform, false);
            for (var y = 0; y < _world.Height; y++)
            for (var x = 0; x < _world.Width; x++)
            {
                var id = _world.GetObstacleId(x, y);
                if (id == null) continue;
                var view = CreateObstacleView(id, x, y, _world.GetObstacle(x, y));
                view.transform.SetParent(root, false);
                _obstacleViews[new Vector2Int(x, y)] = view;
            }
        }

        GameObject CreateObstacleView(string id, int x, int y, FarmObstacleType type)
        {
            var root = new GameObject($"Obs_{id}_{x}_{y}");
            root.transform.position = FarmIso.GridToWorld(x, y, FarmIso.TileHeight);

            switch (id)
            {
                case "weed":
                    for (var i = 0; i < 3; i++)
                    {
                        var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        blade.transform.SetParent(root.transform, false);
                        blade.transform.localPosition = new Vector3((i - 1) * 0.15f, 0.25f, 0f);
                        blade.transform.localScale = new Vector3(0.06f, 0.45f, 0.06f);
                        blade.transform.localRotation = Quaternion.Euler(0f, 0f, (i - 1) * 12f);
                        blade.GetComponent<Renderer>().sharedMaterial = _matLeaf;
                        Object.Destroy(blade.GetComponent<Collider>());
                    }
                    break;
                case "bush":
                    AddSphere(root.transform, new Vector3(0f, 0.35f, 0f), new Vector3(0.7f, 0.5f, 0.6f), _matLeaf);
                    AddSphere(root.transform, new Vector3(-0.15f, 0.45f, 0.05f), new Vector3(0.4f, 0.35f, 0.35f), _matAutumn);
                    break;
                case "stump":
                    AddCylinder(root.transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.45f, 0.2f, 0.45f), _matWood);
                    AddCylinder(root.transform, new Vector3(0f, 0.38f, 0f), new Vector3(0.48f, 0.05f, 0.48f), _matWood);
                    break;
                case "tree":
                    AddCylinder(root.transform, new Vector3(0f, 0.55f, 0f), new Vector3(0.18f, 0.55f, 0.18f), _matWood);
                    AddSphere(root.transform, new Vector3(0f, 1.15f, 0f), new Vector3(0.85f, 0.7f, 0.85f), _matLeaf);
                    AddSphere(root.transform, new Vector3(0.15f, 1.3f, 0.05f), new Vector3(0.45f, 0.4f, 0.45f), _matAutumn);
                    break;
                case "rock":
                    AddSphere(root.transform, new Vector3(0f, 0.28f, 0f), new Vector3(0.55f, 0.4f, 0.5f), _matRock);
                    break;
                case "boulder":
                    AddSphere(root.transform, new Vector3(0f, 0.4f, 0f), new Vector3(0.85f, 0.65f, 0.75f), _matRock);
                    AddSphere(root.transform, new Vector3(0.2f, 0.35f, 0.1f), new Vector3(0.45f, 0.4f, 0.4f), _matRock);
                    break;
            }

            // Level badge
            var badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            badge.name = "Badge";
            badge.transform.SetParent(root.transform, false);
            badge.transform.localPosition = new Vector3(0f, id == "tree" ? 1.7f : 0.95f, 0f);
            badge.transform.localScale = new Vector3(0.45f, 0.18f, 0.08f);
            badge.GetComponent<Renderer>().sharedMaterial = _matBadge;
            Object.Destroy(badge.GetComponent<Collider>());

            var label = new GameObject("Label");
            label.transform.SetParent(badge.transform, false);
            var tm = label.AddComponent<TextMesh>();
            tm.text = $"Lv.{type.requiredLevel}";
            tm.fontSize = 32;
            tm.characterSize = 0.06f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            label.transform.localPosition = new Vector3(0f, 0f, -0.6f);

            // Billboard badge toward camera each frame via helper
            badge.AddComponent<FarmBillboard>();

            // Click collider
            var hit = root.AddComponent<BoxCollider>();
            hit.center = new Vector3(0f, 0.5f, 0f);
            hit.size = new Vector3(0.9f, 1.2f, 0.9f);
            return root;
        }

        static void AddSphere(Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.Destroy(go.GetComponent<Collider>());
        }

        static void AddCylinder(Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.Destroy(go.GetComponent<Collider>());
        }

        void BuildPlayer()
        {
            _playerView = new GameObject("Player");
            _playerView.transform.SetParent(transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(_playerView.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            body.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            body.GetComponent<Renderer>().sharedMaterial = _matPlayer;
            Object.Destroy(body.GetComponent<Collider>());

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(_playerView.transform, false);
            head.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            head.transform.localScale = Vector3.one * 0.38f;
            head.GetComponent<Renderer>().sharedMaterial = Mat(new Color(0.88f, 0.72f, 0.58f));
            Object.Destroy(head.GetComponent<Collider>());

            SyncPlayer();
        }

        void BuildHover()
        {
            _hover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _hover.name = "Hover";
            _hover.transform.SetParent(transform, false);
            _hover.transform.localScale = new Vector3(FarmIso.TileSize * 0.95f, 0.05f, FarmIso.TileSize * 0.95f);
            var m = Mat(new Color(0.91f, 0.69f, 0.35f));
            m.color = new Color(0.91f, 0.69f, 0.35f, 0.55f);
            _hover.GetComponent<Renderer>().sharedMaterial = m;
            Object.Destroy(_hover.GetComponent<Collider>());
            _hover.SetActive(false);
        }

        public void SyncPlayer()
        {
            if (_playerView == null || _world == null) return;
            var p = FarmIso.GridToWorld(_world.Player.X, _world.Player.Y, FarmIso.TileHeight);
            _playerView.transform.position = p;
        }

        public void SetHover(Vector2Int? cell)
        {
            if (_hover == null) return;
            if (cell == null || !_world.InBounds(cell.Value.x, cell.Value.y))
            {
                _hover.SetActive(false);
                return;
            }
            _hover.SetActive(true);
            _hover.transform.position = FarmIso.GridToWorld(cell.Value.x, cell.Value.y, FarmIso.TileHeight + 0.05f);
        }

        public void RemoveObstacle(int x, int y)
        {
            var key = new Vector2Int(x, y);
            if (_obstacleViews.TryGetValue(key, out var go))
            {
                Destroy(go);
                _obstacleViews.Remove(key);
            }
        }

        public Vector3 MapCenter => FarmIso.GridToWorld((_world.Width - 1) / 2, (_world.Height - 1) / 2, 0f);
    }

    public class FarmBillboard : MonoBehaviour
    {
        void LateUpdate()
        {
            if (Camera.main == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
    }
}
