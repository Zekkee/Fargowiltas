using System;
using System.Collections.Generic;
using Fargowiltas.Items.Vanity;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace Fargowiltas.NPCs
{
    public class FargoGlobalNPC : GlobalNPC
    {
        internal static int[] Bosses = { NPCID.KingSlime, NPCID.EyeofCthulhu, NPCID.BrainofCthulhu, NPCID.QueenBee, NPCID.SkeletronHead, NPCID.TheDestroyer, NPCID.SkeletronPrime, NPCID.Retinazer, NPCID.Spazmatism, NPCID.Plantera, NPCID.Golem, NPCID.DukeFishron, NPCID.CultistBoss, NPCID.MoonLordCore, NPCID.MartianSaucerCore, NPCID.Pumpking, NPCID.IceQueen, NPCID.DD2Betsy, NPCID.DD2OgreT3, NPCID.IceGolem, NPCID.SandElemental, NPCID.Paladin, NPCID.Everscream, NPCID.MourningWood, NPCID.SantaNK1, NPCID.HeadlessHorseman, NPCID.PirateShip };

        public static int LastWoFIndex = -1;
        public static int WoFDirection = 0;

        internal bool PillarSpawn = true;
        internal bool SwarmActive;
        internal bool PandoraActive;
        internal bool NoLoot = false;
        //internal bool DestroyerSwarm = false;

        public static int eaterBoss = -1;
        public static int brainBoss = -1;
        public static int plantBoss = -1;

        public override bool InstancePerEntity => true;

        public override void SetDefaults(NPC npc)
        {
            if (GetInstance<FargoConfig>().CatchNPCs)
            {
                if (npc.townNPC && npc.type < NPCID.Count && npc.type != NPCID.OldMan)
                {
                    Main.npcCatchable[npc.type] = true;
                    npc.catchItem = npc.type == NPCID.DD2Bartender ? (short)mod.ItemType("Tavernkeep") : (short)mod.ItemType(NPCID.GetUniqueKey(npc.type).Replace("Terraria ", string.Empty));
                }

                if (npc.type == NPCID.SkeletonMerchant)
                {
                    Main.npcCatchable[npc.type] = true;
                    npc.catchItem = (short)mod.ItemType("SkeletonMerchant");
                }
            }
        }

        public override bool? CanHitNPC(NPC npc, NPC target)
        {
            if (target.dontTakeDamage && target.type == NPCType<Squirrel>())
                return false;
            return base.CanHitNPC(npc, target);
        }

        public override bool PreAI(NPC npc)
        {
            if (npc.boss)
            {
                boss = npc.whoAmI;
            }

            switch (npc.type)
            {
                case NPCID.EaterofWorldsHead:
                    eaterBoss = npc.whoAmI;
                    break;

                case NPCID.BrainofCthulhu:
                    brainBoss = npc.whoAmI;
                    break;

                case NPCID.Plantera:
                    plantBoss = npc.whoAmI;
                    break;

                case NPCID.TheDestroyer:
                    if (SwarmActive)
                    {
                        if (npc.ai[0] == 0)
                        {
                            if (Main.netMode == NetmodeID.MultiplayerClient)
                                return false;

                            for (int i = 0; i < Main.maxNPCs; i++) //purge segments i shouldn't have
                            {
                                if (Main.npc[i].active && (Main.npc[i].type == NPCID.TheDestroyerBody || Main.npc[i].type == NPCID.TheDestroyerTail) && Main.npc[i].realLife == npc.whoAmI)
                                {
                                    npc.life = 0;
                                    npc.HitEffect();
                                    npc.active = false;
                                    if (Main.netMode == NetmodeID.Server)
                                        NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
                                }
                            }

                            npc.lifeMax /= 4;
                            if (npc.life > npc.lifeMax)
                                npc.life = npc.lifeMax;
                            npc.ai[3] = npc.whoAmI;
                            npc.realLife = npc.whoAmI;
                            int prev = npc.whoAmI;
                            int bodySegments = 9;
                            for (int j = 0; j < bodySegments; j++)
                            {
                                int type = NPCID.TheDestroyerBody;
                                if (j == bodySegments - 1)
                                {
                                    type = NPCID.TheDestroyerTail;
                                }

                                int n = NPC.NewNPC((int)(npc.position.X + (float)(npc.width / 2)), (int)(npc.position.Y + npc.height), type, npc.whoAmI);
                                Main.npc[n].ai[3] = npc.whoAmI;
                                Main.npc[n].realLife = npc.whoAmI;
                                Main.npc[n].ai[1] = prev;
                                Main.npc[prev].ai[0] = n;
                                Main.npc[n].GetGlobalNPC<FargoGlobalNPC>().SwarmActive = true;
                                if (Main.netMode == NetmodeID.Server)
                                    NetMessage.SendData(MessageID.SyncNPC, number: n);
                                prev = n;
                            }
                            if (Main.netMode == NetmodeID.Server)
                            {
                                NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
                                var netMessage = mod.GetPacket();
                                netMessage.Write((byte)4);
                                netMessage.Write(npc.whoAmI);
                                netMessage.Write(npc.lifeMax);
                                netMessage.Send();
                            }
                            return false;
                        }
                        else if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int count = 0;
                            for (int i = 0; i < Main.maxNPCs; i++) //confirm i have exactly the right number of segments behind me
                            {
                                if (Main.npc[i].active && (Main.npc[i].type == NPCID.TheDestroyerBody || Main.npc[i].type == NPCID.TheDestroyerTail) && Main.npc[i].realLife == npc.whoAmI)
                                {
                                    count++;
                                    if (count > 9)
                                        break;
                                }
                            }

                            if (count != 9) //if not exactly the right pieces, die
                            {
                                npc.life = 0;
                                npc.HitEffect();
                                npc.active = false;
                                if (Main.netMode == NetmodeID.Server)
                                {
                                    //NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("head killed by wrong count, " + count.ToString()), Color.White);
                                    NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
                                }
                                else
                                {
                                    //Main.NewText("head killed by wrong count, " + count.ToString());
                                }
                            }
                        }
                    }
                    break;

                case NPCID.TheDestroyerBody:
                case NPCID.TheDestroyerTail:
                    if (SwarmActive)// && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        //kill if real life is invalid
                        if (!(npc.realLife > -1 && npc.realLife < Main.maxNPCs && Main.npc[npc.realLife].active && Main.npc[npc.realLife].type == NPCID.TheDestroyer))
                        {
                            //Main.NewText("body realLife invalid, die");
                            npc.life = 0;
                            npc.HitEffect();
                            npc.active = false;
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
                            return false;
                        }

                        int prev = npc.whoAmI;
                        int segment = (int)npc.ai[1];
                        int i = 0;
                        const int maxLength = 9;
                        for (; i < maxLength; i++) //iterate upwards along destroyer's body
                        {
                            if (segment > -1 && segment < Main.maxNPCs && Main.npc[segment].active && Main.npc[segment].type == NPCID.TheDestroyerBody
                                && Main.npc[segment].ai[3] == npc.ai[3] && Main.npc[segment].ai[0] == Main.npc[prev].whoAmI)
                            {
                                prev = segment;
                                segment = (int)Main.npc[segment].ai[1]; //continue if next is a valid BODY segment
                            }
                            else
                            {
                                break; //stop otherwise (this includes if head is found early, which is okay!)
                            }
                        }

                        //if last segment seen is indeed destroyer head
                        if (segment > -1 && segment < Main.maxNPCs && Main.npc[segment].active && Main.npc[segment].type == NPCID.TheDestroyer)
                        {
                            if (i == maxLength && npc.type != NPCID.TheDestroyerTail) //i am the furthest possible segment, become tail
                            {
                                //Main.NewText("body: become tail");
                                npc.type = NPCID.TheDestroyerTail;
                                npc.ai[0] = 0f;
                                npc.ai[2] = 0f;
                                npc.localAI[0] = 0f;
                                npc.localAI[1] = 0f;
                                npc.localAI[2] = 0f;
                                npc.localAI[3] = 0f;
                                if (Main.netMode == NetmodeID.Server)
                                    NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
                            }
                        }
                        else //last segment seen isn't destroyer head, die
                        {
                            //Main.NewText("body killed by wrong lead");
                            npc.life = 0;
                            npc.HitEffect();
                            npc.active = false;
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncNPC, number: npc.whoAmI);
                            return false;
                        }
                    }
                    break;

                case NPCID.BlueSlime:
                    if (FargoWorld.OverloadedSlimeRain && npc.netID == NPCID.GreenSlime)
                    {
                        int[] slimes = { NPCID.BlueSlime, NPCID.RedSlime, NPCID.PurpleSlime, NPCID.YellowSlime, NPCID.BlackSlime, NPCID.JungleSlime };

                        npc.SetDefaults(slimes[Main.rand.Next(slimes.Length)]);

                        if (Main.netMode == NetmodeID.Server)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, npc.whoAmI);
                    }

                    break;

                default:
                    break;
            }

            return true;
        }

        public override void AI(NPC npc)
        {
            // Wack ghost saucers begone
            if (FargoWorld.OverloadMartians && npc.type == NPCID.MartianSaucerCore && npc.dontTakeDamage)
            {
                npc.dontTakeDamage = false;
            }

            if (Fargowiltas.SwarmActive && Fargowiltas.ModLoaded["ThoriumMod"])
            {
                Mod thorium = ModLoader.GetMod("ThoriumMod");

                if (npc.type == thorium.NPCType("BoreanStriderPopped") || npc.type == thorium.NPCType("FallenDeathBeholder2") || npc.type == thorium.NPCType("LichHeadless") || npc.type == thorium.NPCType("AbyssionReleased"))
                {
                    SwarmActive = true;
                }
            }
        }

        public override void PostAI(NPC npc)
        {
            if (SwarmActive && npc.type == NPCID.Golem)
                npc.dontTakeDamage = false; //always vulnerable in swarm
        }

        public override void SetupShop(int type, Chest shop, ref int nextSlot)
        {
            Player player = Main.LocalPlayer;

            if (GetInstance<FargoConfig>().NPCSales)
            {
                switch (type)
                {
                    case NPCID.Clothier:
                        shop.item[nextSlot].SetDefaults(ItemID.PharaohsMask);
                        shop.item[nextSlot++].shopCustomPrice = 10000;

                        shop.item[nextSlot].SetDefaults(ItemID.PharaohsRobe);
                        shop.item[nextSlot++].shopCustomPrice = 10000;

                        if (player.anglerQuestsFinished >= 10)
                        {
                            shop.item[nextSlot++].SetDefaults(ItemID.AnglerHat);

                            if (player.anglerQuestsFinished >= 15)
                            {
                                shop.item[nextSlot++].SetDefaults(ItemID.AnglerVest);

                                if (player.anglerQuestsFinished >= 20)
                                {
                                    shop.item[nextSlot++].SetDefaults(ItemID.AnglerPants);
                                }
                            }
                        }

                        shop.item[nextSlot].SetDefaults(ItemID.BlueBrick);
                        shop.item[nextSlot++].shopCustomPrice = 100;
                        shop.item[nextSlot].SetDefaults(ItemID.GreenBrick);
                        shop.item[nextSlot++].shopCustomPrice = 100;
                        shop.item[nextSlot].SetDefaults(ItemID.PinkBrick);
                        shop.item[nextSlot++].shopCustomPrice = 100;
                        
                        foreach (Item item in Main.LocalPlayer.inventory)
                        {
                            if (item.stack > 0 && item.useAmmo == ItemID.Bone)
                            {
                                shop.item[nextSlot++].SetDefaults(ModContent.ItemType<Items.Ammos.BrittleBone>());
                                break;
                            }
                        }
                        break;

                    case NPCID.Merchant:
                        if (player.anglerQuestsFinished >= 5)
                        {
                            shop.item[nextSlot].SetDefaults(ItemID.FuzzyCarrot);
                            nextSlot++;

                            if (player.anglerQuestsFinished >= 10)
                            {
                                shop.item[nextSlot].SetDefaults(ItemID.AnglerEarring);
                                nextSlot++;

                                shop.item[nextSlot].SetDefaults(ItemID.HighTestFishingLine);
                                nextSlot++;

                                shop.item[nextSlot].SetDefaults(ItemID.TackleBox);
                                nextSlot++;

                                shop.item[nextSlot].SetDefaults(ItemID.GoldenBugNet);
                                nextSlot++;

                                shop.item[nextSlot].SetDefaults(ItemID.FishHook);
                                nextSlot++;

                                if (Main.hardMode)
                                {
                                    shop.item[nextSlot].SetDefaults(ItemID.FinWings);
                                    nextSlot++;

                                    shop.item[nextSlot].SetDefaults(ItemID.SuperAbsorbantSponge);
                                    nextSlot++;

                                    shop.item[nextSlot].SetDefaults(ItemID.BottomlessBucket);
                                    nextSlot++;

                                    if (player.anglerQuestsFinished >= 25)
                                    {
                                        shop.item[nextSlot].SetDefaults(ItemID.HotlineFishingHook);
                                        nextSlot++;

                                        if (player.anglerQuestsFinished >= 30)
                                        {
                                            shop.item[nextSlot].SetDefaults(ItemID.GoldenFishingRod);
                                            nextSlot++;
                                        }
                                    }
                                }
                            }
                        }

                        break;

                    case NPCID.Painter:

                        if (player.ZoneDungeon)
                        {
                            nextSlot = 15;

                            shop.item[nextSlot++].SetDefaults(ItemID.BloodMoonRising);
                            shop.item[nextSlot++].SetDefaults(ItemID.BoneWarp);
                            shop.item[nextSlot++].SetDefaults(ItemID.TheCreationoftheGuide);
                            shop.item[nextSlot++].SetDefaults(ItemID.TheCursedMan);
                            shop.item[nextSlot++].SetDefaults(ItemID.TheDestroyer);
                            shop.item[nextSlot++].SetDefaults(ItemID.Dryadisque);
                            shop.item[nextSlot++].SetDefaults(ItemID.TheEyeSeestheEnd);
                            shop.item[nextSlot++].SetDefaults(ItemID.FacingtheCerebralMastermind);
                            shop.item[nextSlot++].SetDefaults(ItemID.GloryoftheFire);
                            shop.item[nextSlot++].SetDefaults(ItemID.GoblinsPlayingPoker);
                            shop.item[nextSlot++].SetDefaults(ItemID.GreatWave);
                            shop.item[nextSlot++].SetDefaults(ItemID.TheGuardiansGaze);
                            shop.item[nextSlot++].SetDefaults(ItemID.TheHangedMan);
                            shop.item[nextSlot++].SetDefaults(ItemID.Impact);
                            shop.item[nextSlot++].SetDefaults(ItemID.ThePersistencyofEyes);
                            shop.item[nextSlot++].SetDefaults(ItemID.PoweredbyBirds);
                            shop.item[nextSlot++].SetDefaults(ItemID.TheScreamer);
                            shop.item[nextSlot++].SetDefaults(ItemID.SkellingtonJSkellingsworth);
                            shop.item[nextSlot++].SetDefaults(ItemID.SparkyPainting);
                            shop.item[nextSlot++].SetDefaults(ItemID.SomethingEvilisWatchingYou);
                            shop.item[nextSlot++].SetDefaults(ItemID.StarryNight);
                            shop.item[nextSlot++].SetDefaults(ItemID.TrioSuperHeroes);
                            shop.item[nextSlot++].SetDefaults(ItemID.TheTwinsHaveAwoken);
                            shop.item[nextSlot++].SetDefaults(ItemID.UnicornCrossingtheHallows);
                        }
                        else if (player.ZoneRockLayerHeight || player.ZoneDirtLayerHeight)
                        {
                            nextSlot = 19;

                            shop.item[nextSlot++].SetDefaults(ItemID.AmericanExplosive);
                            shop.item[nextSlot++].SetDefaults(ItemID.CrownoDevoursHisLunch);
                            shop.item[nextSlot++].SetDefaults(ItemID.Discover);
                            shop.item[nextSlot++].SetDefaults(ItemID.FatherofSomeone);
                            shop.item[nextSlot++].SetDefaults(ItemID.FindingGold);
                            shop.item[nextSlot++].SetDefaults(ItemID.GloriousNight);
                            shop.item[nextSlot++].SetDefaults(ItemID.GuidePicasso);
                            shop.item[nextSlot++].SetDefaults(ItemID.Land);
                            shop.item[nextSlot++].SetDefaults(ItemID.TheMerchant);
                            shop.item[nextSlot++].SetDefaults(ItemID.NurseLisa);
                            shop.item[nextSlot++].SetDefaults(ItemID.OldMiner);
                            shop.item[nextSlot++].SetDefaults(ItemID.RareEnchantment);
                            shop.item[nextSlot++].SetDefaults(ItemID.Sunflowers);
                            shop.item[nextSlot++].SetDefaults(ItemID.TerrarianGothic);
                            shop.item[nextSlot++].SetDefaults(ItemID.Waldo);
                        }
                        else if (player.ZoneUnderworldHeight)
                        {
                            nextSlot = 19;

                            shop.item[nextSlot++].SetDefaults(ItemID.DarkSoulReaper);
                            shop.item[nextSlot++].SetDefaults(ItemID.Darkness);
                            shop.item[nextSlot++].SetDefaults(ItemID.DemonsEye);
                            shop.item[nextSlot++].SetDefaults(ItemID.FlowingMagma);
                            shop.item[nextSlot++].SetDefaults(ItemID.HandEarth);
                            shop.item[nextSlot++].SetDefaults(ItemID.ImpFace);
                            shop.item[nextSlot++].SetDefaults(ItemID.LakeofFire);
                            shop.item[nextSlot++].SetDefaults(ItemID.LivingGore);
                            shop.item[nextSlot++].SetDefaults(ItemID.OminousPresence);
                            shop.item[nextSlot++].SetDefaults(ItemID.ShiningMoon);
                            shop.item[nextSlot++].SetDefaults(ItemID.Skelehead);
                            shop.item[nextSlot++].SetDefaults(ItemID.TrappedGhost);
                        }
                        //deserttt

                        break;

                    case NPCID.Demolitionist:
                        if (Main.hardMode)
                        {
                            shop.item[nextSlot++].SetDefaults(ItemID.CopperOre);
                            shop.item[nextSlot++].SetDefaults(ItemID.TinOre);
                            shop.item[nextSlot++].SetDefaults(ItemID.IronOre);
                            shop.item[nextSlot++].SetDefaults(ItemID.LeadOre);
                            shop.item[nextSlot++].SetDefaults(ItemID.SilverOre);
                            shop.item[nextSlot++].SetDefaults(ItemID.TungstenOre);
                            shop.item[nextSlot++].SetDefaults(ItemID.GoldOre);
                            shop.item[nextSlot++].SetDefaults(ItemID.PlatinumOre);
                        }

                        if (NPC.downedMoonlord)
                        {
                            shop.item[nextSlot++].SetDefaults(ItemID.CobaltOre);
                            shop.item[nextSlot++].SetDefaults(ItemID.PalladiumOre);
                            shop.item[nextSlot++].SetDefaults(ItemID.MythrilOre);
                            shop.item[nextSlot++].SetDefaults(ItemID.OrichalcumOre);
                            shop.item[nextSlot++].SetDefaults(ItemID.AdamantiteOre);
                            shop.item[nextSlot++].SetDefaults(ItemID.TitaniumOre);
                            shop.item[nextSlot++].SetDefaults(ItemID.ChlorophyteOre);
                        }

                        break;

                    case NPCID.Steampunker:
                        shop.item[nextSlot++].SetDefaults(WorldGen.crimson ? ItemID.PurpleSolution : ItemID.RedSolution);
                        shop.item[nextSlot++].SetDefaults(WorldGen.crimson ? ItemID.FleshCloningVaat : ItemID.FleshCloningVaat); //DECAY CHAMBER

                        break;

                    case NPCID.DyeTrader:
                        FargoPlayer modPlayer = player.GetModPlayer<FargoPlayer>();
                        if (modPlayer.FirstDyeIngredients["RedHusk"])
                        {
                            shop.item[nextSlot++].SetDefaults(ItemID.RedHusk);
                        }
                        if (modPlayer.FirstDyeIngredients["OrangeBloodroot"])
                        {
                            shop.item[nextSlot++].SetDefaults(ItemID.OrangeBloodroot);
                        }
                        if (modPlayer.FirstDyeIngredients["YellowMarigold"])
                        {
                            shop.item[nextSlot++].SetDefaults(ItemID.YellowMarigold);
                        }
                        if (modPlayer.FirstDyeIngredients["LimeKelp"])
                        {
                            shop.item[nextSlot++].SetDefaults(ItemID.LimeKelp);
                        }
                        if (modPlayer.FirstDyeIngredients["GreenMushroom"])
                        {
                            shop.item[nextSlot++].SetDefaults(ItemID.GreenMushroom);
                        }
                        if (modPlayer.FirstDyeIngredients["TealMushroom"])
                        {
                            shop.item[nextSlot++].SetDefaults(ItemID.TealMushroom);
                        }
                        if (modPlayer.FirstDyeIngredients["CyanHusk"])
                        {
                            shop.item[nextSlot++].SetDefaults(ItemID.CyanHusk);
                        }
                        if (modPlayer.FirstDyeIngredients["SkyBlueFlower"])
                        {
                            shop.item[nextSlot++].SetDefaults(ItemID.SkyBlueFlower);
                        }
                        if (modPlayer.FirstDyeIngredients["BlueBerries"])
                        {
                            shop.item[nextSlot++].SetDefaults(ItemID.BlueBerries);
                        }
                        if (modPlayer.FirstDyeIngredients["PurpleMucos"])
                        {
                            shop.item[nextSlot++].SetDefaults(ItemID.PurpleMucos);
                        }
                        if (modPlayer.FirstDyeIngredients["VioletHusk"])
                        {
                            shop.item[nextSlot++].SetDefaults(ItemID.VioletHusk);
                        }
                        if (modPlayer.FirstDyeIngredients["PinkPricklyPear"])
                        {
                            shop.item[nextSlot++].SetDefaults(ItemID.PinkPricklyPear);
                        }
                        if (modPlayer.FirstDyeIngredients["BlackInk"])
                        {
                            shop.item[nextSlot++].SetDefaults(ItemID.BlackInk);
                        }

                        break;

                    case NPCID.Dryad:
                        if (Main.hardMode)
                        {
                            shop.item[nextSlot].SetDefaults(ItemID.NaturesGift);
                            shop.item[nextSlot++].shopCustomPrice = 200000;
                            shop.item[nextSlot].SetDefaults(ItemID.JungleRose);
                            shop.item[nextSlot++].shopCustomPrice = 100000;

                            shop.item[nextSlot].SetDefaults(ItemID.StrangePlant1);
                            shop.item[nextSlot++].shopCustomPrice = 50000;
                            shop.item[nextSlot].SetDefaults(ItemID.StrangePlant2);
                            shop.item[nextSlot++].shopCustomPrice = 50000;
                            shop.item[nextSlot].SetDefaults(ItemID.StrangePlant3);
                            shop.item[nextSlot++].shopCustomPrice = 50000;
                            shop.item[nextSlot].SetDefaults(ItemID.StrangePlant4);
                            shop.item[nextSlot++].shopCustomPrice = 50000;
                        }
                        break;

                    case NPCID.Wizard:
                        if (NPC.downedGolemBoss)
                            shop.item[nextSlot].SetDefaults(ItemID.SuperManaPotion);
                        break;
                }
            }
        }

        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            if (player.GetFargoPlayer().BattleCry)
            {
                spawnRate = (int)(spawnRate * 0.1);
                maxSpawns = (int)(maxSpawns * 10f);
            }

            if ((FargoWorld.OverloadGoblins || FargoWorld.OverloadPirates) && player.position.X > Main.invasionX * 16.0 - 3000 && player.position.X < Main.invasionX * 16.0 + 3000)
            {
                if (FargoWorld.OverloadGoblins)
                {
                    spawnRate = (int)(spawnRate * 0.2);
                    maxSpawns = (int)(maxSpawns * 10f);
                }
                else if (FargoWorld.OverloadPirates)
                {
                    spawnRate = (int)(spawnRate * 0.2);
                    maxSpawns = (int)(maxSpawns * 30f);
                }
            }

            if (FargoWorld.OverloadPumpkinMoon || FargoWorld.OverloadFrostMoon)
            {
                spawnRate = (int)(spawnRate * 0.2);
                maxSpawns = (int)(maxSpawns * 10f);
            }
            else if (FargoWorld.OverloadMartians)
            {
                spawnRate = (int)(spawnRate * 0.2);
                maxSpawns = (int)(maxSpawns * 30f);
            }

            if (GetInstance<FargoConfig>().BossZen && AnyBossAlive())
            {
                maxSpawns = 0;
            }
        }

        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            Player player = Main.LocalPlayer;

            if (FargoWorld.OverloadGoblins && player.position.X > Main.invasionX * 16.0 - 3000 && player.position.X < Main.invasionX * 16.0 + 3000)
            {
                // Literally nothing in the pool in the invasion so set everything to custom
                pool[NPCID.GoblinSummoner] = 1f;
                pool[NPCID.GoblinArcher] = 3f;
                pool[NPCID.GoblinPeon] = 5f;
                pool[NPCID.GoblinSorcerer] = 3f;
                pool[NPCID.GoblinWarrior] = 5f;
                pool[NPCID.GoblinThief] = 5f;
                pool[NPCID.GoblinScout] = 3f;
            }
            else if (FargoWorld.OverloadPirates && player.position.X > Main.invasionX * 16.0 - 3000 && player.position.X < Main.invasionX * 16.0 + 3000)
            {
                // Literally nothing in the pool in the invasion so set everything to custom
                if (NPC.CountNPCS(NPCID.PirateShip) < 4)
                {
                    pool[NPCID.PirateShip] = .5f;
                }

                pool[NPCID.Parrot] = 2f;
                pool[NPCID.PirateCaptain] = 1f;
                pool[NPCID.PirateCrossbower] = 3f;
                pool[NPCID.PirateCorsair] = 5f;
                pool[NPCID.PirateDeadeye] = 4f;
                pool[NPCID.PirateDeckhand] = 5f;
            }

            else if (FargoWorld.OverloadPumpkinMoon)
            {
                pool[NPCID.Pumpking] = 4f;
                pool[NPCID.MourningWood] = 4f;
                pool[NPCID.HeadlessHorseman] = 3f;
                pool[NPCID.Scarecrow1] = .5f;
                pool[NPCID.Scarecrow2] = .5f;
                pool[NPCID.Scarecrow3] = .5f;
                pool[NPCID.Scarecrow4] = .5f;
                pool[NPCID.Scarecrow5] = .5f;
                pool[NPCID.Scarecrow6] = .5f;
                pool[NPCID.Scarecrow7] = .5f;
                pool[NPCID.Scarecrow8] = .5f;
                pool[NPCID.Scarecrow9] = .5f;
                pool[NPCID.Scarecrow10] = .5f;
                pool[NPCID.Hellhound] = 3f;
                pool[NPCID.Poltergeist] = 3f;
                pool[NPCID.Splinterling] = 3f;
            }
            else if (FargoWorld.OverloadFrostMoon)
            {
                pool[NPCID.IceQueen] = 5f;
                pool[NPCID.Everscream] = 5f;
                pool[NPCID.SantaNK1] = 5f;
                pool[NPCID.ZombieElf] = 1f;
                pool[NPCID.ZombieElfBeard] = 1f;
                pool[NPCID.ZombieElfGirl] = 1f;
                pool[NPCID.GingerbreadMan] = 2f;
                pool[NPCID.ElfArcher] = 2f;
                pool[NPCID.Nutcracker] = 3f;
                pool[NPCID.ElfCopter] = 3f;
                pool[NPCID.Flocko] = 2f;
                pool[NPCID.Yeti] = 4f;
                pool[NPCID.PresentMimic] = 2f;
                pool[NPCID.Krampus] = 4f;
            }
            else if (FargoWorld.OverloadMartians)
            {
                pool[NPCID.MartianSaucerCore] = 1f;
                pool[NPCID.Scutlix] = 3f;
                pool[NPCID.ScutlixRider] = 2f;
                pool[NPCID.MartianWalker] = 3f;
                pool[NPCID.MartianDrone] = 2f;
                pool[NPCID.GigaZapper] = 1f;
                pool[NPCID.MartianEngineer] = 2f;
                pool[NPCID.MartianOfficer] = 2f;
                pool[NPCID.RayGunner] = 1f;
                pool[NPCID.GrayGrunt] = 1f;
                pool[NPCID.BrainScrambler] = 1f;
            }
        }

        public override bool PreNPCLoot(NPC npc)
        {
            if (NoLoot)
            {
                return false;
            }

            if (Fargowiltas.SwarmActive && (npc.type == NPCID.BlueSlime || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail || npc.type == NPCID.Creeper || (npc.type >= NPCID.PirateCorsair && npc.type <= NPCID.PirateCrossbower)))
            {
                return false;
            }

            if (SwarmActive && Fargowiltas.SwarmActive && Main.netMode != NetmodeID.MultiplayerClient)
            {
                switch (npc.type)
                {
                    case NPCID.KingSlime:
                        Swarm(npc, NPCID.KingSlime, NPCID.BlueSlime, ItemID.KingSlimeBossBag, ItemID.KingSlimeTrophy, "EnergizerSlime");
                        break;

                    case NPCID.EyeofCthulhu:
                        Swarm(npc, NPCID.EyeofCthulhu, NPCID.ServantofCthulhu, ItemID.EyeOfCthulhuBossBag, ItemID.EyeofCthulhuTrophy, "EnergizerEye");
                        break;

                    case NPCID.EaterofWorldsHead:
                        Swarm(npc, NPCID.EaterofWorldsHead, NPCID.EaterofWorldsTail, ItemID.EaterOfWorldsBossBag, ItemID.EaterofWorldsTrophy, "EnergizerWorm");
                        break;

                    case NPCID.BrainofCthulhu:
                        Swarm(npc, NPCID.BrainofCthulhu, NPCID.Creeper, ItemID.BrainOfCthulhuBossBag, ItemID.BrainofCthulhuTrophy, "EnergizerBrain");
                        break;

                    case NPCID.DD2DarkMageT1:
                        Swarm(npc, NPCID.DD2DarkMageT1, -1, ItemID.DefenderMedal, ItemID.BossTrophyDarkmage, "EnergizerDarkMage");
                        break;

                    case NPCID.QueenBee:
                        Swarm(npc, NPCID.QueenBee, NPCID.BeeSmall, ItemID.QueenBeeBossBag, ItemID.QueenBeeTrophy, "EnergizerBee");
                        break;

                    case NPCID.SkeletronHead:
                        Swarm(npc, NPCID.SkeletronHead, -1, ItemID.SkeletronBossBag, ItemID.SkeletronTrophy, "EnergizerSkele");
                        break;

                    case NPCID.WallofFlesh:
                        Swarm(npc, NPCID.WallofFlesh, NPCID.TheHungry, ItemID.WallOfFleshBossBag, ItemID.WallofFleshTrophy, "EnergizerWall");
                        break;

                    case NPCID.TheDestroyer:
                        Swarm(npc, NPCID.TheDestroyer, NPCID.Probe, ItemID.DestroyerBossBag, ItemID.DestroyerTrophy, "EnergizerDestroy");
                        break;

                    case NPCID.Retinazer:
                        Swarm(npc, NPCID.Retinazer, -1, ItemID.TwinsBossBag, ItemID.RetinazerTrophy, "EnergizerTwins");
                        break;

                    case NPCID.Spazmatism:
                        Swarm(npc, NPCID.Spazmatism, -1, -1, ItemID.SpazmatismTrophy, string.Empty);
                        break;

                    case NPCID.SkeletronPrime:
                        Swarm(npc, NPCID.SkeletronPrime, -1, ItemID.SkeletronPrimeBossBag, ItemID.SkeletronPrimeTrophy, "EnergizerPrime");
                        break;

                    case NPCID.Plantera:
                        Swarm(npc, NPCID.Plantera, NPCID.PlanterasHook, ItemID.PlanteraBossBag, ItemID.PlanteraTrophy, "EnergizerPlant");
                        break;

                    case NPCID.Golem:
                        Swarm(npc, NPCID.Golem, NPCID.GolemHeadFree, ItemID.GolemBossBag, ItemID.GolemTrophy, "EnergizerGolem");
                        break;

                    case NPCID.DD2Betsy:
                        Swarm(npc, NPCID.DD2Betsy, NPCID.DD2WyvernT3, ItemID.BossBagBetsy, ItemID.BossTrophyBetsy, "EnergizerBetsy");
                        break;

                    case NPCID.DukeFishron:
                        Swarm(npc, NPCID.DukeFishron, NPCID.Sharkron, ItemID.FishronBossBag, ItemID.DukeFishronTrophy, "EnergizerFish");
                        break;

                    case NPCID.CultistBoss:
                        Swarm(npc, NPCID.CultistBoss, -1, ItemID.CultistBossBag, ItemID.AncientCultistTrophy, "EnergizerCultist");
                        break;

                    case NPCID.MoonLordCore:
                        Swarm(npc, NPCID.MoonLordCore, NPCID.MoonLordFreeEye, ItemID.MoonLordBossBag, ItemID.MoonLordTrophy, "EnergizerMoon");
                        break;

                    /*case NPCID.MourningWood:
                        Swarm(npc, NPCID.MourningWood, -1, -1, string.Empty);
                        break;

                    case NPCID.Pumpking:
                        Swarm(npc, NPCID.Pumpking, -1, -1, string.Empty);
                        break;

                    case NPCID.Everscream:
                        Swarm(npc, NPCID.Everscream, -1, -1, string.Empty);
                        break;

                    case NPCID.SantaNK1:
                        Swarm(npc, NPCID.SantaNK1, -1, -1, string.Empty);
                        break;

                    case NPCID.IceQueen:
                        Swarm(npc, NPCID.IceQueen, -1, -1, string.Empty);
                        break;

                    case NPCID.DD2OgreT3:
                        Swarm(npc, NPCID.DD2OgreT3, -1, -1, string.Empty);
                        break;

                    case NPCID.PirateShip:
                        Swarm(npc, NPCID.PirateShip, -1, -1, string.Empty);
                        break;

                    case NPCID.MartianSaucerCore:
                        Swarm(npc, NPCID.MartianSaucerCore, -1, -1, string.Empty);
                        break;*/

                    case NPCID.DungeonGuardian:
                        Swarm(npc, NPCID.DungeonGuardian, -1, -1, ItemID.BoneKey, "EnergizerDG");
                        break;
                }




                if (Fargowiltas.ModLoaded["ThoriumMod"])
                {
                    Mod thorium = ModLoader.GetMod("ThoriumMod");

                    if (npc.type == thorium.NPCType("TheGrandThunderBirdv2"))
                    {
                        Swarm(npc, thorium.NPCType("TheGrandThunderBirdv2"), thorium.NPCType("Hatchling"), thorium.ItemType("ThunderBirdBag"), -1, string.Empty);
                    }
                    else if (npc.type == thorium.NPCType("QueenJelly"))
                    {
                        Swarm(npc, thorium.NPCType("QueenJelly"), thorium.NPCType("ZealousJelly"), thorium.ItemType("JellyFishBag"), -1, string.Empty);
                    }
                    else if (npc.type == thorium.NPCType("GraniteEnergyStorm"))
                    {
                        Swarm(npc, thorium.NPCType("GraniteEnergyStorm"), thorium.NPCType("EncroachingEnergy"), thorium.ItemType("GraniteBag"), -1, string.Empty);
                    }
                    else if (npc.type == thorium.NPCType("TheBuriedWarrior"))
                    {
                        Swarm(npc, thorium.NPCType("TheBuriedWarrior"), -1, thorium.ItemType("HeroBag"), -1, string.Empty);
                    }
                    else if (npc.type == thorium.NPCType("Viscount"))
                    {
                        Swarm(npc, thorium.NPCType("Viscount"), -1, thorium.ItemType("CountBag"), -1, string.Empty);
                    }
                    else if (npc.type == thorium.NPCType("ThePrimeScouter"))
                    {
                        Swarm(npc, thorium.NPCType("ThePrimeScouter"), -1, thorium.ItemType("ScouterBag"), -1, string.Empty);
                    }
                    else if (npc.type == thorium.NPCType("BoreanStriderPopped"))
                    {
                        Swarm(npc, thorium.NPCType("BoreanStrider"), thorium.ItemType("BoreanMyte1"), thorium.ItemType("BoreanBag"), -1, string.Empty);
                    }
                    else if (npc.type == thorium.NPCType("FallenDeathBeholder2"))
                    {
                        Swarm(npc, thorium.NPCType("FallenDeathBeholder"), thorium.ItemType("EnemyBeholder"), thorium.ItemType("BeholderBag"), -1, string.Empty);
                    }
                    else if (npc.type == thorium.NPCType("LichHeadless"))
                    {
                        Swarm(npc, thorium.NPCType("Lich"), -1, thorium.ItemType("LichBag"), -1, string.Empty);
                    }
                    else if (npc.type == thorium.NPCType("AbyssionReleased"))
                    {
                        Swarm(npc, thorium.NPCType("Abyssion"), thorium.NPCType("AbyssalSpawn"), thorium.ItemType("AbyssionBag"), -1, string.Empty);
                    }
                    else if (npc.type == thorium.NPCType("RealityBreaker"))
                    {
                        Swarm(npc, thorium.NPCType("Aquaius"), thorium.NPCType("AquaiusBubble"), thorium.ItemType("RagBag"), -1, string.Empty);
                        Swarm(npc, thorium.NPCType("Omnicide"), -1, -1, -1, string.Empty);
                        Swarm(npc, thorium.NPCType("SlagFury"), -1, -1, -1, string.Empty);
                    }
                }

                return false;
            }

            if (!PandoraActive)
            {
                return true;
            }

            switch (npc.type)
            {
                case NPCID.GreekSkeleton:
                    NPCLoader.blockLoot.Add(ItemID.GladiatorHelmet);
                    NPCLoader.blockLoot.Add(ItemID.GladiatorBreastplate);
                    NPCLoader.blockLoot.Add(ItemID.GladiatorLeggings);
                    break;
            }

            return false;
        }

        public override void NPCLoot(NPC npc)
        {
            void TryDowned(params string[] names)
            {
                bool update = false;

                foreach (string name in names)
                {
                    if (!FargoWorld.DownedBools[name])
                    {
                        FargoWorld.DownedBools[name] = true;
                        update = true;
                    }
                }

                if (update && Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.WorldData); //sync world
            };

            // Lumber Jaxe
            if (npc.FindBuffIndex(mod.BuffType("WoodDrop")) != -1)
            {
                Item.NewItem(npc.Hitbox, ItemID.Wood, Main.rand.Next(10, 30));
            }

            switch (npc.type)
            {
                // Avoid lunar event with cultist summon
                case NPCID.CultistBoss:
                    if (!PillarSpawn)
                    {
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC npc2 = Main.npc[i];
                            NPC.LunarApocalypseIsUp = false;

                            if (npc2.type == NPCID.LunarTowerNebula || npc2.type == NPCID.LunarTowerSolar || npc2.type == NPCID.LunarTowerStardust || npc2.type == NPCID.LunarTowerVortex)
                            {
                                NPC.TowerActiveSolar = true;
                                npc2.active = false;
                            }

                            NPC.TowerActiveSolar = false;
                        }
                    }

                    break;

                case NPCID.ZombieEskimo:
                case NPCID.ArmedZombieEskimo:
                case NPCID.Penguin:
                case NPCID.IceSlime:
                case NPCID.SpikedIceSlime:
                    if (Main.rand.NextBool(10))
                    {
                        int i = Main.rand.Next(3);
                        switch (i)
                        {
                            case 0:
                                Item.NewItem(npc.Hitbox, ItemID.EskimoHood);
                                break;

                            case 1:
                                Item.NewItem(npc.Hitbox, ItemID.EskimoCoat);
                                break;

                            default:
                                Item.NewItem(npc.Hitbox, ItemID.EskimoPants);
                                break;
                        }
                    }
                    break;


                case NPCID.GreekSkeleton:
                    if (Main.rand.NextBool(10))
                    {
                        int i = Main.rand.Next(3);
                        switch (i)
                        {
                            case 0:
                                Item.NewItem(npc.Hitbox, ItemID.GladiatorHelmet);
                                break;

                            case 1:
                                Item.NewItem(npc.Hitbox, ItemID.GladiatorBreastplate);
                                break;

                            default:
                                Item.NewItem(npc.Hitbox, ItemID.GladiatorLeggings);
                                break;
                        }
                    }

                    break;

                case NPCID.Merchant:
                    if (Main.rand.NextBool(8))
                    {
                        Item.NewItem(npc.Hitbox, ItemID.MiningShirt);
                        Item.NewItem(npc.Hitbox, ItemID.MiningPants);
                    }

                    break;

                case NPCID.Nurse:
                    if (Main.rand.NextBool(5))
                    {
                        Item.NewItem(npc.Hitbox, ItemID.LifeCrystal);
                    }

                    break;

                case NPCID.Demolitionist:
                    if (Main.rand.NextBool(2))
                    {
                        Item.NewItem(npc.Hitbox, ItemID.Dynamite, 5);
                    }

                    break;

                case NPCID.Dryad:
                    if (Main.rand.NextBool(3))
                    {
                        Item.NewItem(npc.Hitbox, ItemID.HerbBag);
                    }

                    break;

                case NPCID.DD2Bartender:
                    if (Main.rand.NextBool(2))
                    {
                        Item.NewItem(npc.Hitbox, ItemID.Ale, 4);
                    }

                    break;

                case NPCID.ArmsDealer:
                    if (Main.rand.NextBool(4))
                    {
                        Item.NewItem(npc.Hitbox, ItemID.NanoBullet, 30);
                    }

                    break;

                case NPCID.Painter:
                    if (NPC.AnyNPCs(NPCID.MoonLordCore))
                    {
                        Item.NewItem(npc.Hitbox, mod.ItemType("EchPainting"));
                    }

                    break;

                case NPCID.Clothier:
                    if (Main.rand.NextBool(20))
                    {
                        Item.NewItem(npc.Hitbox, ItemID.Skull);
                    }

                    break;

                case NPCID.Mechanic:
                    if (Main.rand.NextBool(5))
                    {
                        Item.NewItem(npc.Hitbox, ItemID.Wire, 40);
                    }

                    break;

                case NPCID.Wizard:
                    if (Main.rand.NextBool(5))
                    {
                        Item.NewItem(npc.Hitbox, ItemID.FallenStar, 5);
                    }

                    break;

                case NPCID.TaxCollector:
                    if (Main.rand.NextBool(8))
                    {
                        Item.NewItem(npc.Hitbox, ItemID.GoldCoin, 10);
                    }

                    break;

                case NPCID.Truffle:
                    if (Main.rand.NextBool(8))
                    {
                        Item.NewItem(npc.Hitbox, ItemID.MushroomStatue);
                    }

                    break;

                case NPCID.Angler:
                    if (Main.rand.NextBool(2))
                    {
                        int[] fishingTrash = new int[] { ItemID.OldShoe, ItemID.TinCan, ItemID.FishingSeaweed };

                        Item.NewItem(npc.Hitbox, Main.rand.Next(fishingTrash));
                    }
                    break;

                case NPCID.Pumpking:

                    break;

                case NPCID.IceQueen:

                    break;

                case NPCID.GiantWormHead:
                case NPCID.DiggerHead:
                    TryDowned("worm");
                    break;

                case NPCID.DD2OgreT2:
                case NPCID.DD2OgreT3:
                    TryDowned("ogre");

                    Item.NewItem(npc.Hitbox, ItemID.DefenderMedal, 20);

                    if (!DD2Event.Ongoing)
                    {
                        if (Main.rand.NextBool(14))
                        {
                            Item.NewItem(npc.Hitbox, ItemID.BossMaskOgre);
                        }

                        Item.NewItem(npc.Hitbox, Main.rand.Next(new short[] { ItemID.ApprenticeScarf, ItemID.SquireShield, ItemID.HuntressBuckler, ItemID.MonkBelt, ItemID.DD2SquireDemonSword, ItemID.MonkStaffT1, ItemID.MonkStaffT2, ItemID.BookStaff, ItemID.DD2PhoenixBow, ItemID.DD2PetGhost }));

                        Item.NewItem(npc.Hitbox, ItemID.GoldCoin, Main.rand.Next(4, 7));
                    }

                    break;

                case NPCID.DD2DarkMageT1:
                case NPCID.DD2DarkMageT3:
                    TryDowned("darkMage");

                    Item.NewItem(npc.Hitbox, ItemID.DefenderMedal, 5);

                    if (!DD2Event.Ongoing)
                    {
                        if (Main.rand.NextBool(14))
                        {
                            Item.NewItem(npc.Hitbox, ItemID.BossMaskDarkMage);
                        }

                        if (Main.rand.NextBool(10))
                        {
                            if (Main.rand.NextBool(2))
                            {
                                Item.NewItem(npc.Hitbox, ItemID.WarTableBanner);
                            }
                            else
                            {
                                Item.NewItem(npc.Hitbox, ItemID.WarTable);
                            }
                        }

                        if (Main.rand.NextBool(6))
                        {
                            Item.NewItem(npc.Hitbox, Main.rand.Next(new short[] { ItemID.DD2PetGato, ItemID.DD2PetDragon }));
                        }

                        if (DD2Event.ShouldDropCrystals())
                        {
                            Item.NewItem(npc.Hitbox, ItemID.DD2EnergyCrystal);
                        }
                    }

                    break;

                case NPCID.Raven:
                    Item.NewItem(npc.Hitbox, ItemID.GoodieBag);

                    break;

                case NPCID.SlimeRibbonRed:
                case NPCID.SlimeRibbonGreen:
                case NPCID.SlimeRibbonWhite:
                case NPCID.SlimeRibbonYellow:
                    Item.NewItem(npc.Hitbox, ItemID.Present);

                    break;

                case NPCID.BloodZombie:
                    if (Main.rand.NextBool(200))
                    {
                        Item.NewItem(npc.Hitbox, Main.rand.NextBool(2) ? ItemID.BladedGlove : ItemID.BloodyMachete);
                    }

                    break;

                case NPCID.Clown:
                    Item.NewItem(npc.Hitbox, ItemID.Bananarang);

                    TryDowned("rareEnemy", "clown");
                    break;

                case NPCID.BlueSlime:
                    if (npc.netID == NPCID.Pinky)
                    {
                        TryDowned("rareEnemy", "pinky");
                    }
                    break;

                case NPCID.UndeadMiner:
                    TryDowned("rareEnemy", "undeadMiner");
                    break;

                case NPCID.Tim:
                    TryDowned("rareEnemy", "tim");
                    break;

                case NPCID.DoctorBones:
                    TryDowned("rareEnemy", "doctorBones");
                    break;

                case NPCID.Mimic:
                    TryDowned("rareEnemy", "mimic");
                    break;

                case NPCID.WyvernHead:
                    TryDowned("rareEnemy", "wyvern");
                    break;

                case NPCID.RuneWizard:
                    TryDowned("rareEnemy", "runeWizard");
                    break;

                case NPCID.Nymph:
                    TryDowned("rareEnemy", "nymph");
                    break;

                case NPCID.Moth:
                    TryDowned("rareEnemy", "moth");
                    break;

                case NPCID.RainbowSlime:
                    TryDowned("rareEnemy", "rainbowSlime");
                    break;

                case NPCID.Paladin:
                    TryDowned("rareEnemy", "paladin");
                    break;

                case NPCID.Medusa:
                    TryDowned("rareEnemy", "medusa");
                    break;

                case NPCID.IceGolem:
                    TryDowned("rareEnemy", "iceGolem");
                    break;

                case NPCID.SandElemental:
                    TryDowned("rareEnemy", "sandElemental");
                    break;

                case NPCID.Nailhead:
                    TryDowned("rareEnemy", "nailhead");
                    break;

                case NPCID.Mothron:
                    TryDowned("rareEnemy", "mothron");
                    break;

                case NPCID.BigMimicCorruption:
                    TryDowned("rareEnemy", "mimicCorrupt");
                    break;

                case NPCID.BigMimicHallow:
                    TryDowned("rareEnemy", "mimicHallow");
                    break;

                case NPCID.BigMimicCrimson:
                    TryDowned("rareEnemy", "mimicCrimson");
                    break;

                case NPCID.BigMimicJungle:
                    TryDowned("rareEnemy", "mimicJungle");
                    break;

                case NPCID.GoblinSummoner:
                    TryDowned("rareEnemy", "goblinSummoner");
                    break;

                case NPCID.PirateShip:
                    TryDowned("rareEnemy", "flyingDutchman");
                    break;

                case NPCID.DungeonSlime:
                    TryDowned("rareEnemy", "dungeonSlime");
                    break;

                case NPCID.PirateCaptain:
                    TryDowned("rareEnemy", "pirateCaptain");
                    break;

                case NPCID.SkeletonSniper:
                case NPCID.TacticalSkeleton:
                case NPCID.SkeletonCommando:
                    TryDowned("rareEnemy", "skeletonGun");
                    break;

                case NPCID.Necromancer:
                case NPCID.NecromancerArmored:
                case NPCID.DiabolistRed:
                case NPCID.DiabolistWhite:
                case NPCID.RaggedCaster:
                case NPCID.RaggedCasterOpenCoat:
                    TryDowned("rareEnemy", "skeletonMage");
                    break;

                case NPCID.BoneLee:
                    TryDowned("rareEnemy", "boneLee");
                    break;

                case NPCID.HeadlessHorseman:
                    TryDowned("headlessHorseman");
                    break;

                default:
                    break;
            }

            if (Fargowiltas.ModRareEnemies.ContainsKey(npc.type))
            {
                TryDowned("rareEnemy", Fargowiltas.ModRareEnemies[npc.type]);
            }
        }

        public override bool CheckDead(NPC npc)
        {
            if (npc.type == NPCID.DD2Betsy && !PandoraActive)
            {
                Main.NewText("Betsy has been defeated!", 175, 75);
                FargoWorld.DownedBools["betsy"] = true;
                //DD2Event.DownedInvasionT3 = true;
            }

            if (npc.boss)
            {
                FargoWorld.DownedBools["boss"] = true;
            }

            return true;
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (GetInstance<FargoConfig>().RottenEggs && projectile.type == ProjectileID.RottenEgg && npc.townNPC)
            {
                damage *= 20;
            }
        }

        public override void OnChatButtonClicked(NPC npc, bool firstButton)
        {
            // No angler check enables luiafk compatibility
            if (GetInstance<FargoConfig>().AnglerQuestInstantReset && Main.anglerQuestFinished)
            {
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    Main.AnglerQuestSwap();
                }
                else if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    // Broadcast swap request to server
                    var netMessage = mod.GetPacket();
                    netMessage.Write((byte)3);
                    netMessage.Send();
                }
            }
        }

        private void SpawnBoss(NPC npc, int boss)
        {
            int spawn;

            if (SwarmActive)
            {
                if (npc.type == NPCID.WallofFlesh)
                {
                    NPC currentWoF = Main.npc[LastWoFIndex];
                    int startingPos = (int)currentWoF.position.X;
                    spawn = NPC.NewNPC(startingPos + (400 * WoFDirection), (int)currentWoF.position.Y, NPCID.WallofFlesh, 0);
                    if (spawn != Main.maxNPCs)
                    {
                        Main.npc[spawn].GetGlobalNPC<FargoGlobalNPC>().SwarmActive = true;
                        LastWoFIndex = spawn;
                    }
                }
                else
                {
                    spawn = NPC.NewNPC((int)npc.position.X + Main.rand.Next(-1000, 1000), (int)npc.position.Y + Main.rand.Next(-400, -100), boss);

                    if (spawn != Main.maxNPCs)
                    {
                        Main.npc[spawn].GetGlobalNPC<FargoGlobalNPC>().SwarmActive = true;
                        NetMessage.SendData(MessageID.SyncNPC, number: boss);
                    }
                }
            }
            else
            {
                // Pandora
                int random;

                do
                {
                    random = Main.rand.Next(Bosses);
                }
                while (NPC.CountNPCS(random) >= 4);

                spawn = NPC.NewNPC((int)npc.position.X + Main.rand.Next(-1000, 1000), (int)npc.position.Y + Main.rand.Next(-400, -100), random);
                if (spawn != Main.maxNPCs)
                {
                    Main.npc[spawn].GetGlobalNPC<FargoGlobalNPC>().PandoraActive = true;
                    NetMessage.SendData(MessageID.SyncNPC, number: random);
                }
            }
        }

        private void Swarm(NPC npc, int boss, int minion, int bossbag, int trophy, string reward)
        {
            if (bossbag >= 0 && bossbag != ItemID.DefenderMedal)
            {
                npc.DropItemInstanced(npc.Center, npc.Size, bossbag);
            }
            else if (bossbag >= 0 && bossbag == ItemID.DefenderMedal)
            {
                npc.DropItemInstanced(npc.Center, npc.Size, bossbag, 5);
            }

            int count = 0;
            if (SwarmActive)
            {
                count = NPC.CountNPCS(boss) - 1; // Since this one is about to be dead
            }
            else
            {
                // Pandora
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Array.IndexOf(Bosses, Main.npc[i].type) > -1)
                    {
                        count++;
                    }
                }
            }

            int missing = Fargowiltas.SwarmSpawned - count;

            Fargowiltas.SwarmKills++;

            // Drop swarm reward every 100 kills
            if (Fargowiltas.SwarmKills % 100 == 0 && !string.IsNullOrEmpty(reward))
            {
                Item.NewItem(npc.Hitbox, mod.ItemType(reward));
            }

            //drop trphy every 10 killa
            if (Fargowiltas.SwarmKills % 10 == 0 && trophy != -1)
            {
                Item.NewItem(npc.Hitbox, trophy);
            }

            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Killed: " + Fargowiltas.SwarmKills), new Color(206, 12, 15));
                NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Total: " + Fargowiltas.SwarmTotal), new Color(206, 12, 15));
            }
            else
            {
                Main.NewText("Killed: " + Fargowiltas.SwarmKills, new Color(206, 12, 15));
                Main.NewText("Total: " + Fargowiltas.SwarmTotal, new Color(206, 12, 15));
            }

            if (minion != -1 && NPC.CountNPCS(minion) >= Fargowiltas.SwarmSpawned)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == minion)
                    {
                        Main.npc[i].StrikeNPCNoInteraction(Main.npc[i].lifeMax, 0f, -Main.npc[i].direction, true);
                    }
                }
            }

            // If theres still more to spawn
            if (Fargowiltas.SwarmKills <= Fargowiltas.SwarmTotal - Fargowiltas.SwarmSpawned)
            {
                int spawned = 0;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    // Count NPCs
                    int num = 0;
                    for (int j = 0; j < Main.maxNPCs; j++)
                    {
                        if (Main.npc[j].active)
                        {
                            num++;
                        }
                    }

                    // Kill a minion and spawn boss if too many npcs
                    if (num >= Main.maxNPCs)
                    {
                        if (SwarmActive && minion > 0 && i < Main.maxNPCs - 1)
                        {
                            if (Main.npc[i].type == minion)
                            {
                                Main.npc[i].StrikeNPCNoInteraction(Main.npc[i].lifeMax, 0f, -Main.npc[i].direction, true);
                            }
                        }
                        else
                        {
                            // Pandora
                            if (Array.IndexOf(Bosses, Main.npc[i].type) == -1 && !Main.npc[i].boss)
                            {
                                Main.npc[i].StrikeNPCNoInteraction(Main.npc[i].lifeMax, 0f, -Main.npc[i].direction, true);
                            }
                        }
                    }

                    SpawnBoss(npc, boss);
                    spawned++;

                    if (spawned <= missing)
                    {
                        continue;
                    }

                    break;
                }
            }
            else if (Fargowiltas.SwarmKills >= Fargowiltas.SwarmTotal)
            {
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("The swarm has been defeated!"), new Color(206, 12, 15));
                }
                else
                {
                    Main.NewText("The swarm has been defeated!", new Color(206, 12, 15));
                }

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC kill = Main.npc[i];
                    if (kill.active && !kill.friendly && kill.type != NPCID.LunarTowerNebula && kill.type != NPCID.LunarTowerSolar && kill.type != NPCID.LunarTowerStardust && kill.type != NPCID.LunarTowerVortex)
                    {
                        Main.npc[i].GetGlobalNPC<FargoGlobalNPC>().NoLoot = true;
                        Main.npc[i].StrikeNPCNoInteraction(Main.npc[i].lifeMax, 0f, -Main.npc[i].direction, true);
                    }
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Fargowiltas.SwarmActive = false;
                    LastWoFIndex = -1;
                    WoFDirection = 0;
                    if (Main.netMode == NetmodeID.Server)
                        NetMessage.SendData(MessageID.WorldData);
                }
            }

            // Make sure theres enough left to beat it
            else
            {
                // Spawn more if needed
                if (count >= Fargowiltas.SwarmSpawned || Fargowiltas.SwarmTotal <= 20)
                {
                    return;
                }

                int extraSpawn = 0;

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    // Count NPCs
                    int num = 0;
                    for (int j = 0; j < Main.maxNPCs; j++)
                    {
                        if (Main.npc[j].active)
                        {
                            num++;
                        }
                    }

                    // Kill a minion and spawn boss if too many NPCs
                    if (num >= Main.maxNPCs)
                    {
                        if (SwarmActive && minion > 0 && i < Main.maxNPCs - 1)
                        {
                            if (Main.npc[i].type == minion)
                            {
                                Main.npc[i].StrikeNPCNoInteraction(Main.npc[i].lifeMax, 0f, -Main.npc[i].direction, true);
                            }
                        }
                        else
                        {
                            // Pandora
                            if (Array.IndexOf(Bosses, Main.npc[i].type) == -1 && !Main.npc[i].boss)
                            {
                                Main.npc[i].StrikeNPCNoInteraction(Main.npc[i].lifeMax, 0f, -Main.npc[i].direction, true);
                            }
                        }
                    }

                    SpawnBoss(npc, boss);
                    extraSpawn++;

                    if (extraSpawn < 5)
                    {
                        continue;
                    }

                    break;
                }
            }
        }

        public static void SpawnWalls(Player player)
        {
            int startingPos;

            if (LastWoFIndex == -1)
            {
                startingPos = (int)player.position.X;
            }
            else
            {
                startingPos = (int)Main.npc[LastWoFIndex].position.X;
            }

            Vector2 pos = player.position;

            if (WoFDirection == 0)
            {
                //1 is to the right, -1 is left
                WoFDirection = ((player.position.X / 16) > (Main.maxTilesX / 2)) ? 1 : -1;
            }

            int wof = NPC.NewNPC(startingPos + (400 * WoFDirection), (int)pos.Y, NPCID.WallofFlesh, 0);
            Main.npc[wof].GetGlobalNPC<FargoGlobalNPC>().SwarmActive = true;

            LastWoFIndex = wof;
        }

        public static bool SpecificBossIsAlive(ref int bossID, int bossType)
        {
            if (bossID != -1)
            {
                if (Main.npc[bossID].active && Main.npc[bossID].type == bossType)
                {
                    return true;
                }
                else
                {
                    bossID = -1;
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static int boss = -1;

        public static bool AnyBossAlive()
        {
            if (boss == -1)
                return false;

            NPC npc = Main.npc[boss];

            if (npc.active && npc.type != NPCID.MartianSaucerCore && (npc.boss || npc.type == NPCID.EaterofWorldsHead))
                return true;
            boss = -1;
            return false;
        }
    }
}