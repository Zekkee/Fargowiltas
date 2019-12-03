using Terraria.ID;
using Terraria.ModLoader;

namespace Fargowiltas.Items.Tiles
{
    public class Omnistation : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Omnistation");
            Tooltip.SetDefault("Effects of all stations");
        }

        public override void SetDefaults()
        {
            item.width = 20;
            item.height = 20;
            item.useTurn = true;
            item.autoReuse = true;
            item.useAnimation = 15;
            item.useTime = 10;
            item.useStyle = 1;
            item.consumable = false;
            item.rare = 1;
            item.createTile = mod.TileType("OmnistationSheet");
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Sunflower, 30);
            recipe.AddIngredient(ItemID.Campfire, 30);
            recipe.AddIngredient(ItemID.HeartLantern, 30);
            recipe.AddIngredient(ItemID.StarinaBottle, 30);
            recipe.AddIngredient(ItemID.HoneyBucket, 30);
            recipe.AddIngredient(ItemID.SharpeningStation, 5);
            recipe.AddIngredient(ItemID.AmmoBox, 5);
            recipe.AddIngredient(ItemID.CrystalBall, 5);
            recipe.AddIngredient(ItemID.BewitchingTable, 5);
            recipe.AddIngredient(ItemID.ChlorophyteBar, 5);
            recipe.AddTile(TileID.MythrilAnvil);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}