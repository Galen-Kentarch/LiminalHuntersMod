using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace LiminalHuntersMod.Content.Dusts
{
    /// <summary>
    /// Requirement 2:
    /// "leave a trail behind only of the blade, not the handle.
    /// Sprite 69x69 with only the glow of the saber as a particle.
    /// Opacity 100 to 0 in 0.2 seconds (12 ticks). Once 0 opacity it gets destroyed."
    /// </summary>
    public static class KylesSaberBladeGlowTrail
    {
        public class TrailParticle
        {
            public Vector2 Position;
            public float Rotation;
            public int Direction;
            public float Scale;
            public Color Tint;
            public int LifetimeTicks;
            public const int MaxLifetime = 12; // 0.2 seconds at 60 FPS
        }

        private static readonly List<TrailParticle> ActiveParticles = new();

        public static void SpawnBladeGlow(Vector2 position, float rotation, int direction, float scale, Color lightColor)
        {
            ActiveParticles.Add(new TrailParticle
            {
                Position = position,
                Rotation = rotation,
                Direction = direction,
                Scale = scale,
                Tint = lightColor,
                LifetimeTicks = 0
            });
        }

        public static void UpdateAndDrawParticles(SpriteBatch spriteBatch)
        {
            if (ActiveParticles.Count == 0) return;

            Texture2D glowTexture = ModContent.Request<Texture2D>("LiminalHuntersMod/Content/Items/Weapons/KylesSaber_Glow").Value;
            if (glowTexture == null) return;

            // Handle is at bottom-left: origin = (12f, 57f)
            Vector2 origin = new Vector2(12f, 57f);

            for (int i = ActiveParticles.Count - 1; i >= 0; i--)
            {
                var p = ActiveParticles[i];
                p.LifetimeTicks++;

                // 0.2 seconds = 12 ticks
                if (p.LifetimeTicks >= TrailParticle.MaxLifetime)
                {
                    ActiveParticles.RemoveAt(i);
                    continue;
                }

                // Opacity fades linearly from 1.0 (100%) to 0.0 (0%)
                float progress = (float)p.LifetimeTicks / TrailParticle.MaxLifetime;
                float alpha = MathHelper.Clamp(1f - progress, 0f, 1f);

                Color drawColor = p.Tint * alpha;
                SpriteEffects effects = p.Direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                Vector2 adjustedOrigin = p.Direction == -1 ? new Vector2(glowTexture.Width - origin.X, origin.Y) : origin;

                Vector2 screenPos = p.Position - Main.screenPosition;

                spriteBatch.Draw(
                    glowTexture,
                    screenPos,
                    null,
                    drawColor,
                    p.Rotation,
                    adjustedOrigin,
                    p.Scale,
                    effects,
                    0f
                );
            }
        }
    }
}
