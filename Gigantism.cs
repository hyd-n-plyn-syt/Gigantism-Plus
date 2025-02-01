using System;
using System.Collections.Generic;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class Gigantism : BaseDefaultEquipmentMutation
    {
        public Gigantism()
        {
            DisplayName = "Gigantism ({{r|D}})";
            base.Type = "Physical";
        }

        public override bool CanLevel() { return true; } // Enable leveling

        public override string GetDescription()
        {
            return "You are unusually large, find it difficult to enter small spaces, and can {{rules|only}} use {{rules|gigantic}} size equipment. You are heavy and can carry more weight.\n\n" +
                   "Your gigantic fists gain {{rules|d1}} every {{rules|3}} ranks of the mutation, {{rules|1d}} every {{rules|5}}. They have {{rules|no}} penetration bonus cap, but you hit far less often with them due to your size.";
        }

        public override string GetLevelText(int Level)
        {
            int diceCount = 1 + (Level / 5); // Number of dice increases every 5 levels
            int diceType = 3 + (Level / 3); // Start at d3, increase every 3 levels
            int toHitBonus = -3 + (Level / 2); // -3 to start, increases by +1 every 2 levels

            return "{{rules|" + toHitBonus + "}} To-Hit. Your fists currently deal {{rules|" + diceCount + "d" + diceType + "}} damage.";
        }

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