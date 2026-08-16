using UnityEngine;

namespace Game.Battle
{
    /// <summary>Plain C# battle settings, persisted via PlayerPrefs (first use in the
    /// project -- a handful of scalars doesn't need a JSON file). Loaded once by
    /// BattleBootstrap and shared by BattleController (speed/pause/auto-default) and
    /// BattleHud (the settings panel that edits it).</summary>
    public class BattleSettings
    {
        const string KeySpeed = "battle.speedMultiplier";
        const string KeyAutoDefault = "battle.autoModeDefault";
        const string KeyShowDamageNumbers = "battle.showDamageNumbers";
        const string KeyLogOpenDefault = "battle.logOpenByDefault";
        const string KeyMasterVolume = "battle.masterVolume";

        public static readonly float[] SpeedOptions = { 0.5f, 1f, 2f, 4f };

        public float SpeedMultiplier = 1f;
        public bool AutoModeDefault = true;
        public bool ShowDamageNumbers = true;
        public bool LogOpenByDefault;
        public float MasterVolume = 1f;

        public static BattleSettings Load() => new()
        {
            SpeedMultiplier = PlayerPrefs.GetFloat(KeySpeed, 1f),
            AutoModeDefault = PlayerPrefs.GetInt(KeyAutoDefault, 1) != 0,
            ShowDamageNumbers = PlayerPrefs.GetInt(KeyShowDamageNumbers, 1) != 0,
            LogOpenByDefault = PlayerPrefs.GetInt(KeyLogOpenDefault, 0) != 0,
            MasterVolume = PlayerPrefs.GetFloat(KeyMasterVolume, 1f),
        };

        public void Save()
        {
            PlayerPrefs.SetFloat(KeySpeed, SpeedMultiplier);
            PlayerPrefs.SetInt(KeyAutoDefault, AutoModeDefault ? 1 : 0);
            PlayerPrefs.SetInt(KeyShowDamageNumbers, ShowDamageNumbers ? 1 : 0);
            PlayerPrefs.SetInt(KeyLogOpenDefault, LogOpenByDefault ? 1 : 0);
            PlayerPrefs.SetFloat(KeyMasterVolume, MasterVolume);
            PlayerPrefs.Save();
            AudioListener.volume = MasterVolume;
        }
    }
}
