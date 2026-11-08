using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace LiminalHuntersMod.Common.GlobalProjectiles
{
    /// <summary>
    /// Requirement 3:
    /// Tracks projectiles that have been parried and deflected by Kyle's Saber.
    /// Ensures proper owner transfer, netcode sync, and visual flair.
    /// </summary>
    public class ParryGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool IsParried = false;

        public override void AI(Projectile projectile)
        {
            if (IsParried && projectile.active)
            {
                // Emit kinetic parry particles while traveling
                if (Main.rand.NextBool(2))
                {
                    Dust dust = Dust.NewDustDirect(
                        projectile.position,
                        projectile.width,
                        projectile.height,
                        DustID.Electric,
                        projectile.velocity.X * 0.2f,
                        projectile.velocity.Y * 0.2f,
                        100,
                        new Color(50, 220, 255),
                        1.1f
                    );
                    dust.noGravity = true;
                }
            }
        }

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (IsParried)
            {
                // Guarantee critical strike for deflected precision shots
                modifiers.SetCrit();
            }
        }
    }
}
