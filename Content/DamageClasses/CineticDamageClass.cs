using Terraria;
using Terraria.ModLoader;

namespace LiminalHuntersMod.Content.DamageClasses
{
    /// <summary>
    /// Kyle's custom damage class: Cinetic Damage.
    /// Scales independently from vanilla melee/ranged/magic/summoner,
    /// but can optionally inherit stats or benefit from universal damage boosts.
    /// </summary>
    public class CineticDamageClass : DamageClass
    {
        // Internal canonical identifier
        public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
        {
            // By default, Generic (Universal) damage bonuses (e.g. Celestial Stone) apply to Cinetic damage.
            if (damageClass == Generic)
                return StatInheritanceData.Full;

            // Does not inherit from Melee, Ranged, Magic, or Summon bonuses unless explicitly desired.
            return StatInheritanceData.None;
        }

        public override bool GetEffectInheritance(DamageClass damageClass)
        {
            // If you want Melee-specific on-hit effects (like Magma Stone fire) to trigger, return true for Melee.
            if (damageClass == Melee)
                return false;

            return false;
        }

        public override void SetDefaultStats(Player player)
        {
            // Baseline stats (100% damage, 4% base crit, normal knockback)
            // Can be modified by Cinetic accessories/armor sets
        }

        public override bool ShowStatTooltipLine(Player player, string lineName)
        {
            // Ensures tooltip shows "[damage] cinetic damage" and "[crit]% cinetic critical strike chance"
            return true;
        }
    }
}
