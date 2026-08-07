namespace StattrackClient.Patches;

using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using Utils;

public class MenuLoadPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(EftClientBackendSession), nameof(EftClientBackendSession.RequestBuilds));
    }

    [PatchPostfix]
    public static async void Postfix(Task<IResult> __result)
    {
        await __result;
        await JsonFileUtils.LoadFromServer();
    }
}
