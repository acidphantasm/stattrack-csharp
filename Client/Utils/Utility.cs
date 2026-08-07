namespace StattrackClient.Utils;

using EFT.InventoryLogic;
using Comfort.Common;
using EFT;
using SPT.Reflection.Utils;

public static class Utility
{
    public enum EStatTrackAttributeId
    {
        Kills = 23,
        Headshots = 43,
        ShotsPerKillAverage = 8,
        Shots = 48
    }

    public static string GetName(this EStatTrackAttributeId id)
    {
        switch (id)
        {
            case EStatTrackAttributeId.Kills:
                return "KILLS | K/D RATIO";
            case EStatTrackAttributeId.Headshots:
                return "HEADSHOT KILL %";
            case EStatTrackAttributeId.ShotsPerKillAverage:
                return "ROUNDS TO KILL AVG";
            case EStatTrackAttributeId.Shots:
                return "ROUNDS FIRED";
            default:
                return id.ToString();
        }
    }

    public static void SafelyAddAttributeToList(RangedItemAttribute itemAttribute, Weapon __instance)
    {
        if (itemAttribute.Base() != 0f)
        {
            __instance.Attributes.Add(itemAttribute);
        }
    }
    
    public static string GetItemLocalizedName(string itemID)
    {
        if (Singleton<ItemFactory>.Instance != null)
        {
            if (Singleton<ItemFactory>.Instance.GetPresetItem(itemID).LocalizedShortName() != null)
            {
                return Singleton<ItemFactory>.Instance.GetPresetItem(itemID).LocalizedShortName();
            }
        }
        return "WEAPON NAME NOT FOUND";
    }

    public static string GetBotLocalizedName(string botId)
    {
        return "bot";
    }
    
    public static Profile GetPlayerProfile()
    {
        return ClientAppUtils.GetClientApp().GetClientBackEndSession().Profile;
    }
}