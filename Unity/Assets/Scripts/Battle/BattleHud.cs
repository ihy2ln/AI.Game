using System.Collections.Generic;
using UnityEngine;
using Game.Data;

namespace Game.Battle
{
    /// <summary>IMGUI HUD (no Canvas wiring), mirrors FarmHud's approach: HP/MP bars,
    /// battle log line/review panel, damage numbers, win/lose banner, and the modern-UX
    /// layer -- pause menu, settings, undo/redo, keybind legend.</summary>
    public class BattleHud : MonoBehaviour
    {
        BattleController _ctrl;
        Camera _cam;
        BattleVisuals _visuals;
        GUIStyle _title, _body, _name, _big, _sub, _dmg, _btn, _smallBtn, _iconBtn, _prompt, _actorName, _logEntry, _logRound, _toggle, _barLabel;

        bool _showLog;
        Vector2 _logScroll;
        bool _showSettings;
        bool _showKeybinds;
        bool _confirmRestart;

        // Press-and-hold on the "SM" icon -- see Update() and DrawActionMenu.
        const float SmHoldSeconds = 0.35f;
        Rect _smButtonRect;
        float _smHoldStartTime = -1f;
        bool _showSkillList;

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

            bool choosingAction = _ctrl != null && _ctrl.Phase == ActionPhase.ChooseAction && _smButtonRect.width > 0f;
            if (!choosingAction)
            {
                _showSkillList = false;
                _smHoldStartTime = -1f;
                return;
            }

            var mouseGui = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            bool overSm = _smButtonRect.Contains(mouseGui);

            if (Input.GetMouseButtonDown(0) && overSm) _smHoldStartTime = Time.unscaledTime;
            if (Input.GetMouseButtonUp(0)) _smHoldStartTime = -1f;
            if (_smHoldStartTime >= 0f && Time.unscaledTime - _smHoldStartTime >= SmHoldSeconds)
            {
                _showSkillList = true;
                _smHoldStartTime = -1f;
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
            _name = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = Color.white } };
            _big = new GUIStyle(GUI.skin.label)
            {
                fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow },
            };
            _sub = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            _dmg = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _btn = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold };
            _smallBtn = new GUIStyle(GUI.skin.button) { fontSize = 12, fontStyle = FontStyle.Bold };
            _iconBtn = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _prompt = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) },
            };
            _actorName = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) },
            };
            _logEntry = new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }, wordWrap = true };
            _logRound = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.91f, 0.69f, 0.35f) } };
            _toggle = new GUIStyle(GUI.skin.toggle) { fontSize = 14, normal = { textColor = new Color(0.94f, 0.90f, 0.83f) } };
            _barLabel = new GUIStyle(GUI.skin.label) { fontSize = 9, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
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

            DrawRoster(_ctrl.World.PlayerUnits, 16, 90, false);
            DrawRoster(_ctrl.World.EnemyUnits, w - 176, 90, true);

            GUI.Box(new Rect(12, h - 56, Mathf.Min(700, w - 24), 40), GUIContent.none);
            GUI.Label(new Rect(24, h - 48, Mathf.Min(680, w - 48), 30), _ctrl.LastAction, _body);

            DrawDamageNumbers();
            if (_ctrl.Phase == ActionPhase.ChooseAction) DrawActionMenu();
            if (_ctrl.Phase == ActionPhase.ChooseBench) DrawBenchMenu(w);
            if (_ctrl.Phase == ActionPhase.ChooseTarget) DrawTargetPrompt(w);
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
            var panel = new Rect(w / 2f - 170, 130, 340, 210);
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
                "BA/R/S -- tap. SM -- press and hold for skill list",
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

        static int CountRoundHeaders(IReadOnlyList<BattleLogEntry> entries)
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

        // Compact per-unit roster readout: name, a thin HP bar, a thinner MP bar below it.
        const float BarWidth = 160f, HpHeight = 11f, MpHeight = 6f, NameHeight = 13f, BarGap = 2f, UnitGap = 9f;

        void DrawRoster(IEnumerable<BattleUnit> units, float x, float y, bool rightAligned)
        {
            foreach (var unit in units)
            {
                DrawUnitBars(x, y, unit, rightAligned);
                y += NameHeight + HpHeight + BarGap + MpHeight + UnitGap;
            }
        }

        void DrawUnitBars(float x, float y, BattleUnit unit, bool rightAligned)
        {
            var nameStyle = new GUIStyle(_name) { alignment = rightAligned ? TextAnchor.UpperRight : TextAnchor.UpperLeft };
            GUI.Label(new Rect(x, y, BarWidth, NameHeight), unit.Definition.displayName, nameStyle);
            y += NameHeight;

            var hpRect = new Rect(x, y, BarWidth, HpHeight);
            GUI.Box(hpRect, GUIContent.none);
            float hpPct = unit.Stats.hp > 0 ? (float)unit.CurrentHp / unit.Stats.hp : 0f;
            DrawBarFill(hpRect, hpPct, rightAligned,
                !unit.IsAlive ? Color.gray : (hpPct > 0.5f ? Color.green : (hpPct > 0.2f ? Color.yellow : Color.red)));
            GUI.Label(hpRect, $"{unit.CurrentHp}/{unit.Stats.hp}", _barLabel);
            y += HpHeight + BarGap;

            var mpRect = new Rect(x, y, BarWidth, MpHeight);
            GUI.Box(mpRect, GUIContent.none);
            float mpPct = unit.MaxMp > 0 ? (float)unit.CurrentMp / unit.MaxMp : 0f;
            DrawBarFill(mpRect, mpPct, rightAligned, new Color(0.2f, 0.45f, 1f));
        }

        static void DrawBarFill(Rect rect, float pct, bool rightAligned, Color color)
        {
            // 1px padding per side, not 2 -- 2px-per-side left zero height for the thin
            // MP bar (height 4-6) once the border was subtracted, which is why it always
            // rendered empty regardless of the underlying value.
            float fillWidth = (rect.width - 2) * Mathf.Clamp01(pct);
            float fillX = rightAligned ? rect.x + 1 + (rect.width - 2 - fillWidth) : rect.x + 1;
            var fill = new Rect(fillX, rect.y + 1, fillWidth, Mathf.Max(1f, rect.height - 2));
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(fill, Texture2D.whiteTexture);
            GUI.color = old;
        }

        void DrawModeToggle(int w)
        {
            string label = _ctrl.ManualMode ? "Mode: Manual (T)" : "Mode: Auto (T)";
            if (GUI.Button(new Rect(w / 2f - 90, 14, 180, 30), label, _btn)) _ctrl.ToggleMode();
        }

        void DrawTargetPrompt(int w)
        {
            if (_ctrl.PendingActor == null || _cam == null || _visuals == null) return;

            GUI.Label(new Rect(0, 126, w, 26), $"{_ctrl.PendingActor.Definition.displayName} -- tap a highlighted target",
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

        // Small icon bar (BA/SM/R/S) anchored at the acting unit's feet, plus its name
        // label just above the icons -- replaces the old full-width centred panel.
        const float IconSize = 32f, IconGap = 4f;

        /// <summary>Keeps a centred UI block fully on screen -- returns the left edge X
        /// for the given desired centre X and total width, clamped so neither edge goes
        /// past the screen margin. Units near the left/right edge of the battlefield
        /// (e.g. Sable in the back column) would otherwise push the action menu or skill
        /// popup partly off-screen.</summary>
        static float ClampedLeftX(float desiredCenterX, float totalWidth, float margin = 10f)
        {
            float half = totalWidth / 2f;
            float min = margin + half;
            float max = Screen.width - margin - half;
            float center = max < min ? Screen.width / 2f : Mathf.Clamp(desiredCenterX, min, max);
            return center - half;
        }

        void DrawActionMenu()
        {
            var actor = _ctrl.PendingActor;
            if (actor == null || _cam == null || _visuals == null) { _smButtonRect = default; return; }

            // Anchored below the acting unit's actual feet -- DockPosition is the
            // sprite's pivot, which is Center (Unity's default sprite import pivot), not
            // Bottom, so it sits at the character's torso, not their feet. Stepping down
            // half the sprite's world-space height (BattleLayout.TargetUnitHeight, what
            // every sprite is normalized to -- see BattleVisuals.BuildUnitView) reaches
            // the visual bottom edge instead. Clamped horizontally so it can't run off
            // the edge of the screen for a back-column/edge unit.
            var feetWorld = _visuals.DockPosition(actor) + Vector3.down * (BattleLayout.TargetUnitHeight / 2f);
            var screen = _cam.WorldToScreenPoint(feetWorld);
            if (screen.z < 0) { _smButtonRect = default; return; }
            float anchorX = screen.x;
            float anchorY = Screen.height - screen.y;

            float nameLeft = ClampedLeftX(anchorX, 140f);
            GUI.Label(new Rect(nameLeft, anchorY + 2, 140, 18), actor.Definition.displayName, _actorName);

            var basicAttack = _ctrl.BasicAttackSkill(actor);
            var skillMoveOptions = _ctrl.SkillMoveOptions(actor);

            var labels = new[] { "BA", "SM", "R", "S" };
            var enabled = new[] { basicAttack != null, skillMoveOptions.Count > 0, _ctrl.CanReposition, _ctrl.CanSub };

            float totalW = labels.Length * IconSize + (labels.Length - 1) * IconGap;
            float startX = ClampedLeftX(anchorX, totalW);
            float y = anchorY + 22f;

            for (int i = 0; i < labels.Length; i++)
            {
                var rect = new Rect(startX + i * (IconSize + IconGap), y, IconSize, IconSize);
                if (labels[i] == "SM") _smButtonRect = rect;

                GUI.enabled = enabled[i];
                bool clicked = GUI.Button(rect, labels[i], _iconBtn);
                GUI.enabled = true;

                // SM's own click is ignored -- it only opens via press-and-hold, tracked
                // in Update() against _smButtonRect (drawn here, polled next frame).
                if (labels[i] == "SM" || !clicked) continue;

                switch (labels[i])
                {
                    case "BA": _ctrl.ChooseSkill(basicAttack); break;
                    case "R": _ctrl.ChooseReposition(); break;
                    case "S": _ctrl.OpenBenchMenu(); break;
                }
            }

            if (_showSkillList) DrawSkillListPopup(actor, skillMoveOptions, startX + totalW / 2f, y);
        }

        void DrawSkillListPopup(BattleUnit actor, IReadOnlyList<SkillDefinition> options, float anchorX, float iconsY)
        {
            const float panelW = 150f, btnH = 26f;
            float panelH = options.Count * (btnH + 4f) + 8f;
            float panelLeft = ClampedLeftX(anchorX, panelW);
            float panelTop = Mathf.Max(4f, iconsY - panelH - 8f);
            var panel = new Rect(panelLeft, panelTop, panelW, panelH);
            GUI.Box(panel, GUIContent.none);

            float y = panel.y + 4f;
            foreach (var skill in options)
            {
                string name = skill.targetsAllies ? "Heal"
                    : !string.IsNullOrEmpty(skill.displayName) ? skill.displayName : "Skill";
                int mpCost = _ctrl.EffectiveMpCost(skill);
                string label = mpCost > 0 ? $"{name} ({mpCost} MP)" : name;

                GUI.enabled = actor.CurrentMp >= mpCost;
                if (GUI.Button(new Rect(panel.x + 4, y, panelW - 8, btnH), label, _smallBtn))
                {
                    _ctrl.ChooseSkill(skill);
                    _showSkillList = false;
                }
                GUI.enabled = true;
                y += btnH + 4f;
            }
        }

        void DrawBenchMenu(int w)
        {
            var actor = _ctrl.PendingActor;
            if (actor == null) return;

            var bench = _ctrl.BenchOptions;
            const float panelW = 360f, btnH = 36f;
            float panelH = 60f + bench.Count * (btnH + 8f) + 44f;
            var panel = new Rect(w / 2f - panelW / 2f, 150, panelW, panelH);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 10, panel.y + 6, panel.width - 20, 24),
                $"Sub in for {actor.Definition.displayName}", _prompt);

            float y = panel.y + 40;
            foreach (var benched in bench)
            {
                string label = $"{benched.Definition.displayName}  ({benched.CurrentHp}/{benched.Stats.hp} HP)";
                if (GUI.Button(new Rect(panel.x + 20, y, panel.width - 40, btnH), label, _btn))
                    _ctrl.ChooseSub(benched);
                y += btnH + 8f;
            }
            if (GUI.Button(new Rect(panel.x + 20, y + 8f, panel.width - 40, btnH), "Back", _smallBtn))
                _ctrl.CancelBenchMenu();
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
            bool advancing = _ctrl.Outcome == BattleOutcome.PlayerVictory && _ctrl.World.HasNextMap;
            string label = _ctrl.Outcome == BattleOutcome.PlayerVictory ? "VICTORY" : "DEFEAT";
            GUI.Label(new Rect(0, h / 2f - 60, w, 60), label, _big);

            if (advancing)
            {
                GUI.Label(new Rect(0, h / 2f, w, 30), "The party presses onward, wounds and all", _sub);
                if (GUI.Button(new Rect(w / 2f - 100, h / 2f + 36, 200, 44), "Next Battle", _btn)) _ctrl.AdvanceToNextMap();
            }
            else
            {
                GUI.Label(new Rect(0, h / 2f, w, 30), "Press R or tap below to fight again", _sub);
                if (GUI.Button(new Rect(w / 2f - 80, h / 2f + 36, 160, 44), "Restart", _btn)) _ctrl.Restart();
            }
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
            const float panelW = 400f, panelH = 520f;
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
            y += 34;

            GUI.Label(new Rect(x, y, cw, 20), "Dev tuning -- speeds through battles, not real balance", _body);
            y += 22;
            GUI.Label(new Rect(x, y, cw, 20), $"Damage dealt -- {settings.DamageDealtMultiplier:0.0}x", _body);
            y += 22;
            settings.DamageDealtMultiplier = GUI.HorizontalSlider(new Rect(x, y, cw, 20), settings.DamageDealtMultiplier, 0.25f, 5f);
            y += 30;
            GUI.Label(new Rect(x, y, cw, 20), $"Damage received -- {settings.DamageReceivedMultiplier:0.0}x", _body);
            y += 22;
            settings.DamageReceivedMultiplier = GUI.HorizontalSlider(new Rect(x, y, cw, 20), settings.DamageReceivedMultiplier, 0f, 2f);
            y += 30;
            GUI.Label(new Rect(x, y, cw, 20), $"MP usage -- {settings.MpCostMultiplier:0.0}x (0x = free Skill Moves)", _body);
            y += 22;
            settings.MpCostMultiplier = GUI.HorizontalSlider(new Rect(x, y, cw, 20), settings.MpCostMultiplier, 0f, 2f);
            y += 34;

            if (GUI.Button(new Rect(x, y, cw, 36), "Save & Back", _btn))
            {
                settings.Save();
                _showSettings = false;
            }
        }
    }
}
