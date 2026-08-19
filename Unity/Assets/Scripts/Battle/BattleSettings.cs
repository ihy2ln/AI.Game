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
        const string KeyDamageDealtMult = "battle.damageDealtMultiplier";
        const string KeyDamageReceivedMult = "battle.damageReceivedMultiplier";
        const string KeyMpCostMult = "battle.mpCostMultiplier";

        public static readonly float[] SpeedOptions = { 0.5f, 1f, 2f, 4f };

        public float SpeedMultiplier = 1f;
        public bool AutoModeDefault = true;
        public bool ShowDamageNumbers = true;
        public bool LogOpenByDefault;
        public float MasterVolume = 1f;

        /// <summary>Dev-convenience multipliers for speeding through battles while the
        /// game is being built -- applied to damage the player deals / receives
        /// respectively (BattleController.ResolveAction). 1x is the real, untuned rate.</summary>
        public float DamageDealtMultiplier = 1f;
        public float DamageReceivedMultiplier = 1f;

        /// <summary>Multiplies every skill's mpCost when it's spent -- 0x makes Skill
        /// Move actions free so they can be tested repeatedly without waiting on regen
        /// (there isn't any yet). 1x is the real, untuned rate.</summary>
        public float MpCostMultiplier = 1f;

        public static BattleSettings Load() => new()
        {
            SpeedMultiplier = PlayerPrefs.GetFloat(KeySpeed, 1f),
            AutoModeDefault = PlayerPrefs.GetInt(KeyAutoDefault, 1) != 0,
            ShowDamageNumbers = PlayerPrefs.GetInt(KeyShowDamageNumbers, 1) != 0,
            LogOpenByDefault = PlayerPrefs.GetInt(KeyLogOpenDefault, 0) != 0,
            MasterVolume = PlayerPrefs.GetFloat(KeyMasterVolume, 1f),
            DamageDealtMultiplier = PlayerPrefs.GetFloat(KeyDamageDealtMult, 1f),
            DamageReceivedMultiplier = PlayerPrefs.GetFloat(KeyDamageReceivedMult, 1f),
            MpCostMultiplier = PlayerPrefs.GetFloat(KeyMpCostMult, 1f),
        };

        public void Save()
        {
            PlayerPrefs.SetFloat(KeySpeed, SpeedMultiplier);
            PlayerPrefs.SetInt(KeyAutoDefault, AutoModeDefault ? 1 : 0);
            PlayerPrefs.SetInt(KeyShowDamageNumbers, ShowDamageNumbers ? 1 : 0);
            PlayerPrefs.SetInt(KeyLogOpenDefault, LogOpenByDefault ? 1 : 0);
            PlayerPrefs.SetFloat(KeyMasterVolume, MasterVolume);
            PlayerPrefs.SetFloat(KeyDamageDealtMult, DamageDealtMultiplier);
            PlayerPrefs.SetFloat(KeyDamageReceivedMult, DamageReceivedMultiplier);
            PlayerPrefs.SetFloat(KeyMpCostMult, MpCostMultiplier);
            PlayerPrefs.Save();
            AudioListener.volume = MasterVolume;
        }
    }
}
