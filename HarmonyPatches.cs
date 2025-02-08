using HarmonyLib;
using System;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using Mods.GigantismPlus;

namespace Mods.GigantismPlus.HarmonyPatches
{
    [HarmonyPatch(typeof(XRL.World.GameObject))]
    class PseudoGiganticCreature_BodyWeight
    {
        // Goal is to simulate being Gigantic for the purposes of calculating body weight, if the GameObject in question is PseudoGigantic

        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameObject.GetBodyWeight))]
        static void Prefix(ref GameObject __state, GameObject __instance)
        {
            // Object matches the paramater of the original,
            // __state lets you keep stuff between Pre- and Postfixes (might be redundant for this one)

            __state = __instance; // make the transferable object the current instance.
            if (__state.HasPart<PseudoGigantism>() && !__state.IsGiganticCreature) 
            {
                // is the GameObject PseudoGigantic but not Gigantic
                Debug.Entry(4, "HarmonyPatches.cs | [HarmonyPrefix]");
                Debug.Entry(3, "GameObject.GetBodyWeight() > PseudoGigantic not Gigantic");
                __state.IsGiganticCreature = true; // make the GameObject Gigantic (we revert this as soon as the origianl method completes)
                Debug.Entry(2, "Trying to be Heavy and PseudoGigantic");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(GameObject.GetBodyWeight))]
        static void Postfix(GameObject __state)
        {
            // only need __state this time, since it holds the __instance anyway.

            if (__state.HasPart<PseudoGigantism>() && __state.IsGiganticCreature)
            {
                // is the GameObject both PseudoGigantic and Gigantic (only supposed to be possible here)
                Debug.Entry(4, "HarmonyPatches.cs | [HarmonyPostfix]");
                Debug.Entry(3, "GameObject.GetBodyWeight() > PseudoGigantic and Gigantic");
                __state.IsGiganticCreature = false; // make the GameObject not Gigantic 
                Debug.Entry(2, "Should be Heavy and PseudoGigantic\n");
            }
        }
    }


    // Why harmony for this one when it's an available event?
    // -- in the event that this hard-coded element is adjusted (such as the increase amount),
    //    this just ensures the "vanilla" behaviour is preserved by "masking" as Gigantic for the check.

    [HarmonyPatch(typeof(XRL.World.GetMaxCarriedWeightEvent))]
    class PseudoGiganticCreature_CarryCapacity
    {
        // Goal is to simulate being Gigantic for the purposes of calculating carry capacity, if the GameObject in question is PseudoGigantic
        
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GetMaxCarriedWeightEvent.GetFor))]
        static void Prefix(ref GameObject Object, ref GameObject __state) 
        {
            // Object matches the paramater of the original,
            // __state lets you keep stuff between Pre- and Postfixes (might be redundant for this one)

            __state = Object;
            if (__state.HasPart<PseudoGigantism>() && !__state.IsGiganticCreature)
            {
                // is the GameObject PseudoGigantic but not Gigantic
                Debug.Entry(4, "HarmonyPatches.cs | [HarmonyPrefix]");
                Debug.Entry(3, "GetMaxCarriedWeightEvent.GetFor > PseudoGigantic not Gigantic");
                __state.IsGiganticCreature = true; // make the GameObject Gigantic (we revert this as soon as the origianl method completes)
                Debug.Entry(2, "Trying to have Carry Capacity and PseudoGigantic\n");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(GetMaxCarriedWeightEvent.GetFor))]
        static void Postfix(GameObject __state)
        {
            // only need __state this time, since it holds the __instance anyway.

            if (__state.HasPart<PseudoGigantism>() && __state.IsGiganticCreature)
            {
                // is the GameObject both PseudoGigantic and Gigantic (only supposed to be possible here)
                Debug.Entry(4, "HarmonyPatches.cs | [HarmonyPostfix]");
                Debug.Entry(3, "GetMaxCarriedWeightEvent.GetFor() > PseudoGigantic and Gigantic");
                __state.IsGiganticCreature = false; // make the GameObject not Gigantic 
                Debug.Entry(2, "Should have Carry Capacity and PseudoGigantic");
            }
        }
    }

    // Why harmony for this one when it's an available event?
    // -- this keeps the behaviour consistent with vanilla but hijacks control
    //    outside of a vanilla getting a significant rework, this should remain compatable.

    [HarmonyPatch]
    [HarmonyPatch(typeof(ModGigantic))]
    [HarmonyPatch("HandleEvent")]
    [HarmonyPatch(new Type[] { typeof(GetDisplayNameEvent) })]
    public static class ModGiganticPatch
    {
        // goal is to change the display name of the gigantic modifier to include a text shader
        static bool Prefix(GetDisplayNameEvent E)
        {
            if (!E.Object.HasTagOrProperty("ModGiganticNoDisplayName")
                && (!E.Object.HasTagOrProperty("ModGiganticNoUnknownDisplayName") 
                    || E.Understood()) 
                /*&& !E.Object.HasProperName*/) // uncommenting this will stop relic items and the like from having gigantic displayed.
            {
                // don't put Debug.Entry lines here. This runs near constantly for every item with the gigantic mod.
                E.ApplySizeAdjective("{{gigantic|gigantic}}", 30, -20); // We're changing gigantic to {{gigantic|gigantic}}
                return false;
            }
            return true; // Continue with the original method
        }
    }
}