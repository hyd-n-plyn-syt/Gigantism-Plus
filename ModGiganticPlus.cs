using HarmonyLib;
using XRL.World.Parts;
using XRL.World;

namespace Mods.GigantismPlus
{
    [HarmonyPatch(typeof(ModGigantic))]
    [HarmonyPatch("ApplyModification")]
    public static class ModGigantic_LightSourcePatch
    {
        static void Postfix(ModGigantic __instance, GameObject Object)
        {
            LightSource part = Object.GetPart<LightSource>();
            if (part != null)
            {
                part.Radius *= 2;
            }
        }
    }
}
