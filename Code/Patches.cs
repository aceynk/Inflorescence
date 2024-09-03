using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Inventories;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using xTile.Dimensions;
using Object = StardewValley.Object;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Inflorescence.Code;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

public class PatchHelper
{
    public static DayOfWeek Inflor_Shop_Day = DayOfWeek.Tuesday;
    public static Point Inflor_Shop_Origin = new(15, 8);
    internal static readonly Texture2D Inflor_Shop_Texture =
        Game1.content.Load<Texture2D>(ModEntry.ContentPackId + "/Shop");
    public static Rectangle Inflor_Shop_Bounds = new (960, 640, 192, 48);
    public static List<string> SpecialCrafting = new()
    {
        ModEntry.ContentPackId + "_Bouquet"
    };
    
    public static bool isValidFlowerTile(HoeDirt dirt, bool isHarvestable = false)
    {
        Crop thisCrop = dirt.crop;

        if (thisCrop is null) return false;
        
        bool harvestable = thisCrop.currentPhase.Value >= thisCrop.phaseDays.Count - 1
                           && (!thisCrop.fullyGrown.Value
                               || thisCrop.dayOfCurrentPhase.Value <= 0);
        
        if (isHarvestable != harvestable) return false;
        
        if (!Helper.FlowerCache.Contains(thisCrop.indexOfHarvest.Value)) return false;

        return true;
    }
    
    public static bool isValidFlowerTile(TerrainFeature terrainFeature, bool isHarvestable = false)
    {
        if (terrainFeature is not HoeDirt dirt || dirt.crop == null) return false;

        return isValidFlowerTile(dirt, isHarvestable);
    }

    public static bool isValidFlowerTile(GameLocation location, int x, int y, bool isHarvestable = false)
    {
        Vector2 position = new Vector2(x, y);
        return isValidFlowerTile(location.terrainFeatures[position], isHarvestable);
    }

    public static List<Tuple<string, IInventory>> FindFlowers(List<IInventory> inventories, int number)
    {
        if (inventories is null)
        {
            Helper.Log("null inventories");
            return new();
        }
        List<string> usedIds = new();
        List<Tuple<string, IInventory>> outList = new();
        
        foreach (var coll in inventories)
        {
            if (!coll.HasAny()) continue;

            foreach (var item in coll.GetRange(0, coll.Count))
            {
                if (item is null) continue;
                
                if (ModEntry.FlowerCache.Contains(item.ItemId) && !usedIds.Contains(item.ItemId))
                {
                    usedIds.Add(item.ItemId);
                    outList.Add(new Tuple<string, IInventory>(item.ItemId, coll));
                }

                if (outList.Count >= number) return outList;
            }
        }

        return outList;
    }

    public static void RemoveFlowers(List<Tuple<string, IInventory>> found)
    {
        foreach (var idCollTuple in found)
        {
            idCollTuple.Item2.ReduceId(idCollTuple.Item1, 1);
        }
    }

    public static string FormatColor(Color color)
    {
        return color.R + "," + color.G + "," + color.B;
    }
}

[HarmonyPatch(typeof(Forest), nameof(Forest.draw))]
public class SDV_Loc_Forest_draw
{
    public static void Postfix(SpriteBatch spriteBatch)
    {
        if (Game1.Date.DayOfWeek != PatchHelper.Inflor_Shop_Day) return;
        
        spriteBatch.Draw(
            PatchHelper.Inflor_Shop_Texture, 
            Game1.GlobalToLocal(new Vector2(PatchHelper.Inflor_Shop_Origin.X * 64, PatchHelper.Inflor_Shop_Origin.Y * 64 - 32)),
            new Rectangle(0, 0, 48, 50),
            Color.White,
            0f,
            Vector2.Zero,
            4f,
            SpriteEffects.None,
            0.065f
        );
    }
}

[HarmonyPatch(typeof(Forest), nameof(Forest.isCollidingPosition))]
public class SDV_Loc_Forest_isCollidingPosition
{
    public static bool Prefix(
        Rectangle position,
        xTile.Dimensions.Rectangle viewport,
        bool isFarmer,
        int damagesFarmer,
        bool glider,
        Character character,
        bool pathfinding,
        Forest __instance,
        ref bool __result,
        bool projectile = false,
        bool ignoreCharacterRequirement = false,
        bool skipCollisionEffects = false
        )
    {
        if (PatchHelper.Inflor_Shop_Bounds == null) return true;
        
        if (Game1.Date.DayOfWeek != PatchHelper.Inflor_Shop_Day) return true;
            
        if (position.Intersects(PatchHelper.Inflor_Shop_Bounds))
        {
            __result = true;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(Forest), nameof(Forest.checkAction))]
public class SDV_Loc_Forest_checkAction
{
    public static bool Prefix(Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who, ref bool __result)
    {
        Point origin = PatchHelper.Inflor_Shop_Origin;
        
        if (Game1.Date.DayOfWeek != PatchHelper.Inflor_Shop_Day) return true;

        if (origin.X <= tileLocation.X && tileLocation.X <= origin.X + 2 && tileLocation.Y == origin.Y + 2)
        {
            Utility.TryOpenShopMenu(ModEntry.ContentPackId + "_Inflor_Shop", null, playOpenSound: true);
            __result = true;
        }
        
        return true;
    }
}

[HarmonyPatch(typeof(HoeDirt), nameof(HoeDirt.CanApplyFertilizer))]
public class SDV_TerrainFeature_HoeDirt_CanApplyFertilizer
{
    public static bool Prefix(string fertilizerId, HoeDirt __instance, ref bool __result)
    {
        if (fertilizerId != ModEntry.ContentPackId + "_PowerGro") return true;

        if (PatchHelper.isValidFlowerTile(__instance))
        {
            __result = true;
            return false;
        }

        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(HoeDirt), nameof(HoeDirt.plant))]
public class SDV_TerrainFeature_HoeDirt_plant
{
    public static bool Prefix(string itemId, Farmer who, bool isFertilizer, HoeDirt __instance, ref bool __result)
    {
        if (itemId != ModEntry.ContentPackId + "_PowerGro") return true;

        if (!PatchHelper.isValidFlowerTile(__instance)) return true;
        
        Game1.player.reduceActiveItemByOne();
        
        __instance.Location.playSound("dirtyHit");
        __result = false;
        
        __instance.crop.growCompletely();
        return false;
    }
}

[HarmonyPatch(typeof(Object), nameof(Object.performUseAction))]
public class SDV_Object_performUseAction
{
    public static void Prefix(GameLocation location, Object __instance)
    {
        List<string> processIds = new() { ModEntry.ContentPackId + "_ScoreRadio", ModEntry.ContentPackId + "_InformantEarpiece" };
        
        if (!processIds.Contains(__instance.ItemId)) return;
        
        // taken from decompiled StardewValley.Object.performUseAction
        bool normalGameplay = !Game1.eventUp && !Game1.isFestival() && !Game1.fadeToBlack && !Game1.player.swimming && !Game1.player.bathingClothes && !Game1.player.onBridge.Value;

        if (!normalGameplay) return;
        
        int score = Helper.api.InflorescenceScore;

        if (__instance.ItemId == processIds[0])
        {
            HUDMessage phoneMessage = new HUDMessage("Your current Inflorescence score is " + score);

            phoneMessage.type = ModEntry.Manifest.UniqueID + "_RadioMsg" + score;
            phoneMessage.messageSubject = ItemRegistry.Create(ModEntry.ContentPackId + "_ScoreRadio");
        
            Game1.addHUDMessage(phoneMessage);
        }
        else if (__instance.ItemId == processIds[1])
        {
            int gate = Helper.PrizeClass(score);
            HUDMessage earpieceMessage = new HUDMessage("All you hear is static...");
            string uiItem = ModEntry.ContentPackId + "_InformantEarpiece";

            switch (gate)
            {
                case 1:
                    earpieceMessage = new HUDMessage("You're one of the best farms around! You'll earn Gold Bouquets.");
                    uiItem = ModEntry.ContentPackId + "_GoldBouquet";
                    break;
                case 2:
                    earpieceMessage = new HUDMessage("You're doing incredible! You'll earn Silver Bouquets.");
                    uiItem = ModEntry.ContentPackId + "_SilverBouquet";
                    break;
                case 3:
                    earpieceMessage = new HUDMessage("You're in the runnings! You'll earn Bronze Bouquets.");
                    uiItem = ModEntry.ContentPackId + "_BronzeBouquet";
                    break;
                case 4:
                    earpieceMessage = new HUDMessage("You're not even in the runnings! You won't win anything...");
                    break;
            }
            
            earpieceMessage.type = ModEntry.Manifest.UniqueID + "_EarpieceMessage" + gate;
            earpieceMessage.messageSubject = ItemRegistry.Create(uiItem);
            
            Game1.addHUDMessage(earpieceMessage);
        }
    }
}

/*
// Private method. sorry for no nameof().
[HarmonyPatch(typeof(CraftingPage), "clickCraftingRecipe")]
public class SDV_Menus_CraftingPage_clickCraftingRecipe
{
    public static bool Prefix(ClickableTextureComponent c, CraftingPage __instance, bool playSound = true)
    {
        CraftingRecipe recipe = __instance.pagesOfCraftingRecipes[__instance.currentCraftingPage][c];

        if (!PatchHelper.SpecialCrafting.Contains(recipe.name)) return true;

        // currently only using this for bouquets

        List<Tuple<string, IInventory>> flowers = new();
        
        Helper.Log("inventory");

        flowers = flowers.Concat(PatchHelper.FindFlowers(
            new List<IInventory> {(IInventory)__instance.inventory.actualInventory},
            3
        )).ToList();

        if (flowers.Count != 3)
        {
            Helper.Log("materials");
            
            flowers = flowers.Concat(PatchHelper.FindFlowers(
                __instance._materialContainers,
                3 - flowers.Count
            )).ToList();
        }

        if (flowers.Count != 3) return false;
        
        Helper.Log("3 flowers");

        List<string> usedFlowers = flowers.Select(v => v.Item1).ToList();
        List<Color> colors = usedFlowers.Select(
            v => ItemContextTagManager.GetColorFromTags(new Object(v, 1)) ?? Color.White
        ).ToList();
        Item item = recipe.createItem();

        //if (item is not ColoredObject thisObj) return false;
        
        Helper.Log("ColoredItem");

        ColoredObject thisObj = new ColoredObject(item.ItemId, item.Stack, colors[0]);
        thisObj.modData["selph.ExtraMachineConfig.ExtraColor.1"] = PatchHelper.FormatColor(colors[1]);
        thisObj.modData["selph.ExtraMachineConfig.ExtraColor.2"] = PatchHelper.FormatColor(colors[2]);

        thisObj.hovering = true;

        Item heldItem = __instance.heldItem;

        thisObj.Price += usedFlowers.Select(v => new Object(v, 1).Price).Sum();

        if (__instance.heldItem is null)
        {
            PatchHelper.RemoveFlowers(flowers);
            __instance.heldItem = thisObj;
        }
        else
        {
            // Taken from decompiled StardewValley.Menus.CraftingPage.clickCraftingRecipe
            bool stackCondition = heldItem.Name != thisObj.Name ||
                                  !heldItem.getOne().canStackWith(thisObj.getOne()) ||
                                  heldItem.Stack + recipe.numberProducedPerCraft - 1 >=
                                  heldItem.maximumStackSize();
            
            if (stackCondition) return false;
            
            if (heldItem is not ColoredObject chItem) return false;

            if (thisObj.color != chItem.color) return false;
            if (thisObj.modData["selph.ExtraMachineConfig.ExtraColor.1"] !=
                chItem.modData["selph.ExtraMachineConfig.ExtraColor.1"]) return false;
            if (thisObj.modData["selph.ExtraMachineConfig.ExtraColor.2"] !=
                chItem.modData["selph.ExtraMachineConfig.ExtraColor.2"]) return false;

            heldItem.Stack++;
            
            PatchHelper.RemoveFlowers(flowers);
        }
        
        if (playSound)
        {
            Game1.playSound("coin");
        }
        
        Game1.stats.checkForCraftingAchievements();
        
        return false;
    }
}
*/