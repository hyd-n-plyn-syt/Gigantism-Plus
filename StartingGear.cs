using XRL; 
using XRL.World;
using XRL.World.Tinkering;

namespace Mods.GigantismPlus
{
    [PlayerMutator]
    public class GigantifyStartingLoadout : IPlayerMutator
    {
        public void mutate(GameObject player)
        {
            // Is the player Gigantic, and is the option to have gigantic starting gear set?
            if (player.HasPart("GigantismPlus") && Options.EnableGiganticStartingGear)
            {
                // Cycle the player's inventory and equipped items.
                foreach (GameObject item in player.GetInventoryAndEquipment())
                {
                    // Can the item have the gigantic modifier applied?
                    if (ItemModding.ModificationApplicable("ModGigantic", item))
                    {
                        // Is the item a grenade, and is the option not set to include them?
                        if (!Options.EnableGiganticStartingGear_Grenades && item.HasTag("Grenade")) continue;
                        
                        // Is the item a trade good? We don't want gigantic copper nuggets making the start too easy
                        if (item.HasTag("DynamicObjectsTable:TradeGoods")) continue;

                        // Is the item a tonic? Double doses are basically useless in the early game
                        if (item.HasTag("DynamicObjectsTable:Tonics_NonRare")) continue;

                        // apply the gigantic mod to the item and attempt to auto-equip it
                        ItemModding.ApplyModification(item, "ModGigantic");
                        player.AutoEquip(item);
                    }
                }
            }

        }
    }
}