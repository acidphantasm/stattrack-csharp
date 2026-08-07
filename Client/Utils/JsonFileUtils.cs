namespace StattrackClient.Utils;

using Newtonsoft.Json;
using SPT.Common.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using EFT.Communications;

public static class JsonFileUtils
{
    private static bool _hasLoadedFromServer;
    private static Dictionary<string, StatTrackData> WeaponInfoOutOfRaid { get; set; } = [];
    private static Dictionary<string, StatTrackData> WeaponInfoForRaid { get; set; } = [];

    private static Dictionary<string, StatTrackData> MergeDictionary(Dictionary<string, StatTrackData> primaryDictionary, Dictionary<string, StatTrackData> inRaidDictionary)
    {
        var mergedDictionary = new Dictionary<string, StatTrackData>();
        foreach (var kvp in inRaidDictionary)
        {
            if (!mergedDictionary.ContainsKey(kvp.Key)) mergedDictionary[kvp.Key] = kvp.Value;
            else
            {
                mergedDictionary[kvp.Key].Kills += inRaidDictionary[kvp.Key].Kills;
                mergedDictionary[kvp.Key].BossKills += inRaidDictionary[kvp.Key].BossKills;
                mergedDictionary[kvp.Key].HeadshotKills += inRaidDictionary[kvp.Key].HeadshotKills;
                mergedDictionary[kvp.Key].TotalShots += inRaidDictionary[kvp.Key].TotalShots;
                mergedDictionary[kvp.Key].TotalHits += inRaidDictionary[kvp.Key].TotalHits;
                mergedDictionary[kvp.Key].TotalHeadHits += inRaidDictionary[kvp.Key].TotalHeadHits;
                mergedDictionary[kvp.Key].TotalDamage += inRaidDictionary[kvp.Key].TotalDamage;
                mergedDictionary[kvp.Key].TimesLost += inRaidDictionary[kvp.Key].TimesLost;
            }
        }
        foreach (var kvp in primaryDictionary)
        {
            if (!mergedDictionary.ContainsKey(kvp.Key)) mergedDictionary[kvp.Key] = kvp.Value;
            else
            {
                mergedDictionary[kvp.Key].Kills += primaryDictionary[kvp.Key].Kills;
                mergedDictionary[kvp.Key].BossKills += primaryDictionary[kvp.Key].BossKills;
                mergedDictionary[kvp.Key].HeadshotKills += primaryDictionary[kvp.Key].HeadshotKills;
                mergedDictionary[kvp.Key].TotalShots += primaryDictionary[kvp.Key].TotalShots;
                mergedDictionary[kvp.Key].TotalHits += primaryDictionary[kvp.Key].TotalHits;
                mergedDictionary[kvp.Key].TotalHeadHits += primaryDictionary[kvp.Key].TotalHeadHits;
                mergedDictionary[kvp.Key].TotalDamage += primaryDictionary[kvp.Key].TotalDamage;
                mergedDictionary[kvp.Key].TimesLost += primaryDictionary[kvp.Key].TimesLost;
            }
        }
        return mergedDictionary;
    }
    public static void TemporaryAddData(string weaponID, bool kill = false, bool headShotKill = false, bool shot = false, bool hit = false, bool hitHead = false, bool bossKill = false, float damageAmount = 0f)
    {
        StatTrackData values = new StatTrackData();
        if (kill)
        {
            values.Kills += 1;
        }
        
        if (headShotKill)
        {
            values.HeadshotKills += 1;
        }
        
        if (shot)
        {
            values.TotalShots += 1;
        }
        
        if (hit)
        {
            values.TotalHits += 1;
        }

        if (hitHead)
        {
            values.TotalHeadHits += 1;
        }

        if (bossKill)
        {
            values.BossKills += 1;
        }

        if (damageAmount > 0f)
        {
            values.TotalDamage += damageAmount;
        }

        if (WeaponInfoForRaid.TryAdd(weaponID, values))
            return;
        
        WeaponInfoForRaid[weaponID].Kills += values.Kills;
        WeaponInfoForRaid[weaponID].BossKills += values.BossKills;
        WeaponInfoForRaid[weaponID].HeadshotKills += values.HeadshotKills;
        WeaponInfoForRaid[weaponID].TotalShots += values.TotalShots;
        WeaponInfoForRaid[weaponID].TotalHits += values.TotalHits;
        WeaponInfoForRaid[weaponID].TotalHeadHits += values.TotalHeadHits;
        WeaponInfoForRaid[weaponID].TotalDamage += values.TotalDamage;
        WeaponInfoForRaid[weaponID].TimesLost += values.TimesLost;

    }

    private static (string killDeathRatio, string headshotPercent, string shotCount, string shotsToKillAverage, string accuracyPercent, string headshotHitPercent, string damage) ComputeDisplayStats(StatTrackData stats)
    {
        var killDeathRatio = stats.TimesLost > 0 ? Math.Round(stats.Kills / (double)stats.TimesLost, 2).ToString(CultureInfo.InvariantCulture) : "∞";
        var headshotPercent = stats.Kills > 0 ? Math.Round(stats.HeadshotKills / (double)stats.Kills * 100, 1).ToString(CultureInfo.InvariantCulture) : "-";
        var shotCount = stats.TotalShots > 0 ? stats.TotalShots.ToString() : "-"; var shotsToKillAverage = stats.Kills > 0 ? Math.Round(stats.TotalShots / (double)stats.Kills, 2).ToString(CultureInfo.InvariantCulture) : "-";
        var accuracyPercent = stats.TotalShots > 0 ? Math.Round(stats.TotalHits / (double)stats.TotalShots * 100, 1).ToString(CultureInfo.InvariantCulture) : "-";
        var headshotHitPercent = stats.TotalHits > 0 ? Math.Round(stats.TotalHeadHits / (double)stats.TotalHits * 100, 1).ToString(CultureInfo.InvariantCulture) : "-";
        var damage = stats.TotalDamage > 0 ? stats.TotalDamage.ToString("F0", CultureInfo.InvariantCulture) : "-";

        return (killDeathRatio, headshotPercent, shotCount, shotsToKillAverage, accuracyPercent, headshotHitPercent, damage);
    }

    public static string GetData(string weaponID, Utility.EStatTrackAttributeId attributeType, bool mainTooltip = false, string instanceId = "", bool botTooltip = false)
    {
        if (!_hasLoadedFromServer)
        {
            return "-";
        }

        if (!WeaponInfoOutOfRaid.TryGetValue(weaponID, out var weaponValues))
            return "-";

        var (killDeathRatio, headshotPercent, shotCount, shotsToKillAverage, accuracyPercent, headshotHitPercent, damage) = ComputeDisplayStats(weaponValues);

        if (mainTooltip)
        {
            var result =
                $"All -{Utility.GetItemLocalizedName(weaponID)}- Stats:" +
                $"\n {weaponValues.Kills} Kills" +
                $"\n {weaponValues.BossKills} Boss Kills" +
                $"\n {killDeathRatio} Kill/Death Ratio" +
                $"\n {headshotPercent}% Headshot Kills" +
                $"\n {shotsToKillAverage} Rounds-To-Kill Average" +
                $"\n" +
                $"\n {shotCount} Total Shots" +
                $"\n {accuracyPercent}% Accuracy" +
                $"\n {headshotHitPercent}% Headshot Hits" +
                $"\n" +
                $"\n {damage} Total Damage";

            if (!string.IsNullOrEmpty(instanceId) && WeaponInfoOutOfRaid.TryGetValue(instanceId, out var instanceValues))
            {
                var (iKillDeathRatio, iHeadshotPercent, iShotCount, iShotsToKillAverage, iAccuracyPercent, iHeadshotHitPercent, iDamage) = ComputeDisplayStats(instanceValues);

                result +=
                    $"\n-----------------" +
                    $"\nThis -{Utility.GetItemLocalizedName(weaponID)}- Stats:" +
                    $"\n {instanceValues.Kills} Kills" +
                    $"\n {weaponValues.BossKills} Boss Kills" +
                    $"\n {iKillDeathRatio} Kill/Death Ratio" +
                    $"\n {iHeadshotPercent}% Headshot Kills" +
                    $"\n {iShotsToKillAverage} Rounds-To-Kill Average" +
                    $"\n" +
                    $"\n {iShotCount} Total Shots" +
                    $"\n {iAccuracyPercent}% Accuracy" +
                    $"\n {iHeadshotHitPercent}% Headshot Hits" +
                    $"\n" +
                    $"\n {iDamage} Total Damage";
            }

            return result;
        }

        switch (attributeType)
        {
            case Utility.EStatTrackAttributeId.Kills:
                return $"{weaponValues.Kills} K | {killDeathRatio} KD";
            case Utility.EStatTrackAttributeId.Headshots:
                return headshotPercent;
            case Utility.EStatTrackAttributeId.ShotsPerKillAverage:
                return shotsToKillAverage;
            case Utility.EStatTrackAttributeId.Shots:
                return shotCount;
            default:
                return "-";
        }
    }

    public static void EndRaidMergeData()
    {
        var newDictionary = MergeDictionary(WeaponInfoOutOfRaid, WeaponInfoForRaid);
        WeaponInfoOutOfRaid = newDictionary;
        
        if (WeaponInfoOutOfRaid.Count > 0) 
            _ = SaveRaidEndInServer();
    }

    private static async Task SaveRaidEndInServer()
    {
        try
        {
            var profile = Utility.GetPlayerProfile().ProfileId;
            var jsonString = JsonConvert.SerializeObject(new RequestData { Data = WeaponInfoOutOfRaid, ProfileId = profile }, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            await RequestHandler.PutJsonAsync("/stattrack/save", jsonString);
            WeaponInfoForRaid.Clear();
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogError("Failed to save: " + ex);
            NotificationManager.DisplayWarningNotification("Failed to save Weapon StatTrack File - check the server");
        }
    }

    public static async Task LoadFromServer()
    {
        try
        {
            var profile = Utility.GetPlayerProfile().ProfileId;

            var payload = await RequestHandler.GetJsonAsync("/stattrack/load");
            var retrievedData =
                JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, StatTrackData>>>(payload);

            WeaponInfoOutOfRaid = retrievedData.TryGetValue(profile, out var value) ? value : new Dictionary<string, StatTrackData>();
            _hasLoadedFromServer = true;
        }
        catch (Exception ex)
        {
            Plugin.LogSource.LogError("Failed to load: " + ex);
            NotificationManager.DisplayWarningNotification("Failed to load Weapon StatTrack File - check the server");
        }
    }

    private class StatTrackData 
    {
        public int Kills;
        public int BossKills;
        public int HeadshotKills;
        public int TotalShots;
        public int TotalHits;
        public int TotalHeadHits;
        public float TotalDamage;
        public int TimesLost;
    }

    private struct RequestData
    {
        public string ProfileId;
        public Dictionary<string, StatTrackData> Data;
    }
}
