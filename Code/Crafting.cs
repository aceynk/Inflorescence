using Leclair.Stardew.BetterCrafting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.Menus;
using StardewValley.Objects;
using static Inflorescence.ModEntry;
using Object = StardewValley.Object;

namespace Inflorescence.Code;

public class Recipes : IRecipeProvider
{
    public int RecipePriority => 1;

    public IRecipe? GetRecipe(CraftingRecipe recipe)
    {
        if (recipe.isCookingRecipe) return null;
        
        if (recipe.name == ContentPackId + "_Bouquet")
        {
            CraftingInit crafting = new();

            return crafting.AddRecipes()[0];
        }
        
        return null;
    }

    public bool CacheAdditionalRecipes => false;

    public IEnumerable<IRecipe>? GetAdditionalRecipes(bool cooking)
    {
        if (cooking) return null;

        CraftingInit crafting = new();

        return crafting.AddRecipes();
    }
}

public class CraftingInit
{
    private readonly List<string> _usedList = new();

    public static string BouquetName(HashSet<string> flowers)
    {
        List<string> flowersList = flowers.ToList();
        
        switch (flowersList.Count)
        {
            case 1:
                return $"{flowersList[0]} Bouquet";
            case 2:
                return $"{flowersList[0]} and {flowersList[1]} Bouquet";
            case 3:
                return $"{flowersList[0]}, {flowersList[1]}, and {flowersList[2]} Bouquet";
        }

        return "Bouquet";
    }
    
    public static void BouquetHandler(IPostCraftEvent c)
    {
        if (c.Item is null) return;

        List<Item> usedFlowers = c.ConsumedItems.Where(v => FlowerCache.Contains(v.ItemId)).ToList();
        List<Color> colors =
            usedFlowers.Select(v => TailoringMenu.GetDyeColor(v) ?? Color.White).ToList();

        HashSet<string> flowerNames = new HashSet<string>(usedFlowers.Select(v => v.DisplayName));
        
        ColoredObject thisObj = new(c.Item.ItemId, c.Item.Stack, colors[0])
        {
            displayName = BouquetName(flowerNames),
            displayNameFormat = BouquetName(flowerNames),
            Price = 200 + usedFlowers.Select(v => v.sellToStorePrice()).Sum(),
            modData =
            {
                ["selph.ExtraMachineConfig.ExtraColor.1"] = PatchHelper.FormatColor(colors[1]),
                ["selph.ExtraMachineConfig.ExtraColor.2"] = PatchHelper.FormatColor(colors[2])
            }
        };

        c.Item = thisObj;
    }

    public bool IsInFlowerCache(Item item)
    {
        if (_usedList.Count >= 3) _usedList.Clear();
        
        if (!_usedList.Contains(item.ItemId) && FlowerCache.Contains(item.ItemId))
        {
            _usedList.Add(item.ItemId);
            return true;
        }

        return false;
    }
    
    public List<IRecipe> AddRecipes()
    {
        IRecipeBuilder? rBuilder = bcApi?.RecipeBuilder(ContentPackId + "_Bouquet");

        if (rBuilder is null || bcApi is null)
        {
            Helper.Log("Could not add the bouquet recipe... Make sure Better Crafting is installed!");
            return new();
        }
        
        ObjectData blueJazz = Game1.objectData["597"];
        ObjectData tulip = Game1.objectData["591"];
        ObjectData sunflower = Game1.objectData["421"];

        Texture2D flowersTexture = Game1.content.Load<Texture2D>("Maps\\springobjects");

        rBuilder.AddIngredients(
            new List<IIngredient> {
                bcApi.CreateMatcherIngredient(
                    IsInFlowerCache,
                    1, 
                    () => "Any Flower", 
                    () => Game1.content.Load<Texture2D>("Maps\\springobjects"), 
                    Game1.getSourceRectForStandardTileSheet(
                        flowersTexture, blueJazz.SpriteIndex, 16, 16),
                    () => null
                ),
                bcApi.CreateMatcherIngredient(
                    IsInFlowerCache,
                    1, 
                    () => "Different Flower", 
                    () => Game1.content.Load<Texture2D>("Maps\\springobjects"), 
                    Game1.getSourceRectForStandardTileSheet(
                        flowersTexture, tulip.SpriteIndex, 16, 16),
                    () => null
                ),
                bcApi.CreateMatcherIngredient(
                    IsInFlowerCache,
                    1, 
                    () => "Other Different Flower", 
                    () => Game1.content.Load<Texture2D>("Maps\\springobjects"), 
                    Game1.getSourceRectForStandardTileSheet(
                        flowersTexture, sunflower.SpriteIndex, 16, 16),
                    () => null
                ),
            }
        );

        rBuilder.Texture(() => Game1.content.Load<Texture2D>(
            Game1.objectData[ContentPackId + "_Bouquet"].Texture
        ));
        rBuilder.Item(() => new Object(ContentPackId + "_Bouquet", 1));
        rBuilder.DisplayName(() => Game1.objectData[ContentPackId + "_Bouquet"].DisplayName);
        rBuilder.Quantity(1);
        rBuilder.Source(() => Game1.getSourceRectForStandardTileSheet(
            Game1.content.Load<Texture2D>(Game1.objectData[ContentPackId + "_Bouquet"].Texture),
            16,
            16,
            16
        ));
        rBuilder.Description(() => Game1.objectData[ContentPackId + "_Bouquet"].Description);
        
        rBuilder.OnPostCraft(BouquetHandler);
        
        IRecipe bouquet = rBuilder.Build();

        return new List<IRecipe>
        {
            bouquet
        };
    }
}