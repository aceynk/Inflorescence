using HarmonyLib;
using Inflorescence.Code;
using Leclair.Stardew.BetterCrafting;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using Object = StardewValley.Object;

namespace Inflorescence;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

public class ModEntry : Mod
{
    public static void Log(string v, LogLevel logLevel = LogLevel.Debug)
    {
        _log.Log(v, logLevel);
    }
    
    public static List<string> GetItemsByContextTag(string contextTag)
    {
        if (Game1.objectData is null) return new List<string>();
        
        return Game1.objectData.Where(
            v => (v.Value.ContextTags ?? new List<string>()).Contains(contextTag)
        ).Select(
            v => v.Key
        ).ToList();
    }

    public static List<string> GetItemsByCategory(int category)
    {
        if (Game1.objectData is null) return new List<string>();

        List<string> thisOut = Game1.objectData.Where(
            v => v.Value.Category == category
        ).Select(
            v => v.Key
        ).ToList();

        return thisOut;
    }

    public static IMonitor _log = null!;
    public static List<string> FlowerCache = new();
    public static IManifest Manifest;
    public static IModHelper thisHelper;
    public static readonly string ContentPackId = "aceynk.inflorescencecontent";
    public static IInflorescenceApi api = new InflorescenceApi();
    public static IBetterCrafting? bcApi;
    
    public override void Entry(IModHelper helper)
    {
        _log = Monitor;

        Manifest = ModManifest;

        thisHelper = Helper;

        Helper.Events.GameLoop.DayStarted += ContentPrizeCheck.OnDayStarted;
        Helper.Events.Content.AssetRequested += OnAssetRequested;
        Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        Helper.Events.GameLoop.GameLaunched += OnGameLaunched;

        helper.ConsoleCommands.Add("inflor_setscore",
            "Sets the player's Inflorescence score.\n\nUsage: inflor_setscore <value>\n- value: the integer amount.",
            SetScore);
        
        var harmony = new Harmony(Manifest.UniqueID);
        harmony.PatchAll();
    }

    public override object GetApi()
    {
        return new InflorescenceApi();
    }

    private static void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        if (thisHelper.ModRegistry.IsLoaded("leclair.bettercrafting"))
        {
            bcApi = thisHelper.ModRegistry.GetApi<IBetterCrafting>("leclair.bettercrafting");
            bcApi!.AddRecipeProvider(new Recipes());
        }
    }

    private void SetScore(string command, string[] args)
    {
        api.InflorescenceScore = int.Parse(args[0]);
        api.InflorescenceLast = int.Parse(args[0]);
        api.InflorescenceBonus = Code.Helper.BonusFunc(api.InflorescenceScore);
        
        Log("Successfully set your Prize Score to " + args[0], LogLevel.Info);
    }

    public static void DoFlowerCache()
    {
        FlowerCache = GetItemsByContextTag("flower_item")
            .Concat(api.FlowerCacheInclude)
            .Concat(GetItemsByCategory(Object.flowersCategory)).ToHashSet().ToList();
        
        foreach (string v in FlowerCache)
        {
            Log(v);
        }
        
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
        
        api.InflorescenceBonus = 0;
    }

    private static void AddInflorContextTags(IAssetData obj)
    {
        IDictionary<string, ObjectData> asDict = obj.AsDictionary<string, ObjectData>().Data;
        
        //Log("inflor context");
        
        if (FlowerCache.Count == 0) DoFlowerCache();

        foreach (var flower in FlowerCache)
        {
            //Log(flower);
            if (!asDict.ContainsKey(flower)) continue;
            asDict[flower].ContextTags.Add("inflor_flower_item");
        }
    }

    private static void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo("Data/Mail")) {
            e.Edit(DataMailAction, AssetEditPriority.Late + 1);
        }

        if (e.Name.IsEquivalentTo("Data/Objects"))
        {
            e.Edit(AddInflorContextTags, AssetEditPriority.Late + 1);
        }
    }
}