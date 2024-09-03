using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Inflorescence.Code;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

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

    public static IInflorescenceApi api = ModEntry.api;

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

    public static int BonusFunc(int score)
    {
        return (int)Math.Max(0, Math.Floor(0.5 * Math.Sqrt(score)));
    }
}

public class ContentPrizeCheck
{
    public static void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        int oldScore = Helper.api.InflorescenceScore;
        
        CountMatureFlowers();
        
        int score = Helper.api.InflorescenceScore;
        
        if (Game1.Date.DayOfMonth % 7 == 0)
        {
            Helper.CachePrizeGate();
            
            int prizeLevel = Helper.PrizeClass(score);

            if (prizeLevel == 4) goto Next_Logic_NewDay;

            int bonus = Helper.BonusFunc(score - Helper.PrizeClassGate[prizeLevel - 1]);
            
            Game1.player.mailbox.Add(Helper.InflorescenceMailKeys[prizeLevel - 1]);

            Helper.api.InflorescenceBonus = bonus;
            Helper.api.InflorescenceScore = 0;
        }

        Next_Logic_NewDay:

        if (!Game1.player.mailReceived.Contains("Inflorescence_MailInitiation"))
        {
            Game1.addMailForTomorrow("Inflorescence_MailInitiation");
        }
        
        Helper.Log("Overnight Prize Score Change (" + oldScore + " -> " + Helper.api.InflorescenceScore + ")");
        
        Helper.api.InflorescenceLast = score;
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
        
            int prizeScore = Helper.api.InflorescenceScore;

            Helper.api.InflorescenceScore = prizeScore + 1;
        }
    }
}