using HarmonyLib;
using Verse;

namespace ChapaGriff_UnloadHere
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("rimworld.mod.chapagriff.unloadhere");
            harmony.PatchAll();
        }
    }
}
