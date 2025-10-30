using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace _acidphantasm_stattrack;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 666)]
public class SaveLoadRouter : StaticRouter
{
    private static JsonUtil? _jsonUtil;
    private static HttpResponseUtil? _httpResponseUtil;
    private static string? _modPath;
    private static string? _savesPath;
    private static ISptLogger<SaveLoadRouter>? _logger;
    
    public static Dictionary<string, Dictionary<string, CustomizedObject>>? WeaponStats = null;

    public SaveLoadRouter(
        JsonUtil jsonUtil,
        HttpResponseUtil httpResponseUtil,
        ModHelper modHelper,
        ISptLogger<SaveLoadRouter> logger
    ) : base(
        jsonUtil,
        GetCustomRoutes()
    )
    {
        _jsonUtil = jsonUtil;
        _httpResponseUtil = httpResponseUtil;
        _modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());;
        _savesPath = Path.Join(_modPath, "Data");
        _logger = logger;
        Load();
    }
    
    private static List<RouteAction> GetCustomRoutes()
    {
        return
        [
            new RouteAction<StatTrackStats>("/stattrack/save",
                async (
                    url,
                    info,
                    sessionID,
                    output
                ) => await SaveWeaponStats(info)
            ),
            new RouteAction("/stattrack/load",
                async (
                    url,
                    info,
                    sessionID,
                    output
                ) => await new ValueTask<string>(_jsonUtil.Serialize(WeaponStats))
            )
        ];
    }
    
    private static ValueTask<string> SaveWeaponStats(StatTrackStats info)
    {
        var profileId = info.ProfileId;
        WeaponStats[profileId] = info.Data;

        Task.Run(() => Save(profileId));
        return new ValueTask<string>(_httpResponseUtil.NullResponse());
    }

    public static async Task Save(string profileId)
    {
        try
        {
            if (!Directory.Exists(_savesPath))
                Directory.CreateDirectory(_savesPath);
            
            if (!WeaponStats.TryGetValue(profileId, out var data))
            {
                _logger.Warning($"No for profile '{profileId}', skipping");
                return;
            }
            
            var dataToSave = _jsonUtil.Serialize(data, indented: true);
            
            var filename = Path.Join(_savesPath, $"{profileId}.json");
            await File.WriteAllTextAsync(filename, dataToSave);
        }
        catch (Exception e)
        {
            _logger.Critical(e.Message);
            throw;
        }
    }

    private static async Task Load()
    {
        try
        {
            WeaponStats = new Dictionary<string, Dictionary<string, CustomizedObject>>();
            
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
                    var data = await _jsonUtil.DeserializeFromFileAsync<Dictionary<string, CustomizedObject>>(filePath);

                    if (data is null)
                    {
                        _logger.Warning($"Skipping '{profileId}' — JSON empty or unreadable.");
                        continue;
                    }

                    WeaponStats[profileId] = data;
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Failed to load profile '{profileId}' from '{fullPath}' : {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to load StatTrack Profiles: {ex.Message}");
        }
    }
}

public record CustomizedObject
{
    public int Kills { get; set; }
    public int Headshots { get; set; }
    public int TotalShots { get; set; }
    public int TimesLost { get; set; }
}


public record StatTrackStats : IRequestData
{
    public Dictionary<string, CustomizedObject> Data { get; set; }
    public string ProfileId { get; set; }
}