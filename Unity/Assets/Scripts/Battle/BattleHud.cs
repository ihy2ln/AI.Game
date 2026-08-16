using UnityEngine;

namespace Game.Battle
{
    /// <summary>IMGUI HUD (no Canvas wiring), mirrors FarmHud's approach: HP bars,
    /// battle log line, damage numbers, win/lose banner with a restart button.</summary>
    public class BattleHud : MonoBehaviour
    {
        BattleController _ctrl;
        Camera _cam;
        BattleVisuals _visuals;
        GUIStyle _title, _body, _name, _big, _sub, _dmg, _btn, _prompt, _logEntry, _logRound;

        bool _showLog;
        Vector2 _logScroll;

        public void Init(BattleController ctrl, Camera cam, BattleVisuals visuals)
        {
            _ctrl = ctrl;
            _cam = cam;
            _visuals = visuals;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                _showLog = !_showLog;
                if (_showLog) _logScroll = new Vector2(0, float.MaxValue);
            }
        }

        void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.91f, 0.69f, 0.35f) },
            };
            _body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14, normal = { textColor = new Color(0.94f, 0.90f, 0.83f) }, wordWrap = true,
            };
            _name = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = Color.white } };
            _big = new GUIStyle(GUI.skin.label)
            {
                fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow },
            };
            _sub = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            _dmg = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _btn = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold };
            _prompt = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) },
            };
            _logEntry = new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }, wordWrap = true };
            _logRound = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.91f, 0.69f, 0.35f) } };
        }

        void OnGUI()
        {
            if (_ctrl == null || _ctrl.World == null) return;
            EnsureStyles();
            int w = Screen.width, h = Screen.height;

            GUI.Label(new Rect(0, 12, w, 30), $"AI.Game -- Battle ({(_ctrl.ManualMode ? "manual" : "auto")})", _title);
            DrawModeToggle(w);

            DrawRoster(_ctrl.World.PlayerUnits, new Rect(16, 56, 220, 28), false);
            DrawRoster(_ctrl.World.EnemyUnits, new Rect(w - 236, 56, 220, 28), true);

            GUI.Box(new Rect(12, h - 56, Mathf.Min(700, w - 24), 40), GUIContent.none);
            GUI.Label(new Rect(24, h - 48, Mathf.Min(680, w - 48), 30), _ctrl.LastAction, _body);

            DrawDamageNumbers();
            DrawTargetPrompt(w);
            DrawLogToggle(w);
            if (_showLog) DrawLogPanel(w, h);

            if (_ctrl.Outcome != BattleOutcome.InProgress) DrawOutcomeBanner(w, h);
        }

        void DrawLogToggle(int w)
        {
            string label = _showLog ? "Hide Log (L)" : "Turn Log (L)";
            if (GUI.Button(new Rect(w - 150, 14, 134, 26), label, _btn)) _showLog = !_showLog;
        }

        void DrawLogPanel(int w, int h)
        {
            var panel = new Rect(w / 2f - 220, 130, 440, Mathf.Min(h - 220, 420));
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 10, panel.y + 6, panel.width - 20, 22), "Turn Log", _title);

            var viewRect = new Rect(panel.x + 8, panel.y + 32, panel.width - 16, panel.height - 40);
            float lineHeight = 20f;
            var entries = _ctrl.Log.Entries;
            float contentHeight = entries.Count * lineHeight + CountRoundHeaders(entries) * 18f + 8f;
            var contentRect = new Rect(0, 0, viewRect.width - 20, contentHeight);

            _logScroll = GUI.BeginScrollView(viewRect, _logScroll, contentRect);
            float y = 0f;
            int lastRound = -1;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Round != lastRound)
                {
                    GUI.Label(new Rect(4, y, contentRect.width - 8, 18), $"-- Round {entry.Round} --", _logRound);
                    y += 18f;
                    lastRound = entry.Round;
                }
                GUI.Label(new Rect(10, y, contentRect.width - 14, lineHeight), entry.Text, _logEntry);
                y += lineHeight;
            }
            GUI.EndScrollView();
        }

        static int CountRoundHeaders(System.Collections.Generic.IReadOnlyList<BattleLogEntry> entries)
        {
            int count = 0, lastRound = -1;
            foreach (var entry in entries)
            {
                if (entry.Round == lastRound) continue;
                lastRound = entry.Round;
                count++;
            }
            return count;
        }

        void DrawRoster(System.Collections.Generic.IEnumerable<BattleUnit> units, Rect origin, bool rightAligned)
        {
            float y = origin.y;
            foreach (var unit in units)
            {
                DrawHpBar(new Rect(origin.x, y, origin.width, origin.height), unit, rightAligned);
                y += origin.height + 14;
            }
        }

        void DrawHpBar(Rect rect, BattleUnit unit, bool rightAligned)
        {
            var nameStyle = new GUIStyle(_name) { alignment = rightAligned ? TextAnchor.UpperRight : TextAnchor.UpperLeft };
            GUI.Label(new Rect(rect.x, rect.y - 16, rect.width, 16), unit.Definition.displayName, nameStyle);

            GUI.Box(rect, GUIContent.none);
            float pct = unit.Stats.hp > 0 ? (float)unit.CurrentHp / unit.Stats.hp : 0f;
            float fillWidth = (rect.width - 6) * pct;
            float fillX = rightAligned ? rect.x + 3 + (rect.width - 6 - fillWidth) : rect.x + 3;
            var fill = new Rect(fillX, rect.y + 3, fillWidth, rect.height - 6);

            var old = GUI.color;
            GUI.color = !unit.IsAlive ? Color.gray : (pct > 0.5f ? Color.green : (pct > 0.2f ? Color.yellow : Color.red));
            GUI.DrawTexture(fill, Texture2D.whiteTexture);
            GUI.color = old;

            GUI.Label(rect, $"{unit.CurrentHp}/{unit.Stats.hp}", new GUIStyle(_name) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } });
        }

        void DrawModeToggle(int w)
        {
            string label = _ctrl.ManualMode ? "Mode: Manual (T)" : "Mode: Auto (T)";
            if (GUI.Button(new Rect(w / 2f - 90, 14, 180, 30), label, _btn)) _ctrl.ToggleMode();
        }

        void DrawTargetPrompt(int w)
        {
            if (_ctrl.PendingActor == null || _cam == null || _visuals == null) return;

            GUI.Label(new Rect(0, 92, w, 26), $"{_ctrl.PendingActor.Definition.displayName}'s turn -- tap a highlighted target",
                _prompt);

            foreach (var target in _ctrl.PendingTargets)
            {
                var pos = _visuals.GetUnitWorldPosition(target);
                var screen = _cam.WorldToScreenPoint(pos);
                if (screen.z < 0) continue;
                var guiPos = new Vector2(screen.x, Screen.height - screen.y);

                var old = GUI.color;
                GUI.color = new Color(1f, 0.9f, 0.2f, 0.9f);
                var ring = new Rect(guiPos.x - 55, guiPos.y - 70, 110, 110);
                GUI.Box(ring, GUIContent.none);
                GUI.color = old;
            }
        }

        void DrawDamageNumbers()
        {
            if (_cam == null) return;
            foreach (var dmg in _ctrl.DamageNumbers)
            {
                var screen = _cam.WorldToScreenPoint(dmg.WorldPos + Vector3.up * (dmg.Age * 1.2f));
                if (screen.z < 0) continue;
                var guiPos = new Vector2(screen.x, Screen.height - screen.y);
                var style = new GUIStyle(_dmg);
                var c = dmg.Color; c.a = Mathf.Clamp01(1.4f - dmg.Age);
                style.normal.textColor = c;
                GUI.Label(new Rect(guiPos.x - 40, guiPos.y - 20, 80, 30), dmg.Text, style);
            }
        }

        void DrawOutcomeBanner(int w, int h)
        {
            string label = _ctrl.Outcome == BattleOutcome.PlayerVictory ? "VICTORY" : "DEFEAT";
            GUI.Label(new Rect(0, h / 2f - 60, w, 60), label, _big);
            GUI.Label(new Rect(0, h / 2f, w, 30), "Press R or tap below to fight again", _sub);
            if (GUI.Button(new Rect(w / 2f - 80, h / 2f + 36, 160, 44), "Restart", _btn)) _ctrl.Restart();
        }
    }
}
