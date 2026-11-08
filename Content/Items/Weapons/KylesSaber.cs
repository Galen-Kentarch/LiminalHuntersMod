using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using LiminalHuntersMod.Common.Players;
using LiminalHuntersMod.Content.DamageClasses;
using LiminalHuntersMod.Content.Dusts;
using LiminalHuntersMod.Content.Projectiles;

namespace LiminalHuntersMod.Content.Items.Weapons
{
    /// <summary>
    /// Kyle's Saber:
    /// - 120 Cinetic Damage
    /// - 2 swings per second (useTime = 30, useAnimation = 30 at 60 TPS)
    /// - 69x69 sprite size
    /// - Blade-only glow trail particle (opacity fades in 0.2s = 12 ticks)
    /// - Right-click Parry: Reverse-grip 45° downward guard, deflects enemy damage (x1.5 + 120) and projectiles (x1.8 back to cursor).
    /// </summary>
    public class KylesSaber : ModItem
    {
        // 69x69 sprite dimensions
        public const int SpriteWidth = 69;
        public const int SpriteHeight = 69;

        // Parry balance constants
        public const int SaberBaseDamage = 120;
        public const float ParryRadius = 135f; // Boundary radius around the player
        public const int ParryCooldownTicks = 180; // 3 seconds at 60 TPS

        public override void SetStaticDefaults()
        {
            // Tooltip handled in localization or SetDefaults
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults()
        {
            Item.width = SpriteWidth;
            Item.height = SpriteHeight;

            // Requirement 1 & 2: Cinetic Damage & 120 Damage
            Item.DamageType = ModContent.GetInstance<CineticDamageClass>();
            Item.damage = SaberBaseDamage;
            Item.knockBack = 5.5f;
            Item.crit = 8; // 8% base crit

            // Requirement 2: Fast enough to swing twice in 1 second.
            // Terraria runs at 60 updates per second. 60 / 2 = 30 ticks per full swing.
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;

            Item.autoReuse = true;
            Item.UseSound = SoundID.Item15; // Energy swing hum
            Item.rare = ItemRarityID.Lime;
            Item.value = Item.sellPrice(gold: 15);
            Item.scale = 1.15f;
        }

        // Requirement 3: Allow Right-Click for Parry
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            var modPlayer = player.GetModPlayer<KylesSaberPlayer>();

            // If right-clicking (Parry attempt)
            if (player.altFunctionUse == 2)
            {
                // Can only parry if cooldown is ready
                if (modPlayer.ParryCooldownTimer > 0)
                {
                    return false;
                }

                // Setup parry state
                Item.useStyle = ItemUseStyleID.HoldUp; // Custom drawn in PlayerDrawLayer
                Item.useTime = 20;
                Item.useAnimation = 20;
                Item.noMelee = true; // Parry doesn't trigger standard swing hitboxes
                Item.UseSound = null; // Parry plays its own clash sound on trigger
            }
            else
            {
                // Normal primary swing
                Item.useStyle = ItemUseStyleID.Swing;
                Item.useTime = 30; // 2 swings per second
                Item.useAnimation = 30;
                Item.noMelee = false;
                Item.UseSound = SoundID.Item15;
            }

            return base.CanUseItem(player);
        }

        public override bool? UseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                var modPlayer = player.GetModPlayer<KylesSaberPlayer>();
                // Activate the parry guard and scan for enemies/projectiles
                modPlayer.ExecuteParry(ParryRadius, SaberBaseDamage);
                return true;
            }

            return base.UseItem(player);
        }

        /// <summary>
        /// Requirement 2: Trail behind ONLY the blade, NOT the handle.
        /// Spawns a custom particle / afterimage dust using KylesSaber_Glow.png (69x69 mask)
        /// which fades from 100% to 0% opacity in 0.2 seconds (12 game ticks).
        /// </summary>
        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            if (Main.netMode == NetmodeID.Server)
                return;

            // Calculate current weapon swing angle & tip position
            float itemRotation = player.itemRotation;
            Vector2 handPosition = player.GetFrontHandPosition(player.compositeFrontArm.stretch, itemRotation);

            // Spawn blade-only glow particle
            // The particle stays at current position & angle and fades out over 0.2s (12 frames)
            KylesSaberBladeGlowTrail.SpawnBladeGlow(
                position: handPosition,
                rotation: itemRotation,
                direction: player.direction,
                scale: Item.scale,
                lightColor: new Color(40, 230, 255) // Cyan lightsaber glow
            );

            // Add ambient light along the blade
            Lighting.AddLight(hitbox.Center.ToVector2(), 0.3f, 0.8f, 1.0f);
        }
    }
}
