using EFT.InventoryLogic;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using HarmonyLib;
using static StattrackClient.Utils.Utility;
using StattrackClient.Utils;

namespace StattrackClient.Patches;

internal class WeaponPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Constructor(typeof(Weapon), new Type[] { typeof(string), typeof(WeaponTemplate)});
    }

    [PatchPostfix]
    private static void PatchPostfix(Weapon __instance, string id, WeaponTemplate template)
    {
        RangedItemAttribute statTrack = new RangedItemAttribute((EItemAttributeId)EStatTrackAttributeId.Kills);
        statTrack.Name = EStatTrackAttributeId.Kills.GetName();
        statTrack.Base = () => 1f;
        statTrack.StringValue = () => JsonFileUtils.GetData(id, EStatTrackAttributeId.Kills);
        statTrack.Tooltip = () => JsonFileUtils.GetData(__instance.TemplateId, EStatTrackAttributeId.Kills, true, id);
        statTrack.DisplayType = () => EItemAttributeDisplayType.Compact;
        SafelyAddAttributeToList(statTrack, __instance);

        RangedItemAttribute hsTrack = new RangedItemAttribute((EItemAttributeId)EStatTrackAttributeId.Headshots);
        hsTrack.Name = EStatTrackAttributeId.Headshots.GetName();
        hsTrack.Base = () => 1f;
        hsTrack.StringValue = () => JsonFileUtils.GetData(id, EStatTrackAttributeId.Headshots);
        hsTrack.DisplayType = () => EItemAttributeDisplayType.Compact;
        SafelyAddAttributeToList(hsTrack, __instance);

        RangedItemAttribute shotPerKillTrack = new RangedItemAttribute((EItemAttributeId)EStatTrackAttributeId.ShotsPerKillAverage);
        shotPerKillTrack.Name = EStatTrackAttributeId.ShotsPerKillAverage.GetName();
        shotPerKillTrack.Base = () => 1f;
        shotPerKillTrack.StringValue = () => JsonFileUtils.GetData(id, EStatTrackAttributeId.ShotsPerKillAverage);
        shotPerKillTrack.DisplayType = () => EItemAttributeDisplayType.Compact;
        SafelyAddAttributeToList(shotPerKillTrack, __instance);

        RangedItemAttribute shotTrack = new RangedItemAttribute((EItemAttributeId)EStatTrackAttributeId.Shots);
        shotTrack.Name = EStatTrackAttributeId.Shots.GetName();
        shotTrack.Base = () => 1f;
        shotTrack.StringValue = () => JsonFileUtils.GetData(id, EStatTrackAttributeId.Shots);
        shotTrack.DisplayType = () => EItemAttributeDisplayType.Compact;
        SafelyAddAttributeToList(shotTrack, __instance);
    }
}

internal class WeaponOnShotPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Weapon), nameof(Weapon.OnShot));
    }

    [PatchPostfix]
    private static void PatchPostfix(Weapon __instance)
    {
        if (__instance.Owner.ID == Utility.GetPlayerProfile().ProfileId)
        {
            var weaponTpl = __instance.TemplateId;
            var weaponID = __instance.Id;

            JsonFileUtils.TemporaryAddData(weaponID, false, false, true, false, false, false, 0f);
            JsonFileUtils.TemporaryAddData(weaponTpl, false, false, true, false, false, false, 0f);
        }
    }
}
