using UnityEngine;

namespace Game.Battle
{
    /// <summary>IMGUI HUD (no Canvas wiring), mirrors FarmHud's approach: HP bars,
    /// battle log line/review panel, damage numbers, win/lose banner, and the modern-UX
    /// layer -- pause menu, settings, undo/redo, keybind legend.</summary>
    public class BattleHud : MonoBehaviour
    {
        BattleController _ctrl;
        Camera _cam;
        BattleVisuals _visuals;
        GUIStyle _title, _body, _name, _big, _sub, _dmg, _btn, _smallBtn, _prompt, _logEntry, _logRound, _toggle;

        bool _showLog;
        Vector2 _logScroll;
        bool _showSettings;
        bool _showKeybinds;
        bool _confirmRestart;

        public void Init(BattleController ctrl, Camera cam, BattleVisuals visuals, bool logOpenByDefault)
        {
            _ctrl = ctrl;
            _cam = cam;
            _visuals = visuals;
            _showLog = logOpenByDefault;
            if (_showLog) _logScroll = new Vector2(0, float.MaxValue);
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
            _smallBtn = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold };
            _prompt = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) },
            };
            _logEntry = new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }, wordWrap = true };
            _logRound = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.91f, 0.69f, 0.35f) } };
            _toggle = new GUIStyle(GUI.skin.toggle) { fontSize = 14, normal = { textColor = new Color(0.94f, 0.90f, 0.83f) } };
        }

        void OnGUI()
        {
            if (_ctrl == null || _ctrl.World == null) return;
            EnsureStyles();
            int w = Screen.width, h = Screen.height;

            GUI.Label(new Rect(0, 12, w, 30), $"AI.Game -- Battle ({(_ctrl.ManualMode ? "manual" : "auto")})", _title);
            DrawModeToggle(w);
            DrawUndoRedoButtons(w);
            DrawPauseButton(w);
            DrawLogToggle(w);
            DrawKeybindToggle(w);

            DrawRoster(_ctrl.World.PlayerUnits, new Rect(16, 90, 220, 28), false);
            DrawRoster(_ctrl.World.EnemyUnits, new Rect(w - 236, 90, 220, 28), true);

            GUI.Box(new Rect(12, h - 56, Mathf.Min(700, w - 24), 40), GUIContent.none);
            GUI.Label(new Rect(24, h - 48, Mathf.Min(680, w - 48), 30), _ctrl.LastAction, _body);

            DrawDamageNumbers();
            DrawTargetPrompt(w);
            if (_showLog) DrawLogPanel(w, h);
            if (_showKeybinds) DrawKeybindPanel(w, h);

            if (_ctrl.Outcome != BattleOutcome.InProgress && !_ctrl.Paused) DrawOutcomeBanner(w, h);
            if (_ctrl.Paused) DrawPauseOverlay(w, h);
        }

        void DrawUndoRedoButtons(int w)
        {
            GUI.enabled = _ctrl.CanUndo;
            if (GUI.Button(new Rect(w / 2f - 150, 14, 56, 30), "Undo", _smallBtn)) _ctrl.Undo();
            GUI.enabled = _ctrl.CanRedo;
            if (GUI.Button(new Rect(w / 2f + 94, 14, 56, 30), "Redo", _smallBtn)) _ctrl.Redo();
            GUI.enabled = true;
        }

        void DrawPauseButton(int w)
        {
            if (GUI.Button(new Rect(w - 304, 14, 140, 26), "Pause (Esc)", _btn)) _ctrl.SetPaused(true);
        }

        void DrawLogToggle(int w)
        {
            string label = _showLog ? "Hide Log (L)" : "Turn Log (L)";
            if (GUI.Button(new Rect(w - 150, 14, 134, 26), label, _btn)) _showLog = !_showLog;
        }

        void DrawKeybindToggle(int w)
        {
            if (GUI.Button(new Rect(w - 150, 44, 134, 24), "Keybinds (?)", _smallBtn)) _showKeybinds = !_showKeybinds;
        }

        void DrawKeybindPanel(int w, int h)
        {
            var panel = new Rect(w / 2f - 160, 130, 320, 190);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 10, panel.y + 6, panel.width - 20, 22), "Keybinds", _title);
            string[] lines =
            {
                "T -- toggle auto / manual mode",
                "L -- open / close turn log",
                "Esc -- pause",
                "Ctrl+Z -- undo last turn",
                "Ctrl+Y -- redo turn",
                "R -- restart (after battle ends)",
                "Click -- choose a highlighted target",
            };
            float y = panel.y + 32;
            foreach (var line in lines)
            {
                GUI.Label(new Rect(panel.x + 12, y, panel.width - 24, 20), line, _logEntry);
                y += 20;
            }
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

            GUI.Label(new Rect(0, 126, w, 26), $"{_ctrl.PendingActor.Definition.displayName}'s turn -- tap a highlighted target",
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

        void DrawPauseOverlay(int w, int h)
        {
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = old;

            if (_confirmRestart) { DrawConfirmRestartPanel(w, h); return; }
            if (_showSettings) { DrawSettingsPanel(w, h); return; }

            const float panelW = 320f, panelH = 320f;
            var panel = new Rect(w / 2f - panelW / 2f, h / 2f - panelH / 2f, panelW, panelH);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x, panel.y + 10, panel.width, 30), "Paused", new GUIStyle(_title) { alignment = TextAnchor.MiddleCenter });

            float bx = panel.x + 20, bw = panel.width - 40, by = panel.y + 50;
            if (GUI.Button(new Rect(bx, by, bw, 34), "Resume (Esc)", _btn)) _ctrl.SetPaused(false);
            by += 40;

            GUI.enabled = _ctrl.CanUndo;
            if (GUI.Button(new Rect(bx, by, bw, 34), "Undo Turn (Ctrl+Z)", _btn)) _ctrl.Undo();
            GUI.enabled = _ctrl.CanRedo;
            by += 40;
            if (GUI.Button(new Rect(bx, by, bw, 34), "Redo Turn (Ctrl+Y)", _btn)) _ctrl.Redo();
            GUI.enabled = true;
            by += 40;

            if (GUI.Button(new Rect(bx, by, bw, 34), "Settings", _btn)) _showSettings = true;
            by += 40;

            if (GUI.Button(new Rect(bx, by, bw, 34), "Restart Whole Battle", _btn))
            {
                if (_ctrl.Outcome == BattleOutcome.InProgress) _confirmRestart = true;
                else _ctrl.Restart();
            }
            by += 40;

            if (GUI.Button(new Rect(bx, by, bw, 34), "Quit", _btn)) Application.Quit();
        }

        void DrawConfirmRestartPanel(int w, int h)
        {
            var panel = new Rect(w / 2f - 190, h / 2f - 75, 380, 150);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 16, panel.y + 14, panel.width - 32, 50),
                "Restart the whole battle? This battle's progress can't be recovered afterward (undo history resets too).",
                _body);
            if (GUI.Button(new Rect(panel.x + 20, panel.y + 96, 160, 36), "Cancel", _btn)) _confirmRestart = false;
            if (GUI.Button(new Rect(panel.x + 200, panel.y + 96, 160, 36), "Restart", _btn))
            {
                _confirmRestart = false;
                _ctrl.Restart();
            }
        }

        void DrawSettingsPanel(int w, int h)
        {
            var settings = _ctrl.Settings;
            const float panelW = 400f, panelH = 400f;
            var panel = new Rect(w / 2f - panelW / 2f, h / 2f - panelH / 2f, panelW, panelH);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 16, panel.y + 10, panel.width - 32, 26), "Settings", _title);

            float x = panel.x + 16, cw = panel.width - 32, y = panel.y + 48;
            GUI.Label(new Rect(x, y, cw, 20), "Battle speed", _body);
            y += 24;
            var speeds = BattleSettings.SpeedOptions;
            float speedBtnW = (cw - (speeds.Length - 1) * 8) / speeds.Length;
            for (int i = 0; i < speeds.Length; i++)
            {
                var r = new Rect(x + i * (speedBtnW + 8), y, speedBtnW, 32);
                bool active = Mathf.Approximately(settings.SpeedMultiplier, speeds[i]);
                var prevBg = GUI.backgroundColor;
                if (active) GUI.backgroundColor = new Color(1f, 0.85f, 0.3f);
                if (GUI.Button(r, $"{speeds[i]:0.#}x", _btn)) _ctrl.SetSpeedMultiplier(speeds[i]);
                GUI.backgroundColor = prevBg;
            }
            y += 44;

            settings.ShowDamageNumbers = GUI.Toggle(new Rect(x, y, cw, 24), settings.ShowDamageNumbers, " Show damage numbers", _toggle);
            y += 30;
            settings.LogOpenByDefault = GUI.Toggle(new Rect(x, y, cw, 24), settings.LogOpenByDefault, " Open turn log by default", _toggle);
            y += 30;
            settings.AutoModeDefault = GUI.Toggle(new Rect(x, y, cw, 24), settings.AutoModeDefault, " Start battles in auto mode", _toggle);
            y += 38;

            GUI.Label(new Rect(x, y, cw, 20), $"Master volume -- {Mathf.RoundToInt(settings.MasterVolume * 100)}%", _body);
            y += 24;
            float newVolume = GUI.HorizontalSlider(new Rect(x, y, cw, 20), settings.MasterVolume, 0f, 1f);
            if (!Mathf.Approximately(newVolume, settings.MasterVolume))
            {
                settings.MasterVolume = newVolume;
                AudioListener.volume = newVolume;
            }
            y += 36;

            if (GUI.Button(new Rect(x, y, cw, 36), "Save & Back", _btn))
            {
                settings.Save();
                _showSettings = false;
            }
        }
    }
}
