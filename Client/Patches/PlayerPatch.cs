namespace StattrackClient.Patches;

using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using EFT;
using Utils;
using EFT.Ballistics;
using EFT.HealthSystem;

internal class PlayerPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player), nameof(Player.OnBeenKilledByAggressor));
    }

    [PatchPostfix]
    private static void PatchPostfix(Player __instance, IPlayer aggressor, DamageInfo damageInfo, EBodyPart bodyPart, EDamageType lethalDamageType)
    {
        if (!aggressor.IsYourPlayer || lethalDamageType != EDamageType.Bullet) return;

        var weapon = damageInfo.Weapon.Id;
        var weaponTpl = damageInfo.Weapon.TemplateId;

        var isBoss = false;
        if (WildSpawnTypeExtension._spawnTypeSettings.TryGetValue(__instance.Profile.Info.Settings.Role, out var wildSpawnTypeSettings))
        {
            isBoss = __instance.IsAI && __instance.AIData.IAmBoss && wildSpawnTypeSettings.ScavRoleKey == "ScavRole/Boss";
        }

        if (bodyPart == EBodyPart.Head)
        {
            JsonFileUtils.TemporaryAddData(weapon, true, true, false, false, false, isBoss, 0f);
            JsonFileUtils.TemporaryAddData(weaponTpl, true, true, false, false, false, isBoss, 0f);
        }
        else
        {
            JsonFileUtils.TemporaryAddData(weapon, true, false, false, false, false, isBoss, 0f);
            JsonFileUtils.TemporaryAddData(weaponTpl, true, false, false, false, false, isBoss, 0f);
        }
    }
}

internal class PlayerPatch2 : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player), nameof(Player.OnHealthApplyDamage));
    }

    [PatchPostfix]
    private static void PatchPostfix(Player __instance, EBodyPart bodyPart, float damage, DamageInfo damageInfo)
    {
        if (__instance.IsYourPlayer || damageInfo.DamageType != EDamageType.Bullet || damageInfo.DamageType.IsSelfInflicted()) return;

        var weapon = damageInfo.Weapon.Id;
        var weaponTpl = damageInfo.Weapon.TemplateId;

        if (bodyPart == EBodyPart.Head)
        {
            JsonFileUtils.TemporaryAddData(weapon, false, false, false, true, true, false, damage);
            JsonFileUtils.TemporaryAddData(weaponTpl, false, false, false, true, true, false, damage);
        }
        else
        {
            JsonFileUtils.TemporaryAddData(weapon, false, false, false, true, false, false, damage);
            JsonFileUtils.TemporaryAddData(weaponTpl, false, false, false, true, false, false, damage);
        }
    }
}