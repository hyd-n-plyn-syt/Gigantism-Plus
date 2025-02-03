using System;
using System.Collections.Generic;
using System.Linq;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class GigantismPlus : BaseDefaultEquipmentMutation
    {

        public GigantismPlus()
        {
            DisplayName = "Gigantism ({{r|D}})";
        }

        public override bool CanLevel() { return true; } // Enable leveling


        public override bool WantEvent(int ID, int cascade)
        {
            // Check if the ID parameter matches BeforeLevelGainedEvent or AfterLevelGainedEvent.
            return base.WantEvent(ID, cascade)
                || ID == BeforeLevelGainedEvent.ID
                || ID == AfterLevelGainedEvent.ID;
        }

        // method to swap Gigantism mutation category between Physical and PhysicalDefects
        // - Rapid advancement checks the Physical MutationCategory Entries.
        private void SwapMutationCategory(bool Before = true)
        {
            // prefer this for repeated uses of strings.
            string Physical = "Physical";
            string PhysicalDefects = "PhysicalDefects";

            // direction of swap depends on whether before or after LevelGain
            string IntoCategory = Before ? Physical : PhysicalDefects;
            string OutOfCategory = Before ? PhysicalDefects : Physical;
            MutationEntry GigantismEntry = MutationFactory.GetMutationEntryByName(this.Name);
            foreach (MutationCategory category in MutationFactory.GetCategories())
            {
                if (category.Name == IntoCategory)
                {
                    // UnityEngine.Debug.LogError("Adding " + GigantismEntry.DisplayName + " to " + IntoCategory + "Category");
                    category.Add(GigantismEntry);
                    category.Entries.Sort((x, y) => x.DisplayName.CompareTo(y.DisplayName));
                    /* Debug Logging. May turn this into an option.
                    foreach (MutationEntry entry in category.Entries)
                    {
                        UnityEngine.Debug.LogError(entry.DisplayName);
                    }*/
                }
                if (category.Name == OutOfCategory)
                {
                    // UnityEngine.Debug.LogError("Removing " + GigantismEntry.DisplayName + " from " + OutOfCategory + "Category");
                    category.Entries.RemoveAll(r => r == GigantismEntry);
                }
            }
        }

        // don't like that these are duplicates.
        // I'm certain there's a way to collapse them into a single function that accepts either.
        public override bool HandleEvent(BeforeLevelGainedEvent E)
        {
            int Level = E.Level;
            GameObject Actor = E.Actor;
            bool IsMutant = Actor.IsMutant();
            bool RapidAdvancement = IsMutant 
                                 && (Level + 5) % 10 == 0 
                                 && !Actor.IsEsper() 
                                 && Mods.GigantismPlus.Options.EnableGigantismRapidAdvance;
            if (RapidAdvancement)
            {
                SwapMutationCategory(true);
            }
            return true;
        }

        public override bool HandleEvent(AfterLevelGainedEvent E)
        {
            int Level = E.Level;
            GameObject Actor = E.Actor;
            bool IsMutant = Actor.IsMutant();
            bool RapidAdvancement = IsMutant
                                 && (Level + 5) % 10 == 0
                                 && !Actor.IsEsper()
                                 && Mods.GigantismPlus.Options.EnableGigantismRapidAdvance;
            if (RapidAdvancement)
            {
                SwapMutationCategory(false);
            }
            return true;
        }

        // adjusted for readability.
        public override string GetDescription()
        {
            return "You are unusually large, find it difficult to enter small spaces, and can typically {{rules|only}} use {{rules|gigantic}} equipment.\n" 
                 + "You are heavy, can carry more weight, and all your natural weapons are now gigantic.\n\n"
                 + "Your gigantic fists gain:\n"
                 + "{{rules|+1}} To-Hit every {{rules|2 mutation levels}}\n"
                 + "{{B|d1}} damage every {{B|3 mutation levels}}\n"
                 + "{{W|1d}} damage every {{W|5 mutation levels}}\n"
                 + "They have {{rules|uncapped penetration}}, but they are harder {{rules|to hit}} with due to their size.";
        }

        // adjusted for readability and accuracy.
        // would like to put the variables used below into public properties so they can be used elsewhere.
        public override string GetLevelText(int Level)
        {
            int diceCount = 1 + (Level / 5); // Number of dice increases every 5 levels
            int diceType = 3 + (Level / 3); // Start at d3, increase every 3 levels
            int toHitBonus = -3 + (Level / 2); // -3 to start, increases by +1 every 2 levels

            return "gigantic fists {{rules|\x1A 4}}{{k|/9999}} {{r|\x03}}{{W|" + diceCount + "}}{{rules|d}}{{B|" + diceType + "}}{{rules|+3}}\n"
                 + "and {{rules|" + toHitBonus + "}} To-Hit\n";
        }

        // need to work out if there's another way to do this that includes adjustments to the creature's weight and carry cap.
        public override bool Mutate(GameObject GO, int Level)
        {
            if (GO != null)
            {
                GO.IsGiganticCreature = true; // Enable the Gigantic flag
            }
            return base.Mutate(GO, Level);
        }

        public override bool Unmutate(GameObject GO)
        {
            if (GO != null)
            {
                GO.IsGiganticCreature = false; // Revert the Gigantic flag
            }
            return base.Unmutate(GO);
        }

        // would like to pull the gigantic fist game object out into a public child of the class similar to how the Carapace mutation does it.
        public override void OnRegenerateDefaultEquipment(Body body)
        {
            int mutationLevel = this.Level;
            int diceCount = 1 + (mutationLevel / 5); // Number of dice increases every 5 levels
            int diceType = 3 + (mutationLevel / 3); // Start at d3, increase every 3 levels
            string damage = $"{diceCount}d{diceType}";
            int toHitBonus = -3 + (mutationLevel / 2); // -3 to start, increases by +1 every 2 levels

            foreach (BodyPart hand in body.GetParts())
            {
                if (hand.Type == "Hand")
                {
                    // Create a gigantic fist object
                    GameObject giganticFist = GameObjectFactory.Factory.CreateObject("GiganticFist");
                    MeleeWeapon fistWeapon = giganticFist.GetPart<MeleeWeapon>();
                    if (fistWeapon != null)
                    {
                        fistWeapon.BaseDamage = damage; // Adjust base damage
                        fistWeapon.MaxStrengthBonus = 9999; // Unlimited strength bonus
                        fistWeapon.HitBonus = toHitBonus; // Hit bonus based on mutation level
                    }

                    // Assign the gigantic fist as the default behavior of the hand
                    hand.DefaultBehavior = giganticFist;
                }
            }

            base.OnRegenerateDefaultEquipment(body);
        }

        public override bool GeneratesEquipment()
        {
            return true;
        }
    }
}