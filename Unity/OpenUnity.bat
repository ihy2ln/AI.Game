@echo off
set UNITY="S:\AI\Game Engine\Unity\UnityEditors\Editor\6000.5.7f1\Editor\Unity.exe"
set PROJECT=S:\AI\Game\AI.Game\Unity
set LOG=S:\AI\Game\AI.Game\logs\unity-open.log
echo Opening AI.Game Unity project...
start "" %UNITY% -projectPath "%PROJECT%"
