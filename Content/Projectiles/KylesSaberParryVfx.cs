using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace LiminalHuntersMod.Content.Projectiles
{
    /// <summary>
    /// Requirement 4:
    /// - Spritesheet size: 800x100
    /// - 8 frames of 100x100
    /// - Total duration: 0.6 seconds (36 game ticks at 60 FPS)
    /// - Each frame lasts 36 / 8 = 4.5 ticks (alternates 4 and 5 ticks)
    /// </summary>
    public class KylesSaberParryVfx : ModProjectile
    {
        public const int TotalFrames = 8;
        public const int FrameWidth = 100;
        public const int FrameHeight = 100;
        public const int DurationTicks = 36; // 0.6 seconds at 60 FPS

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = TotalFrames;
        }

        public override void SetDefaults()
        {
            Projectile.width = FrameWidth;
            Projectile.height = FrameHeight;
            Projectile.aiStyle = -1; // Custom frame logic
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = DurationTicks; // Exactly 0.6s
        }

        public override void AI()
        {
            // Lock to player center
            Player owner = Main.player[Projectile.owner];
            if (owner.active && !owner.dead)
            {
                Projectile.Center = owner.Center;
            }

            // Progress frames across 36 ticks
            // Tick 0 -> frame 0, Tick 35 -> frame 7
            float elapsed = DurationTicks - Projectile.timeLeft;
            Projectile.frame = (int)MathHelper.Clamp((elapsed * TotalFrames) / DurationTicks, 0, TotalFrames - 1);

            // Add dynamic kinetic lighting
            float progress = (float)elapsed / DurationTicks;
            float intensity = (1f - progress) * 1.2f;
            Lighting.AddLight(Projectile.Center, 0.4f * intensity, 0.8f * intensity, 1.0f * intensity);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;

            // Spritesheet is 800x100 horizontal: frame X = frame * 100
            Rectangle sourceRectangle = new Rectangle(
                Projectile.frame * FrameWidth,
                0,
                FrameWidth,
                FrameHeight
            );

            Vector2 origin = sourceRectangle.Size() / 2f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // Additive blending for energetic shockwave bloom
            Main.EntitySpriteDraw(
                texture,
                drawPos,
                sourceRectangle,
                Color.White * 0.95f,
                Projectile.rotation,
                origin,
                Projectile.scale * 1.25f,
                SpriteEffects.None,
                0
            );

            return false;
        }
    }
}
