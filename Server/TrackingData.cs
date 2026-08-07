namespace StattrackServer;

using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Spt.Bots;
using SPTarkov.Server.Core.Utils;

[Injectable(InjectionType = InjectionType.Singleton)]
public class TrackingData(
    JsonUtil jsonUtil,
    ModHelper modHelper,
    ISptLogger<TrackingData> logger): IOnLoad
{
    private string? _modPath;
    private string? _savesPath;
    public Dictionary<string, Dictionary<string, StatTrackData>> WeaponStats = new();


    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        _modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        _savesPath = Path.Join(_modPath, "Data");
        await Load(cancellationToken);
    }
    
    public async ValueTask<string> SaveWeaponStats(StatTrackStats info)
    {
        var profileId = info.ProfileId;
        WeaponStats[profileId] = info.Data;

        await Save(profileId);

        return "Success";
    }

    public async Task Save(string profileId)
    {
        try
        {
            if (!Directory.Exists(_savesPath))
                Directory.CreateDirectory(_savesPath);
            
            if (!WeaponStats.TryGetValue(profileId, out var data))
            {
                logger.Warning($"No for profile '{profileId}', skipping");
                return;
            }
            
            var dataToSave = jsonUtil.Serialize(data, indented: true);
            
            var filename = Path.Join(_savesPath, $"{profileId}.json");
            await File.WriteAllTextAsync(filename, dataToSave);
        }
        catch (Exception e)
        {
            logger.Critical(e.Message);
            throw;
        }
    }

    private async Task Load(CancellationToken cancellationToken)
    {
        try
        {
            WeaponStats = new Dictionary<string, Dictionary<string, StatTrackData>>();
            
            if (!Directory.Exists(_savesPath))
            {
                Directory.CreateDirectory(_savesPath);
                return;
            }

            var profileFilePaths = Directory.EnumerateFiles(_savesPath, "*.json", SearchOption.TopDirectoryOnly);

            foreach (var filePath in profileFilePaths)
            {
                var fullPath = Path.GetFullPath(filePath);
                var profileId = Path.GetFileNameWithoutExtension(fullPath);

                try
                {
                    var data = await jsonUtil.DeserializeFromFileAsync<Dictionary<string, StatTrackData>>(filePath, cancellationToken);

                    if (data is null)
                    {
                        logger.Warning($"Skipping '{profileId}' — JSON empty or unreadable.");
                        continue;
                    }

                    WeaponStats[profileId] = data;
                }
                catch (Exception ex)
                {
                    logger.Warning($"Failed to load profile '{profileId}' from '{fullPath}' : {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            logger.Warning($"Failed to load StatTrack Profiles: {ex.Message}");
        }
    }
}
