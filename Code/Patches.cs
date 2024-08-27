using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using xTile.Dimensions;
using Object = StardewValley.Object;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Inflorescence.Code;

public class PatchHelper
{
    public static DayOfWeek Inflor_Shop_Day = DayOfWeek.Tuesday;
    public static Point Inflor_Shop_Origin = new(15, 8);
    internal static readonly Texture2D Inflor_Shop_Texture =
        Game1.content.Load<Texture2D>(ModEntry.ContentPackId + "/Shop");
    public static Rectangle Inflor_Shop_Bounds = new (960, 640, 192, 48);

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
        if (__instance.ItemId != ModEntry.ContentPackId + "_ScoreRadio") return;
        
        // taken from decompiled StardewValley.Object.performUseAction
        bool normalGameplay = !Game1.eventUp && !Game1.isFestival() && !Game1.fadeToBlack && !Game1.player.swimming && !Game1.player.bathingClothes && !Game1.player.onBridge.Value;

        if (!normalGameplay) return;
        
        Farm thisFarm = Game1.getFarm();
        Helper.Ensure_modData(thisFarm, Helper.ModDataPrizeCheckKey);
        
        int score = int.Parse(thisFarm.modData[Helper.ModDataPrizeCheckKey]);
        HUDMessage phoneMessage = new HUDMessage("Your current Inflorescence score is " + score);

        phoneMessage.type = ModEntry.Manifest.UniqueID + "_RadioMsg";
        phoneMessage.messageSubject = ItemRegistry.Create(ModEntry.ContentPackId + "_ScoreRadio");
        
        Game1.addHUDMessage(phoneMessage);
    }
}