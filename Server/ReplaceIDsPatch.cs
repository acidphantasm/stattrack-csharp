namespace StattrackServer;

using System.Reflection;
using HarmonyLib;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Utils.Cloners;

[Injectable]
public class ReplaceIDsPatch: AbstractPatch
{
    private static ICloner _cloner = default!;
    private static ISptLogger<ReplaceIDsPatch> _logger = default!;
    private static TrackingData _trackingData = default!;
    
    public ReplaceIDsPatch(ICloner cloner, ISptLogger<ReplaceIDsPatch> logger, TrackingData trackingData)
    {
        _cloner = cloner;
        _logger = logger;
        _trackingData = trackingData;
    }
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ItemExtensions), nameof(ItemExtensions.ReplaceIDs));
    }

    [PatchPrefix]
    public static void Prefix(IEnumerable<Item> items, out IEnumerable<Item> __state)
    {
        __state = _cloner.Clone(items);
    }
    
    [PatchPostfix]
    public static void PostFix(IEnumerable<Item> items, IEnumerable<Item> __state)
    {
        var dirty = false;
        var profileListNeedingResaved = new List<string>();
        
        foreach (var (originalItem, newItem) in __state.Zip(items))
        {
            foreach (var (profile, data) in _trackingData.WeaponStats)
            {
                if (data.TryGetValue(originalItem.Id, out var customizedObject))
                {
                    profileListNeedingResaved.Add(profile);
                    data[newItem.Id] = _cloner.Clone(customizedObject);
                    data[newItem.Id].TimesLost += 1;
                    dirty = true;

                    _logger.Info($"StatTrack: weapon {originalItem.Id} is now {newItem.Id}, stats copied");
                }
            }
        }

        if (dirty)
        {
            foreach (var profile in profileListNeedingResaved)
            {
                _logger.Info($"Saving Profile: {profile}");
                var task = _trackingData.Save(profile);
            }
        }
    }
}