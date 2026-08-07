using StattrackClient.Patches;
using BepInEx;
using BepInEx.Logging;

namespace StattrackClient
{
    [BepInPlugin("com.acidphantasm.stattrack", "acidphantasm-StatTrack", "2.1.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;

        internal void Awake()
        {
            LogSource = Logger;

            new WeaponPatch().Enable();
            new WeaponOnShotPatch().Enable();
            new PlayerPatch().Enable();
            new PlayerPatch2().Enable();
            new GameWorldPatch().Enable();
            new MenuLoadPatch().Enable();
            new InsurancePatch().Enable();
        }
    }
}
