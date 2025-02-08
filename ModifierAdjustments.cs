using System;
using System.Collections.Generic;
using XRL; 
using XRL.World;
using XRL.World.Tinkering;
using XRL.World.Parts;

namespace Mods.GigantismPlus
{
    [PlayerMutator]
    public class AdjustGiganticModifier : IPlayerMutator
    {
        public void mutate(GameObject player)
        {
            // converts the two scenarios into a single truth value for readability
            // Option Value 2 is "Everyone"
            // Option Value 1 is "Gigantic (D) players"
            // Everyone || (Gigantic && player is Gigantic)
            bool ShouldDerarify = Options.SelectGiganticDerarification == 2 || (Options.SelectGiganticDerarification == 1 && player.HasPart("GigantismPlus"));
            bool ShouldGiganticTinkerable = Options.SelectGiganticTinkering == 2 || (Options.SelectGiganticTinkering == 1 && player.HasPart("GigantismPlus"));

            Debug.Entry(1, "/ Checking if ModGigantic needs adjustments.");
            Debug.Entry(2, "________________________________________|");
            
            Debug.Entry(2, "/ Spinning up ModList");
            // find the gigantic modifier ModEntry in the ModList
            foreach (ModEntry mod in ModificationFactory.ModList)
            {
                string ModPart = mod.Part;
                Debug.Entry(3, "BEGIN ENTRY", ModPart);
                ModPart = "| " + ModPart;
                if (mod.Part == "ModGigantic")
                {
                    Debug.Entry(3, ModPart, "Found");
                    // should the rarity be adjusted? 
                    // - change the rarity from R2 (3) to R (2) 
                    if (ShouldDerarify)
                    {
                        mod.Rarity = 2;
                        Debug.Entry(2, ModPart, "Rarity adjusted");
                    } else Debug.Entry(2, ModPart, "No rarity adjustment");

                    // should tinkering be allowed? 
                    // - change the tinkerability and add it to the list of recipes
                    if (ShouldGiganticTinkerable)
                    {
                        mod.TinkerAllowed = true;
                        Debug.Entry(2, ModPart, "Can now be tinkered");

                        // Modifiers can actually be set to require an additional ingredient.
                        // mod.TinkerIngredient = "Torch";

                    } else Debug.Entry(2, ModPart, "No tinkerability adjustment");

                    // this is a workaround for what I'm sure is a more straightforward and simple solution
                    // - after adjusting the ModEntry to be tinkerable, it needs to be added to the list of recipes
                    // - flushing the list of recipes and then requesting the list uses an internal "get" function that cycles all the TinkerData and ModEntries and adds them to the TinkerRecipes list
                    // - only works if you flush it first since the "get" function checks if the _list is empty first and if it isn't just returns it
                    // it's probably NOT good, and could pose compatability issues with other mods if they do things post Blueprint pre-load, but I'm not nearly experienced enough to know what issues exactly

                    TinkerData._TinkerRecipes.RemoveAll(r => r != null); Debug.Entry(2, "| Purged TinkerRecipes");
                    List<TinkerData> reinitialise = TinkerData.TinkerRecipes; Debug.Entry(2, "| Reinitialised TinkerRecipes");
                    reinitialise = null; Debug.Entry(4, "| Reinitialisation nulled");

                    Debug.Entry(4, "\\ No Further Actions Required", "Exiting ModList");
                    Debug.Entry(1, "________________________________________");

                    return;

                } else Debug.Entry(2, ModPart, "Not ModGigantic");

                Debug.Entry(3, "END ENTRY ------------------------------|");
                Debug.Entry(3, "________________________________________|");
                Debug.Entry(3, "");
            }
            Debug.Entry(1, "\\ ModList exited, adjustment process finished.");
            Debug.Entry(1, "________________________________________");
        }
    }
}