using UnityEngine;

namespace Game.Farm
{
    /// <summary>Input + clearing loop for the starter farm.</summary>
    public class FarmController : MonoBehaviour
    {
        public FarmWorld World { get; private set; }
        public string LastMessage { get; private set; } = "WASD / arrows move · E / Space / click clears.";
        public string StatusKind { get; private set; } = "info";

        FarmVisuals _visuals;
        Camera _cam;

        public void Init(FarmWorld world, FarmVisuals visuals, Camera cam)
        {
            World = world;
            _visuals = visuals;
            _cam = cam;
        }

        void Update()
        {
            if (World == null) return;
            HandleKeyboard();
            HandlePointer();
        }

        void HandleKeyboard()
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) TryStep(0, -1);
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) TryStep(0, 1);
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) TryStep(-1, 0);
            if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) TryStep(1, 0);
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                AttemptClear(World.Player.X, World.Player.Y);
        }

        void HandlePointer()
        {
            if (_cam == null) return;
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 100f))
            {
                var cell = FarmIso.WorldToGrid(hit.point);
                _visuals.SetHover(cell);

                if (Input.GetMouseButtonDown(0) && World.InBounds(cell.x, cell.y))
                {
                    var dx = Mathf.Abs(cell.x - World.Player.X) + Mathf.Abs(cell.y - World.Player.Y);
                    if (dx == 0) AttemptClear(cell.x, cell.y);
                    else if (dx == 1 && !World.BlocksMovement(cell.x, cell.y))
                    {
                        World.Player.X = cell.x;
                        World.Player.Y = cell.y;
                        _visuals.SyncPlayer();
                        RefreshTileMessage();
                    }
                    else if (World.GetObstacle(cell.x, cell.y) != null)
                    {
                        World.CanClear(cell.x, cell.y, out var reason);
                        Push(reason, World.Player.Level >= World.GetObstacle(cell.x, cell.y).requiredLevel ? "info" : "warn");
                    }
                }
            }
            else _visuals.SetHover(null);
        }

        void TryStep(int dx, int dy)
        {
            if (World.TryMove(dx, dy))
            {
                _visuals.SyncPlayer();
                RefreshTileMessage();
                return;
            }

            var tx = World.Player.X + dx;
            var ty = World.Player.Y + dy;
            if (World.InBounds(tx, ty) && World.BlocksMovement(tx, ty))
            {
                var obs = World.GetObstacle(tx, ty);
                World.CanClear(tx, ty, out var reason);
                Push(World.Player.Level >= obs.requiredLevel
                    ? $"Press E to clear {obs.label}."
                    : reason, World.Player.Level >= obs.requiredLevel ? "info" : "warn");
            }
        }

        void AttemptClear(int x, int y)
        {
            if (!World.TryClear(x, y, out var cleared, out var xp, out var levels, out var message))
            {
                Push(message, "warn");
                return;
            }

            _visuals.RemoveObstacle(x, y);
            Push(message, "ok");
            if (levels > 0) Push($"Farm level up! Now Lv.{World.Player.Level}.", "level");
            if (World.RemainingObstacles() == 0) Push("Starter plot cleared. Expand the grid next.", "level");
            RefreshTileMessage();
        }

        void RefreshTileMessage()
        {
            var obs = World.GetObstacle(World.Player.X, World.Player.Y);
            if (obs == null)
            {
                var soil = World.GetSoil(World.Player.X, World.Player.Y);
                Push(soil == FarmSoilKind.Tilled ? "Tilled soil — ready for crops later." : "Clear grass.", "info");
            }
            else
            {
                World.CanClear(World.Player.X, World.Player.Y, out var reason);
                Push($"{obs.label} — {reason}", World.Player.Level >= obs.requiredLevel ? "ok" : "warn");
            }
        }

        void Push(string msg, string kind)
        {
            LastMessage = msg;
            StatusKind = kind;
        }

        /// <summary>On-screen pad hooks for mobile / Game view buttons.</summary>
        public void UiMove(int dx, int dy) => TryStep(dx, dy);
        public void UiClear() => AttemptClear(World.Player.X, World.Player.Y);
    }
}
