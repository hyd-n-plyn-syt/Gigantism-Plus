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
            
            // find the gigantic modifier ModEntry in the ModList
            foreach (ModEntry mod in ModificationFactory.ModList)
            {
                if (mod.Part == "ModGigantic")
                {
                    // should the rarity be adjusted? 
                    // - change the rarity from R2 (3) to R (2) 
                    if (ShouldDerarify) mod.Rarity = 2;
                    
                    // should tinkering be allowed? 
                    // - change the tinkerability and add it to the list of recipes
                    if (ShouldGiganticTinkerable)
                    {
                        mod.TinkerAllowed = true;
                        // this is a workaround for what I'm sure is a more straightforward and simple solution
                        // - after adjusting the ModEntry to be tinkerable, it needs to be added to the list of recipes
                        // - flushing the list of recipes and then requesting the list uses an internal "get" function that cycles all the TinkerData and ModEntries and adds them to the TinkerRecipes list
                        // - only works if you flush it first since the "get" function checks if the _list is empty first and if it isn't just returns it
                        // it's probably NOT good, and could pose compatability issues with other mods if they do things post Blueprint pre-load, but I'm not nearly experienced enough to know what issues exactly
                        TinkerData._TinkerRecipes.RemoveAll(r => r != null);
                        List<TinkerData> reinitialise = TinkerData.TinkerRecipes;
                        reinitialise = null;
                        return;
                    }

                }
            }

        }
    }
}