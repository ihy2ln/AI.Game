using UnityEngine;

namespace Game.Farm
{
    /// <summary>Lightweight BD2-styled HUD drawn with IMGUI (no Canvas wiring).</summary>
    public class FarmHud : MonoBehaviour
    {
        FarmController _ctrl;
        GUIStyle _title, _body, _warn, _ok, _level, _btn;

        public void Init(FarmController ctrl) => _ctrl = ctrl;

        void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.91f, 0.69f, 0.35f) }
            };
            _body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.94f, 0.90f, 0.83f) },
                wordWrap = true
            };
            _warn = new GUIStyle(_body) { normal = { textColor = new Color(1f, 0.55f, 0.5f) } };
            _ok = new GUIStyle(_body) { normal = { textColor = new Color(0.56f, 0.82f, 0.63f) } };
            _level = new GUIStyle(_body) { normal = { textColor = new Color(0.91f, 0.69f, 0.35f) }, fontStyle = FontStyle.Bold };
            _btn = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold };
        }

        void OnGUI()
        {
            if (_ctrl == null || _ctrl.World == null) return;
            EnsureStyles();
            var w = _ctrl.World;
            var p = w.Player;

            GUI.Box(new Rect(12, 12, 260, 118), GUIContent.none);
            GUI.Label(new Rect(24, 18, 240, 24), "AI.Game  ·  Farm", _title);
            GUI.Label(new Rect(24, 46, 240, 20), $"{w.DisplayName}  ·  4×4  ·  BD2 HD-2D", _body);
            GUI.Label(new Rect(24, 68, 240, 20), $"Farm Lv {p.Level}    XP {FormatXp(p)}", _body);
            GUI.Label(new Rect(24, 90, 240, 20), $"Cleared {w.ClearedCount} / {w.ClearedCount + w.RemainingObstacles()}", _body);

            var msgStyle = _ctrl.StatusKind switch
            {
                "warn" => _warn,
                "ok" => _ok,
                "level" => _level,
                _ => _body
            };
            GUI.Box(new Rect(12, Screen.height - 84, Mathf.Min(520, Screen.width - 24), 48), GUIContent.none);
            GUI.Label(new Rect(24, Screen.height - 74, Mathf.Min(500, Screen.width - 48), 36), _ctrl.LastMessage, msgStyle);

            // Touch pad
            var size = 56f;
            var ox = Screen.width - size * 3 - 28;
            var oy = Screen.height - size * 3 - 28;
            if (GUI.Button(new Rect(ox + size, oy, size, size), "▲", _btn)) _ctrl.UiMove(0, -1);
            if (GUI.Button(new Rect(ox, oy + size, size, size), "◀", _btn)) _ctrl.UiMove(-1, 0);
            if (GUI.Button(new Rect(ox + size, oy + size, size, size), "CLR", _btn)) _ctrl.UiClear();
            if (GUI.Button(new Rect(ox + size * 2, oy + size, size, size), "▶", _btn)) _ctrl.UiMove(1, 0);
            if (GUI.Button(new Rect(ox + size, oy + size * 2, size, size), "▼", _btn)) _ctrl.UiMove(0, 1);
        }

        static string FormatXp(FarmPlayerState p)
        {
            var need = p.XpToNext();
            return need == null ? "MAX" : $"{p.Xp}/{need}";
        }
    }
}
