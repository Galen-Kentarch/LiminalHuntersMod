using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using LiminalHuntersMod.Common.GlobalProjectiles;
using LiminalHuntersMod.Content.Items.Weapons;
using LiminalHuntersMod.Content.Projectiles;

namespace LiminalHuntersMod.Common.Players
{
    /// <summary>
    /// Tracks the parry cooldown, active parry guard stance, and custom weapon drawing.
    /// </summary>
    public class KylesSaberPlayer : ModPlayer
    {
        // Requirement 3: 3 seconds cooldown before parry can be used again (180 ticks at 60 FPS)
        public int ParryCooldownTimer = 0;
        public const int MaxParryCooldown = 180;

        // Visual parry active duration (time player remains in parry stance)
        public int ParryStanceDuration = 0;
        public const int MaxParryStance = 24; // ~0.4s stance hold

        public override void ResetEffects()
        {
            if (ParryCooldownTimer > 0)
            {
                ParryCooldownTimer--;
                if (ParryCooldownTimer == 0 && Player.whoAmI == Main.myPlayer)
                {
                    // Optional: subtle audio/visual cue when parry is ready
                    SoundEngine.PlaySound(SoundID.MaxMana with { Volume = 0.5f, Pitch = 0.4f }, Player.Center);
                }
            }

            if (ParryStanceDuration > 0)
            {
                ParryStanceDuration--;
            }
        }

        /// <summary>
        /// Requirement 3: Executes Parry within the boundary around the player.
        /// Everything is handled independently:
        /// - Enemy: takes 1.5 * enemy damage + saber damage (120)
        /// - Projectile: hostile -> friendly, aimed at cursor, damage * 1.8
        /// </summary>
        public void ExecuteParry(float radius, int saberDamage)
        {
            // Enter parry stance & set 3 second cooldown
            ParryCooldownTimer = MaxParryCooldown;
            ParryStanceDuration = MaxParryStance;

            bool parriedAny = false;
            Vector2 playerCenter = Player.Center;
            Vector2 mouseWorld = Main.MouseWorld;

            // 1. Check all Enemies (NPCs) within parry radius
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC target = Main.npc[i];
                if (target.active && !target.friendly && !target.dontTakeDamage && target.lifeMax > 5)
                {
                    float distance = Vector2.Distance(playerCenter, target.Center);
                    if (distance <= radius + (target.width / 2f))
                    {
                        // Requirement 3: enemy's damage returned with x1.5 + saber damage (120)
                        int baseEnemyDmg = target.damage > 0 ? target.damage : 40;
                        int reflectedDamage = (int)(baseEnemyDmg * 1.5f) + saberDamage;

                        // Calculate knockback away from player
                        Vector2 knockbackDir = (target.Center - playerCenter).SafeNormalize(Vector2.UnitX);
                        float knockbackForce = 10f;

                        // Deal damage
                        Player.ApplyDamageToNPC(
                            target,
                            reflectedDamage,
                            knockbackForce,
                            target.Center.X > playerCenter.X ? 1 : -1,
                            crit: true
                        );

                        // Visual feedback
                        target.netUpdate = true;
                        parriedAny = true;
                    }
                }
            }

            // 2. Check all Projectiles within parry radius
            for (int j = 0; j < Main.maxProjectiles; j++)
            {
                Projectile proj = Main.projectile[j];
                if (proj.active && proj.hostile && !proj.friendly)
                {
                    float distance = Vector2.Distance(playerCenter, proj.Center);
                    if (distance <= radius + (proj.width / 2f))
                    {
                        // Requirement 3: becomes player's projectile, sent towards cursor with x1.8 damage
                        proj.hostile = false;
                        proj.friendly = true;
                        proj.owner = Player.whoAmI;

                        // Multiply damage by 1.8
                        proj.damage = Math.Max(1, (int)(proj.damage * 1.8f));

                        // Aim towards the last location of the player's cursor
                        float currentSpeed = proj.velocity.Length();
                        if (currentSpeed < 10f) currentSpeed = 14f; // Ensure responsive bounce speed

                        Vector2 aimDirection = (mouseWorld - proj.Center).SafeNormalize(Vector2.UnitX);
                        proj.velocity = aimDirection * currentSpeed;

                        // Mark as parried so it pierces or draws a trail
                        proj.GetGlobalProjectile<ParryGlobalProjectile>().IsParried = true;
                        proj.netUpdate = true;
                        parriedAny = true;
                    }
                }
            }

            // Requirement 4: Always spawn Parry effect & sound when parry is triggered!
            // Total duration: 0.6 seconds (36 ticks), 8 frames of 100x100.
            SpawnParryVfx(playerCenter);

            // Requirement 4: Custom sound effect. Default fallback SoundID.Item150, easily replaced.
            PlayParrySound(playerCenter, parriedAny);
        }

        private void SpawnParryVfx(Vector2 position)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                // Spawns the 800x100 (8 frames of 100x100) spritesheet effect
                Projectile.NewProjectile(
                    Player.GetSource_ItemUse(Player.HeldItem),
                    position,
                    Vector2.Zero,
                    ModContent.ProjectileType<KylesSaberParryVfx>(),
                    0,
                    0f,
                    Player.whoAmI
                );
            }
        }

        private void PlayParrySound(Vector2 position, bool success)
        {
            // You can replace this path with your own sound in Assets/Sounds/ParrySound.ogg
            // Example: SoundEngine.PlaySound(new SoundStyle("LiminalHuntersMod/Assets/Sounds/ParrySound"), position);
            // Default placeholder: high-energy shield clash + crystal chime
            SoundEngine.PlaySound(SoundID.Item150 with { Pitch = success ? 0.3f : -0.1f, Volume = 1.0f }, position);
            SoundEngine.PlaySound(SoundID.Item93 with { Pitch = 0.5f, Volume = 0.6f }, position);
        }

        /// <summary>
        /// Requirement 5: When parrying, the player holds the saber at a 45 degree angle,
        /// blade pointing downwards, holding from the handle.
        /// 69x69 sprite: handle at bottom-left, blade at top-right.
        /// We configure composite arm angle and draw layer rotation here.
        /// </summary>
        public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
        {
            if (ParryStanceDuration > 0 && Player.HeldItem.type == ModContent.ItemType<KylesSaber>())
            {
                // Force arm into parry forward-defensive angle
                float armAngle = MathHelper.ToRadians(Player.direction == 1 ? 40f : -40f);
                Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Quarter, armAngle);
            }
        }
    }
}
