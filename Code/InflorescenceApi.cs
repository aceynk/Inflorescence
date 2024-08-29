using StardewValley;

namespace Inflorescence.Code;

public class InflorescenceApi : IInflorescenceApi
{
    /// <inheritdoc/>
    public void InvalidateFlowerCache()
    {
        ModEntry.DoFlowerCache();
    }

    /// <inheritdoc/>
    public List<string> FlowerCacheInclude { get; } = new();
    
    /// <inheritdoc/>
    public int InflorescenceScore {
        get
        {
            Farm thisFarm = Game1.getFarm();
            Helper.Ensure_modData(thisFarm, Helper.ModDataPrizeCheckKey);

            int score = int.Parse(thisFarm.modData[Helper.ModDataPrizeCheckKey]);

            return score;
        }
        set
        {
            Farm thisFarm = Game1.getFarm();
            thisFarm.modData[Helper.ModDataPrizeCheckKey] = value.ToString();
        }
    }

    /// <inheritdoc/>
    public int InflorescenceBonus
    {
        get
        {
            Farmer thisPlayer = Game1.player;
            Helper.Ensure_modData(thisPlayer, Helper.ModDataPrizeBonusKey);

            int bonus = int.Parse(thisPlayer.modData[Helper.ModDataPrizeBonusKey]);

            return bonus;
        }
        set
        {
            Farmer thisPlayer = Game1.player;
            thisPlayer.modData[Helper.ModDataPrizeBonusKey] = value.ToString();
        }
    }

    /// <inheritdoc/>
    public int InflorescenceLast
    {
        get
        {
            Farmer thisPlayer = Game1.player;
            Helper.Ensure_modData(thisPlayer, Helper.ModDataPrizeBonusKey);

            int last = int.Parse(thisPlayer.modData[Helper.ModDataLastScoreKey]);

            return last;
        }
        set
        {
            Farmer thisPlayer = Game1.player;
            thisPlayer.modData[Helper.ModDataLastScoreKey] = value.ToString();
        }
    }
}