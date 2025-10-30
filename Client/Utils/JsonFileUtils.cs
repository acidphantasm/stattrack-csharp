using Newtonsoft.Json;
using SPT.Common.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static acidphantasm_stattrack.Utils.Utility;

namespace acidphantasm_stattrack.Utils
{
    public static class JsonFileUtils
    {
        public static Dictionary<string, CustomizedObject> WeaponInfoOutOfRaid { get; set; } = [];
        public static Dictionary<string, CustomizedObject> WeaponInfoForRaid { get; set; } = [];

        public static Dictionary<string, CustomizedObject> MergeDictionary(Dictionary<string, CustomizedObject> primaryDictionary, Dictionary<string, CustomizedObject> inRaidDictionary)
        {
            var mergedDictionary = new Dictionary<string, CustomizedObject>();
            foreach (var kvp in inRaidDictionary)
            {
                if (!mergedDictionary.ContainsKey(kvp.Key)) mergedDictionary[kvp.Key] = kvp.Value;
                else
                {
                    mergedDictionary[kvp.Key].Kills += inRaidDictionary[kvp.Key].Kills;
                    mergedDictionary[kvp.Key].Headshots += inRaidDictionary[kvp.Key].Headshots;
                    mergedDictionary[kvp.Key].TotalShots += inRaidDictionary[kvp.Key].TotalShots;
                    mergedDictionary[kvp.Key].TimesLost += inRaidDictionary[kvp.Key].TimesLost;
                }
            }
            foreach (var kvp in primaryDictionary)
            {
                if (!mergedDictionary.ContainsKey(kvp.Key)) mergedDictionary[kvp.Key] = kvp.Value;
                else
                {
                    mergedDictionary[kvp.Key].Kills += primaryDictionary[kvp.Key].Kills;
                    mergedDictionary[kvp.Key].Headshots += primaryDictionary[kvp.Key].Headshots;
                    mergedDictionary[kvp.Key].TotalShots += primaryDictionary[kvp.Key].TotalShots;
                    mergedDictionary[kvp.Key].TimesLost += primaryDictionary[kvp.Key].TimesLost;
                }
            }
            return mergedDictionary;
        }
        public static void TemporaryAddData(string weaponID, bool headshot = false, bool shot = false)
        {
            CustomizedObject values = new CustomizedObject();
            if (shot)
            {
                values.Kills = 0;
                values.Headshots = 0;
                values.TotalShots = 1;
                values.TimesLost = 0;
            }
            else if (headshot)
            {
                values.Kills = 1;
                values.Headshots = 1;
                values.TotalShots = 0;
                values.TimesLost = 0;
            }
            else if (!shot && !headshot)
            {
                values.Kills = 1;
                values.Headshots = 0;
                values.TotalShots = 0;
                values.TimesLost = 0;
            }

            if (!WeaponInfoForRaid.ContainsKey(weaponID)) WeaponInfoForRaid.Add(weaponID, values);
            else
            {
                WeaponInfoForRaid[weaponID].Kills += values.Kills;
                WeaponInfoForRaid[weaponID].Headshots += values.Headshots;
                WeaponInfoForRaid[weaponID].TotalShots += values.TotalShots;
                WeaponInfoForRaid[weaponID].TimesLost += values.TimesLost;
            }

        }

        public static string GetData(string weaponID, EStatTrackAttributeId attributeType, bool tooltip = false)
        {
            if (WeaponInfoOutOfRaid == null)
            {
                LoadFromServer();
            }
            if (!WeaponInfoOutOfRaid.ContainsKey(weaponID)) return "-";

            int killCount = WeaponInfoOutOfRaid[weaponID].Kills;
            int insuranceCount = WeaponInfoOutOfRaid[weaponID].TimesLost;
            string killDeathRatio = insuranceCount > 0 ? Math.Round(killCount / (double)insuranceCount, 2).ToString() : "∞";
            string headshotPercent = Math.Round((WeaponInfoOutOfRaid[weaponID].Headshots / (double)WeaponInfoOutOfRaid[weaponID].Kills)*100, 1).ToString();
            string shotCount = WeaponInfoOutOfRaid[weaponID].TotalShots.ToString();
            string shotsToKillAverage = Math.Round(WeaponInfoOutOfRaid[weaponID].TotalShots / (double)WeaponInfoOutOfRaid[weaponID].Kills, 2).ToString();

            if (tooltip)
            {
                return
                    $"All -{Globals.GetItemLocalizedName(weaponID)}- Stats" +
                    $"\n {killCount.ToString()} Kills" +
                    $"\n {killDeathRatio} Kill/Death Ratio" +
                    $"\n {headshotPercent} Headshot Kill %" +
                    $"\n {shotsToKillAverage} Rounds-To-Kill Average" +
                    $"\n {shotCount} Shots";
            }
            switch (attributeType)
            {
                case EStatTrackAttributeId.Kills:
                    var stringToReturn = insuranceCount > 1 ? $"{killCount.ToString()} K | {killDeathRatio} KD" : $"{killCount.ToString()} K | ∞ KD";
                    return stringToReturn;
                case EStatTrackAttributeId.Headshots:
                    return headshotPercent;
                case EStatTrackAttributeId.ShotsPerKillAverage:
                    return shotsToKillAverage;
                case EStatTrackAttributeId.Shots:
                    return shotCount;
                default:
                    return "-";
            }
        }

        public static void EndRaidMergeData()
        {
            Dictionary<string, CustomizedObject> newDictionary = MergeDictionary(WeaponInfoOutOfRaid, WeaponInfoForRaid);
            WeaponInfoOutOfRaid = newDictionary;
            if (WeaponInfoOutOfRaid.Count > 0) SaveRaidEndInServer();
        }

        public static void SaveRaidEndInServer()
        {
            try
            {
                var profile = Globals.GetPlayerProfile().ProfileId;
                
                var jsonString = JsonConvert.SerializeObject(
                    new RequestData() { Data = WeaponInfoOutOfRaid, ProfileId = profile }, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                
                RequestHandler.PutJsonAsync("/stattrack/save", jsonString);
                WeaponInfoForRaid.Clear();
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError("Failed to save: " + ex.ToString());
                NotificationManagerClass.DisplayWarningNotification("Failed to save Weapon StatTrack File - check the server");
            }
        }

        public static async Task LoadFromServer()
        {
            try
            {
                var profile = Globals.GetPlayerProfile().ProfileId;
                
                string payload = await RequestHandler.GetJsonAsync("/stattrack/load");
                var retrievedData =
                    JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, CustomizedObject>>>(payload);

                WeaponInfoOutOfRaid = retrievedData.TryGetValue(profile, out var value) ? value : new Dictionary<string, CustomizedObject>();
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError("Failed to load: " + ex.ToString());
                NotificationManagerClass.DisplayWarningNotification("Failed to load Weapon StatTrack File - check the server");
            }
        }

        public class CustomizedObject
        {
            public int Kills;
            public int Headshots;
            public int TotalShots;
            public int TimesLost;
        }

        private struct RequestData
        {
            public string ProfileId;
            public Dictionary<string, CustomizedObject> Data;
        }
    }
}
