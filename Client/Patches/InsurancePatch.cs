namespace StattrackClient.Patches;

using Utils;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using ChatShared;
using EFT;

public class InsurancePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(SocialNetwork), "DisplayMessage", [typeof(DialogueChatMessage), typeof(string)]);
    }

    [PatchPostfix]
    public static void Postfix(DialogueChatMessage message)
    {
        if (message.HasRewards && message.Type == EMessageType.InsuranceReturn)
        {
            JsonFileUtils.LoadFromServer();
        }
    }
}
