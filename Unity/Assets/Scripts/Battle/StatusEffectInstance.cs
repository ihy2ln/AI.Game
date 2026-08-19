using Game.Data;

namespace Game.Battle
{
    /// <summary>One active buff/debuff/DoT/HoT/stun on a unit. Standard JRPG shape: ticks
    /// and counts down once per the AFFECTED unit's own turn (not globally per round) --
    /// see BattleUnit.TickStatusEffects, called from BattleController.RunBattle -- and is
    /// removed once RemainingTurns reaches 0.</summary>
    public class StatusEffectInstance
    {
        public StatusEffectType Type;

        /// <summary>Flat HP per tick for Poison/Regen; fractional multiplier (0.2 = 20%)
        /// for AttackUp/AttackDown/DefenseUp/DefenseDown; unused for Stun.</summary>
        public float Magnitude;

        public int RemainingTurns;
    }
}
