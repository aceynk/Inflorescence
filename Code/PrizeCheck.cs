using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using Object = System.Object;

namespace Inflorescence.Code;

public class Helper
{
    public static void Log(string v) {ModEntry.Log(v);}
    public static List<string> FlowerCache = ModEntry.FlowerCache;
    public static readonly string ModDataPrizeCheckKey = "aceynk.Inflorescence.FarmPrizeCheck";
    public static readonly string ModDataPrizeBonusKey = "aceynk.Inflorescence.PrizeBonus";
    public static readonly string ModDataLastScoreKey = "aceynk.Inflorescence.LastScore";
    public static List<int> PrizeClassGate = new() {420, 140, 70};
    public static readonly List<string> InflorescenceMailKeys = new()
    {
        "Inflorescence_MailGold",
        "Inflorescence_MailSilver",
        "Inflorescence_MailBronze"
    };

    public static void Ensure_modData(Farm obj, string key)
    {
        if (!obj.modData.ContainsKey(key))
        {
            obj.modData[key] = "0";
        }
    }

    public static void Ensure_modData(Farmer obj, string key)
    {
        if (!obj.modData.ContainsKey(key))
        {
            obj.modData[key] = "0";
        }
    }

    public static void CachePrizeGate()
    {
        // make the bounds a config option later maybe?
        PrizeClassGate = new List<int> {
            420 + 7 * Random.Shared.Next(40),
            140 + 7 * Random.Shared.Next(30),
            70 + 7 * Random.Shared.Next(8)
        };
    }

    public static int PrizeClass(int score)
    {
        if (score > PrizeClassGate[0]) return 1;
        if (score > PrizeClassGate[1]) return 2;
        return score > PrizeClassGate[2] ? 3 : 4;
    }

    public static int BonusFunc(int bonusScore)
    {
        return (int)Math.Max(0, Math.Floor(0.5 * Math.Sqrt(bonusScore)));
    }
}

public class ContentPrizeCheck
{
    public static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        Farm thisFarm = Game1.getFarm();
        Helper.Ensure_modData(thisFarm, Helper.ModDataPrizeCheckKey);
        Helper.Ensure_modData(Game1.player, Helper.ModDataPrizeBonusKey);
        Helper.Ensure_modData(Game1.player, Helper.ModDataLastScoreKey);
        
        CountMatureFlowers();
        
        int score = int.Parse(thisFarm.modData[Helper.ModDataPrizeCheckKey]);
        
        Helper.Log("Current Prize Score: " + score);
        
        if (Game1.Date.DayOfMonth % 7 == 0)
        {
            Helper.CachePrizeGate();
            
            int prizeLevel = Helper.PrizeClass(score);

            if (prizeLevel == 4) return;
            
            Helper.Log((score - Helper.PrizeClassGate[prizeLevel - 1]).ToString());
            Helper.Log(Helper.BonusFunc(score - Helper.PrizeClassGate[prizeLevel - 1]).ToString());

            int bonus = Helper.BonusFunc(score - Helper.PrizeClassGate[prizeLevel - 1]);
            
            Helper.Log("Added Contest Mail");
            Game1.player.mailbox.Add(Helper.InflorescenceMailKeys[prizeLevel - 1]);

            Game1.player.modData[Helper.ModDataPrizeBonusKey] = bonus.ToString();
            thisFarm.modData[Helper.ModDataPrizeCheckKey] = "0";
        }

        if (!Game1.player.mailReceived.Contains("Inflorescence_MailInitiation"))
        {
            Game1.addMailForTomorrow("Inflorescence_MailInitiation");
        }
        
        Game1.player.modData[Helper.ModDataLastScoreKey] = score.ToString();
    }

    public static void CountMatureFlowers()
    {
        Farm thisFarm = Game1.getFarm();

        foreach (Vector2 key in thisFarm.terrainFeatures.Keys)
        {
            // Some logic taken from StardewValley.Locations.IslandFarmCave.OnRequestGourmandClick
            if (thisFarm.terrainFeatures[key] is not HoeDirt dirt || dirt.crop == null) continue;
            
            bool harvestable = dirt.crop.currentPhase.Value >= dirt.crop.phaseDays.Count - 1
                               && (!dirt.crop.fullyGrown.Value
                                   || dirt.crop.dayOfCurrentPhase.Value <= 0);
            
            if (!harvestable) continue;

            if (!Helper.FlowerCache.Contains(dirt.crop.indexOfHarvest.Value)) continue;
            
            Helper.Ensure_modData(thisFarm, Helper.ModDataPrizeCheckKey);
        
            int prizeScore = int.Parse(thisFarm.modData[Helper.ModDataPrizeCheckKey]);

            thisFarm.modData[Helper.ModDataPrizeCheckKey] = (prizeScore + 1).ToString();
        }
    }
}