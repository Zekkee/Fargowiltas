﻿using Terraria.ID;
using Terraria.ModLoader;

namespace Fargowiltas.Items.Vanity
{
    [AutoloadEquip(EquipType.Legs)]
    public class DevianttPants : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Deviantt Pants");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.vanity = true;
            Item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.SoulofNight, 5)
                .AddIngredient(ItemID.SoulofFlight, 5)
                .AddTile(TileID.TinkerersWorkbench)
                .Register();
        }
    }
}