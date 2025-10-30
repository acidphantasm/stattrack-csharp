using System.Reflection;
using HarmonyLib;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;

namespace _acidphantasm_stattrack;

public class ReplaceIDsPatch: AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ItemExtensions), nameof(ItemExtensions.ReplaceIDs));
    }

    [PatchPrefix]
    public static void Prefix(IEnumerable<Item> items, out IEnumerable<Item> __state)
    {
        var cloner = ServiceLocator.ServiceProvider.GetService<ICloner>();
        
        __state = cloner.Clone(items);
    }
    
    [PatchPostfix]
    public static void PostFix(IEnumerable<Item> items, IEnumerable<Item> __state)
    {
        var cloner = ServiceLocator.ServiceProvider.GetService<ICloner>();
        var logger = ServiceLocator.ServiceProvider.GetService<ISptLogger<StatTrack>>();
        
        var dirty = false;
        var profileListNeedingResaved = new List<string>();
        
        foreach (var (originalItem, newItem) in __state.Zip(items))
        {
            foreach (var (profile, data) in SaveLoadRouter.WeaponStats)
            {
                if (data.TryGetValue(originalItem.Id, out CustomizedObject customizedObject))
                {
                    profileListNeedingResaved.Add(profile);
                    data[newItem.Id] = cloner.Clone(customizedObject);
                    dirty = true;

                    logger.Info($"StatTrack: weapon {originalItem.Id} is now {newItem.Id}, stats copied");
                }
            }
        }

        if (dirty)
        {
            foreach (var profile in profileListNeedingResaved)
            {
                logger.Info($"Saving Profile: {profile}");
                var task = SaveLoadRouter.Save(profile);
            }
        }
    }
}