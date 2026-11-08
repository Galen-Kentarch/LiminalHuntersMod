using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;
using LiminalHuntersMod.Content.Items.Weapons;

namespace LiminalHuntersMod.Common.Players
{
    /// <summary>
    /// Requirement 5:
    /// "When parrying with the 'Kyle Saber', the player holds it on its arm at a 45 degree angle,
    /// and the sword has its blade pointing downwards. The player should still be holding the sword
    /// from the handle. The sprite is 69x69, and the handle is located at the bottom left of the sprite,
    /// while the blade is at the top right of the sprite. Adjust the pivot point to account for this diagonal orientation."
    /// </summary>
    public class KylesSaberParryDrawLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.ArmOverItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            if (player.dead || player.invis)
                return false;

            var modPlayer = player.GetModPlayer<KylesSaberPlayer>();
            return modPlayer.ParryStanceDuration > 0 && player.HeldItem.type == ModContent.ItemType<KylesSaber>();
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            Texture2D texture = TextureAssets.Item[ModContent.ItemType<KylesSaber>()].Value;
            if (texture == null) return;

            // Geometry Analysis:
            // Sprite is 69x69. Handle is at bottom-left (~ x: 12, y: 57).
            // Blade extends diagonally to top-right (~ x: 57, y: 12).
            // By default (unrotated), the vector from handle to tip points up-right at +45° from horizontal.
            //
            // Goal: Hold at 45° angle with blade pointing DOWNWARDS.
            // Facing Right (direction = 1):
            // - Blade pointing down-forward at 45° below horizontal:
            //   Target angle in screen coordinates = +45° (or +135° relative to base sprite orientation).
            // - Handle pivot point: (12f, 57f).
            //
            // Facing Left (direction = -1):
            // - Horizontal flip mirrors texture: handle is at bottom-right (69 - 12 = 57f, 57f).
            // - Rotation is mirrored accordingly.

            Vector2 handlePivot = new Vector2(12f, 57f);
            float rotation;
            SpriteEffects effects;
            Vector2 origin;

            if (player.direction == 1)
            {
                // Facing Right
                // Base sprite angle is -45° (pointing up-right). Rotating by +90° turns it to point down-right (+45° downwards).
                rotation = MathHelper.ToRadians(90f);
                effects = SpriteEffects.None;
                origin = handlePivot;
            }
            else
            {
                // Facing Left
                // Flip horizontally so blade points down-left
                rotation = MathHelper.ToRadians(-90f);
                effects = SpriteEffects.FlipHorizontally;
                origin = new Vector2(texture.Width - handlePivot.X, handlePivot.Y);
            }

            // Hand grip position attached to player's front arm
            Vector2 handPos = player.GetFrontHandPosition(player.compositeFrontArm.stretch, player.bodyRotation);
            Vector2 drawPosition = handPos - Main.screenPosition;

            DrawData drawData = new DrawData(
                texture,
                drawPosition,
                null,
                drawInfo.colorArmorBody,
                rotation,
                origin,
                player.HeldItem.scale,
                effects,
                0
            );

            drawInfo.DrawDataCache.Add(drawData);
        }
    }
}
