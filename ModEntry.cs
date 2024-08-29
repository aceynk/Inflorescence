using HarmonyLib;
using Inflorescence.Code;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Inflorescence;

public class ModEntry : Mod
{
    public static void Log(string v, LogLevel logLevel = LogLevel.Debug)
    {
        _log.Log(v, logLevel);
    }
    
    public static List<string> GetItemsByContextTag(string contextTag)
    {
        return Game1.objectData.Where(
            v => (v.Value.ContextTags ?? new List<string>()).Contains(contextTag)
        ).Select(
            v => v.Key
        ).ToList();
    }

    public static IMonitor _log = null!;
    public static List<string> FlowerCache = new();
    public static IManifest Manifest;
    public readonly static string ContentPackId = "aceynk.inflorescencecontent";
    public static IInflorescenceApi api = new InflorescenceApi();
    
    public override void Entry(IModHelper helper)
    {
        _log = Monitor;

        Manifest = ModManifest;

        Helper.Events.GameLoop.DayStarted += ContentPrizeCheck.OnDayStarted;
        Helper.Events.Content.AssetRequested += OnAssetRequested;
        Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        
        helper.ConsoleCommands.Add("inflor_setscore", "Sets the player's Inflorescence score.\n\nUsage: inflor_setscore <value>\n- value: the integer amount.", SetScore);
        
        var harmony = new Harmony(Manifest.UniqueID);
        harmony.PatchAll();
    }

    public override object GetApi()
    {
        return new InflorescenceApi();
    }

    private void SetScore(string command, string[] args)
    {
        api.InflorescenceScore = int.Parse(args[0]);
        api.InflorescenceLast = int.Parse(args[0]);
        
        Log("Successfully set your Prize Score to " + args[0], LogLevel.Info);
    }

    public static void DoFlowerCache()
    {
        FlowerCache = GetItemsByContextTag("flower_item")
            .Concat(api.FlowerCacheInclude).ToList();
    }

    private static void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        DoFlowerCache();
    }

    private static void DataMailAction(IAssetData obj)
    {
        IDictionary<string, string> asDict = obj.AsDictionary<string, string>().Data;
        
        foreach (var key in Code.Helper.InflorescenceMailKeys)
        {
            if (!asDict.ContainsKey(key))
            {
                Log("Failed to edit a mail item. key = " + key);
                continue;
            }

            string[] split = asDict[key].Split("[");
            string currency = ContentPackId + "_" + key[18..] + "Bouquet";

            int bonus = api.InflorescenceBonus;
            int lastScore = api.InflorescenceLast;
            List<string> prizeClassNames = new() { "Gold", "Silver", "Bronze" };

            asDict[key] = split[0] + "^^Your Score: " + lastScore + " (Bonus: " + bonus + ")^^Prize Gates:" + 
                          Code.Helper.PrizeClassGate.Select(
                              v => "^" + prizeClassNames[Code.Helper.PrizeClassGate.IndexOf(v)] + ": " + v
                              ).Join(delimiter: "") 
                          + "%item object " + currency + " " + (5 + bonus) + " %%[" + split[1];
        }
        
        Game1.player.modData[Code.Helper.ModDataPrizeBonusKey] = "0";
    }

    private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo("Data/Mail")) {
            e.Edit(DataMailAction, AssetEditPriority.Late + 1);
        }
    }
}