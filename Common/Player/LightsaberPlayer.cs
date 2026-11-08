using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace LiminalHuntersMod.Common.Players
{
    public class LightsaberPlayer : ModPlayer
    {
        // Parry frame timer tracking (60 ticks = 1 second)
        public int ParryTimer = 0;
        public int ParryCooldown = 0;
        public const int DefaultParryWindow = 16; // Active parry window in frames
        public const int DefaultParryCooldown = 45; // Cooldown ticks before next parry
        
        public bool IsParrying => ParryTimer > 0;
        public bool IsInActiveParryWindow => ParryTimer > 0 && ParryTimer <= DefaultParryWindow;

        public override void ResetEffects()
        {
            if (ParryTimer > 0)
                ParryTimer--;

            if (ParryCooldown > 0)
                ParryCooldown--;
        }

        public void TriggerParry()
        {
            if (ParryCooldown <= 0 && ParryTimer <= 0)
            {
                ParryTimer = DefaultParryWindow + 4; // 4 startup frames + active frames
                ParryCooldown = DefaultParryCooldown;
                
                // Play ignition / defensive stance sound
                SoundEngine.PlaySound(SoundID.Item15 with { Pitch = 0.2f, Volume = 0.7f }, Player.Center);
                
                // Spawn circular ignition dust wave
                for (int i = 0; i < 24; i++)
                {
                    Vector2 speed = Main.rand.NextVector2CircularEdge(5f, 5f);
                    Dust d = Dust.NewDustPerfect(Player.MountedCenter, DustID.Electric, speed, Scale: 1.2f);
                    d.noGravity = true;
                }
            }
        }

        /// <summary>
        /// In tModLoader 1.4.4+, FreeDodge is the standard hook to completely negate incoming damage
        /// </summary>
        public override bool FreeDodge(Player.HurtInfo info)
        {
            if (IsInActiveParryWindow)
            {
                OnSuccessfulParry(info.DamageSource);
                return true; // Returning true completely negates the incoming hit!
            }
            return base.FreeDodge(info);
        }

        /// <summary>
        /// Ensures incoming attacks during the parry window respect dodge mechanics
        /// </summary>
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            /*
            if (IsInActiveParryWindow)
            {
                modifiers.Dodgeable = true;
            }
            */
        }

        /// <summary>
        /// Intercepts and negates direct contact damage from hostile NPCs during active parry
        /// </summary>
        public override bool CanBeHitByNPC(NPC npc, ref int cooldownSlot)
        {
            if (IsInActiveParryWindow)
            {
                OnSuccessfulParry(PlayerDeathReason.ByNPC(npc.whoAmI));
                return false; // Prevent contact hit from occurring
            }
            return base.CanBeHitByNPC(npc, ref cooldownSlot);
        }

        /// <summary>
        /// Intercepts and negates damage from hostile projectiles during active parry
        /// </summary>
        public override bool CanBeHitByProjectile(Projectile proj)
        {
            if (IsInActiveParryWindow)
            {
                OnSuccessfulParry(PlayerDeathReason.ByProjectile(proj.owner, proj.whoAmI));
                return false; // Prevent projectile hit from occurring
            }
            return base.CanBeHitByProjectile(proj);
        }

        /// <summary>
        /// Triggered when the player successfully parries an attack
        /// </summary>
        public void OnSuccessfulParry(PlayerDeathReason damageSource)
        {
            // Grant invulnerability frames
            Player.immune = true;
            Player.immuneTime = 40;

            // Satisfying resonant parry clash sound
            SoundEngine.PlaySound(SoundID.Item37 with { Pitch = 0.4f, Volume = 1.2f }, Player.Center);
            SoundEngine.PlaySound(SoundID.Item93 with { Pitch = 0.1f, Volume = 0.9f }, Player.Center);

            // Visual freeze / hitstop floating text feedback
            if (Main.netMode != NetmodeID.Server)
            {
                CombatText.NewText(Player.getRect(), Color.Gold, "PARRY!", dramatic: true);
            }

            // Radial spark shockwave
            for (int i = 0; i < 40; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(9f, 9f);
                Dust d = Dust.NewDustPerfect(Player.MountedCenter, DustID.Torch, speed, Scale: 1.8f);
                d.noGravity = true;
                d.color = Color.Cyan;
            }

            // Repel, stun, and strike nearby contact enemies
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && !npc.friendly && Vector2.Distance(Player.Center, npc.Center) < 140f)
                {
                    Vector2 knockDirection = (npc.Center - Player.Center).SafeNormalize(Vector2.UnitX);
                    npc.velocity = knockDirection * 12f;
                    
                    // Stun and damage enemy with 2x weapon damage using 1.4.4 SimpleStrikeNPC
                    int weaponDamage = Player.GetWeaponDamage(Player.HeldItem);
                    npc.SimpleStrikeNPC(weaponDamage * 2, (int)knockDirection.X, crit: true, knockBack: 8f);
                }
            }

            // Deflect any active incoming enemy projectiles within radius
            DeflectNearbyProjectiles();
        }

        /// <summary>
        /// Deflects incoming hostile projectiles back at enemies with multiplied velocity and damage
        /// </summary>
        public void DeflectNearbyProjectiles()
        {
            float deflectRadius = 180f;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.hostile && Vector2.Distance(Player.Center, proj.Center) <= deflectRadius)
                {
                    // Deflect! Invert hostility
                    proj.hostile = false;
                    proj.friendly = true;
                    proj.owner = Player.whoAmI;

                    // Aim deflected projectile towards mouse cursor or reflect back
                    Vector2 mouseWorld = Main.MouseWorld;
                    Vector2 deflectDir = (mouseWorld - proj.Center).SafeNormalize(-proj.velocity);
                    proj.velocity = deflectDir * (proj.velocity.Length() * 1.6f + 4f);
                    
                    // Multiply damage by 2.5x for perfect deflection
                    proj.damage = (int)(proj.damage * 2.5f);
                    proj.penetrate = Math.Max(proj.penetrate, 3);

                    // Audio feedback
                    SoundEngine.PlaySound(SoundID.Item94 with { Pitch = 0.5f, Volume = 0.8f }, proj.Center);

                    CombatText.NewText(proj.getRect(), Color.Cyan, "DEFLECTED!", dramatic: true);
                }
            }
        }
    }
}