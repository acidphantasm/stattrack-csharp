using SPTarkov.Server.Core.Models.Utils;

namespace StattrackServer;

using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

[Injectable(TypePriority = OnLoadOrder.Routers + 1)]
public class SaveLoadRouter(JsonUtil jsonUtil, TrackingData trackingData)
    : StaticRouter(jsonUtil,
    [
        new RouteAction<StatTrackStats>("/stattrack/save",
        async (url, info, sessionID, output, token) => await trackingData.SaveWeaponStats(info)
        ),
        new RouteAction("/stattrack/load",
        async (url, info, sessionID, output, token) => await new ValueTask<string>(jsonUtil.Serialize(trackingData.WeaponStats))
        )
    ])
{}

