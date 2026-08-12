using System.Collections.Generic;
using UnityEngine;

namespace Game.Farm
{
    /// <summary>
    /// 2.5D anime × HD pixel farm visuals for the tiny aesthetic sandbox.
    /// Point-filtered textures, tight ortho frame, chibi billboard farmer.
    /// </summary>
    public class FarmVisuals : MonoBehaviour
    {
        FarmWorld _world;
        readonly Dictionary<Vector2Int, GameObject> _obstacleViews = new();
        Material _matGrassA, _matGrassB, _matTilled, _matWood, _matLeaf, _matLeafHi, _matRock, _matCloth, _matBadge, _matGrid;
        GameObject _playerView;
        GameObject _hover;

        public void Build(FarmWorld world)
        {
            _world = world;
            BuildMaterials();
            BuildAtmosphere();
            BuildGroundPlate();
            BuildFringe();
            BuildTiles();
            BuildObstacles();
            BuildPlayer();
            BuildHover();
        }

        void BuildMaterials()
        {
            _matGrassA = FarmPixelArt.MakePixelMat(
                new Color(0.22f, 0.48f, 0.30f),
                new Color(0.34f, 0.62f, 0.36f), 16, 0.4f);
            _matGrassB = FarmPixelArt.MakePixelMat(
                new Color(0.18f, 0.40f, 0.26f),
                new Color(0.28f, 0.52f, 0.30f), 16, 0.35f);
            _matTilled = FarmPixelArt.MakePixelMat(
                new Color(0.48f, 0.30f, 0.18f),
                new Color(0.62f, 0.40f, 0.24f), 12, 0.45f);
            _matWood = FarmPixelArt.MakeFlatPixel(new Color(0.42f, 0.26f, 0.14f));
            _matLeaf = FarmPixelArt.MakeFlatPixel(new Color(0.18f, 0.46f, 0.26f));
            _matLeafHi = FarmPixelArt.MakeFlatPixel(new Color(0.82f, 0.52f, 0.18f));
            _matRock = FarmPixelArt.MakePixelMat(
                new Color(0.40f, 0.38f, 0.42f),
                new Color(0.62f, 0.58f, 0.56f), 10, 0.4f);
            _matCloth = FarmPixelArt.MakeFlatPixel(new Color(0.28f, 0.42f, 0.72f));
            _matBadge = FarmPixelArt.MakeFlatPixel(new Color(0.08f, 0.08f, 0.12f));
            _matGrid = FarmPixelArt.MakeFlatPixel(new Color(1f, 1f, 1f));
        }

        void BuildAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.42f, 0.28f, 0.38f);
            RenderSettings.fogDensity = 0.045f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.48f, 0.62f);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.78f, 0.52f);
            sun.intensity = 0.85f;
            sun.shadows = LightShadows.None; // flatter, more pixel/anime
            sun.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
            sun.transform.SetParent(transform, false);

            // Soft key spotlight on the 2×2 (BD2 victory-stage feel)
            var spot = new GameObject("StageSpot").AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1f, 0.88f, 0.65f);
            spot.intensity = 1.8f;
            spot.range = 12f;
            spot.spotAngle = 55f;
            spot.transform.position = FarmIso.GridToWorld(0, 0, 0f) + new Vector3(0.3f, 5.5f, -0.2f);
            spot.transform.LookAt(FarmIso.GridToWorld(0, 1, 0f));
            spot.transform.SetParent(transform, false);
        }

        void BuildGroundPlate()
        {
            // Dark stage under the plot — miniature diorama base
            var plate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            plate.name = "StagePlate";
            plate.transform.SetParent(transform, false);
            plate.transform.position = FarmIso.GridToWorld(0, 0, -0.08f) + new Vector3(0f, -0.12f, FarmIso.TileSize * 0.5f);
            plate.transform.localScale = new Vector3(4.2f, 0.08f, 4.2f);
            plate.GetComponent<Renderer>().sharedMaterial =
                FarmPixelArt.MakeFlatPixel(new Color(0.08f, 0.09f, 0.12f));
            Object.Destroy(plate.GetComponent<Collider>());
        }

        void BuildFringe()
        {
            var root = new GameObject("Fringe").transform;
            root.SetParent(transform, false);
            var center = FarmIso.GridToWorld(0, 0) + new Vector3(0f, 0f, FarmIso.TileSize * 0.5f);
            // Sparse silhouette bushes — don’t crowd the 2×2 read
            for (var i = 0; i < 8; i++)
            {
                var ang = (i / 8f) * Mathf.PI * 2f + 0.4f;
                var dist = 2.6f + (i % 2) * 0.25f;
                var p = center + new Vector3(Mathf.Cos(ang) * dist, 0f, Mathf.Sin(ang) * dist * 0.7f);
                var bush = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bush.name = "Fringe";
                bush.transform.SetParent(root, false);
                bush.transform.position = p + Vector3.up * 0.28f;
                bush.transform.localScale = new Vector3(0.55f, 0.55f, 0.45f);
                bush.transform.rotation = Quaternion.Euler(0f, i * 35f, 0f);
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
                go.transform.localScale = new Vector3(FarmIso.TileSize * 0.94f, FarmIso.TileHeight, FarmIso.TileSize * 0.94f);
                var soil = _world.GetSoil(x, y);
                go.GetComponent<Renderer>().sharedMaterial = soil == FarmSoilKind.Tilled
                    ? _matTilled
                    : ((x + y) % 2 == 0 ? _matGrassA : _matGrassB);

                // Bright pixel grid rim (BD2 tactical overlay, subtle)
                var rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rim.name = "Rim";
                rim.transform.SetParent(go.transform, false);
                rim.transform.localPosition = new Vector3(0f, 0.52f, 0f);
                rim.transform.localScale = new Vector3(1.04f, 0.03f, 1.04f);
                var rr = rim.GetComponent<Renderer>();
                rr.sharedMaterial = _matGrid;
                // dim via second mat instance
                rr.sharedMaterial = FarmPixelArt.MakeFlatPixel(new Color(0.95f, 0.9f, 0.75f));
                Object.Destroy(rim.GetComponent<Collider>());
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
                    for (var i = 0; i < 4; i++)
                    {
                        var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        blade.transform.SetParent(root.transform, false);
                        blade.transform.localPosition = new Vector3((i - 1.5f) * 0.12f, 0.28f, (i % 2) * 0.05f);
                        blade.transform.localScale = new Vector3(0.07f, 0.5f + i * 0.04f, 0.07f);
                        blade.transform.localRotation = Quaternion.Euler(0f, 0f, (i - 1.5f) * 10f);
                        blade.GetComponent<Renderer>().sharedMaterial = i % 2 == 0 ? _matLeaf : _matLeafHi;
                        Object.Destroy(blade.GetComponent<Collider>());
                    }
                    break;
                case "rock":
                    // Faceted pixel rock — stacked cubes
                    AddCube(root.transform, new Vector3(0f, 0.28f, 0f), new Vector3(0.55f, 0.4f, 0.5f), _matRock);
                    AddCube(root.transform, new Vector3(0.18f, 0.38f, 0.05f), new Vector3(0.32f, 0.28f, 0.3f), _matRock);
                    break;
                case "tree":
                    AddCube(root.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.2f, 0.9f, 0.2f), _matWood);
                    AddCube(root.transform, new Vector3(0f, 1.15f, 0f), new Vector3(0.85f, 0.55f, 0.85f), _matLeaf);
                    AddCube(root.transform, new Vector3(0.2f, 1.35f, 0.1f), new Vector3(0.4f, 0.35f, 0.4f), _matLeafHi);
                    AddCube(root.transform, new Vector3(-0.15f, 1.25f, -0.1f), new Vector3(0.35f, 0.3f, 0.35f), _matLeaf);
                    break;
            }

            var badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            badge.name = "Badge";
            badge.transform.SetParent(root.transform, false);
            badge.transform.localPosition = new Vector3(0f, id == "tree" ? 1.85f : 0.85f, 0f);
            badge.transform.localScale = new Vector3(0.5f, 0.2f, 0.08f);
            badge.GetComponent<Renderer>().sharedMaterial = _matBadge;
            Object.Destroy(badge.GetComponent<Collider>());
            badge.AddComponent<FarmBillboard>();

            var label = new GameObject("Label");
            label.transform.SetParent(badge.transform, false);
            var tm = label.AddComponent<TextMesh>();
            tm.text = $"Lv.{type.requiredLevel}";
            tm.fontSize = 28;
            tm.characterSize = 0.055f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 0.92f, 0.75f);
            label.transform.localPosition = new Vector3(0f, 0f, -0.7f);

            var hit = root.AddComponent<BoxCollider>();
            hit.center = new Vector3(0f, 0.55f, 0f);
            hit.size = new Vector3(0.95f, 1.3f, 0.95f);
            return root;
        }

        static void AddCube(Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
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

            // Body — chibi proportions
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(_playerView.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            body.transform.localScale = new Vector3(0.38f, 0.45f, 0.28f);
            body.GetComponent<Renderer>().sharedMaterial = _matCloth;
            Object.Destroy(body.GetComponent<Collider>());

            // Cape accent
            var cape = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cape.transform.SetParent(_playerView.transform, false);
            cape.transform.localPosition = new Vector3(0.05f, 0.45f, 0.16f);
            cape.transform.localScale = new Vector3(0.3f, 0.4f, 0.06f);
            cape.GetComponent<Renderer>().sharedMaterial =
                FarmPixelArt.MakeFlatPixel(new Color(0.78f, 0.28f, 0.32f));
            Object.Destroy(cape.GetComponent<Collider>());

            // Anime face billboard
            var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
            face.name = "Face";
            face.transform.SetParent(_playerView.transform, false);
            face.transform.localPosition = new Vector3(0f, 0.92f, 0f);
            face.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            var faceMat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent"));
            faceMat.mainTexture = FarmPixelArt.MakeChibiFace(32);
            face.GetComponent<Renderer>().sharedMaterial = faceMat;
            Object.Destroy(face.GetComponent<Collider>());
            face.AddComponent<FarmBillboard>();

            SyncPlayer();
        }

        void BuildHover()
        {
            _hover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _hover.name = "Hover";
            _hover.transform.SetParent(transform, false);
            _hover.transform.localScale = new Vector3(FarmIso.TileSize * 0.96f, 0.06f, FarmIso.TileSize * 0.96f);
            _hover.GetComponent<Renderer>().sharedMaterial =
                FarmPixelArt.MakeFlatPixel(new Color(0.95f, 0.75f, 0.35f));
            Object.Destroy(_hover.GetComponent<Collider>());
            _hover.SetActive(false);
        }

        public void SyncPlayer()
        {
            if (_playerView == null || _world == null) return;
            _playerView.transform.position = FarmIso.GridToWorld(_world.Player.X, _world.Player.Y, FarmIso.TileHeight);
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
            _hover.transform.position = FarmIso.GridToWorld(cell.Value.x, cell.Value.y, FarmIso.TileHeight + 0.06f);
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

        public Vector3 MapCenter =>
            FarmIso.GridToWorld((_world.Width - 1) / 2, (_world.Height - 1) / 2, 0f)
            + new Vector3(0f, 0.2f, FarmIso.TileSize * 0.15f);
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
