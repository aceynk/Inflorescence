namespace Inflorescence.Code;

public interface IInflorescenceApi
{
    /// <summary>
    /// Refreshes the internal cache of flower ids.
    /// </summary>
    public void InvalidateFlowerCache();

    /// <summary>
    /// Add items here not usually included in the flower cache.
    /// Invalidate the cache after editing.
    /// </summary>
    public List<string> FlowerCacheInclude { get; }

    /// <summary>
    /// Int field that allows getting & setting. Accesses value from Farm modData.
    /// This field is for the internal score used for placement in the Inflorescence competition.
    /// </summary>
    public int InflorescenceScore { get; set; }
    
    /// <summary>
    /// Int field that allows getting & setting. Accesses value from Player modData.
    /// This field is for the internal bonus used for extra bouquets in the Inflorescence competition rewards.
    /// </summary>
    public int InflorescenceBonus { get; set; }
    
    /// <summary>
    /// Int field that allows getting & setting. Accesses value from Player modData.
    /// This field is for the internal staggered score used for representing the final contest score in letters.
    /// </summary>
    public int InflorescenceLast { get; set; }
}