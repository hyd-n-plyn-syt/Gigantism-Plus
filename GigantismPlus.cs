using System;
using System.Collections.Generic;
using System.Linq;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class GigantismPlus : BaseDefaultEquipmentMutation
    {

        public int FistDamageDieCount;

        public int FistDamageDieSize;

        private string FistBaseDamage;

        public int FistHitBonus;

        public static int FistMaxStrengthBonus = 999;

        public GameObject GiganticFistObject;

        public int CarryCapacityBonus;

        public bool HunchedOver;
        
        public int HunchedOverAVModifier;

        public int HunchedOverDVModifier;

        public int HunchedOverMSModifier;

        public static int GetFistDamageDieCount(int Level)
        {
            return 1 + (int)Math.Floor((double)Level / 5.0);
        }

        public static int GetFistDamageDieSize(int Level)
        {
            return 3 + (int)Math.Floor((double)Level / 3.0);
        }

        private static string GetFistBaseDamage(int Level)
        {
            return $"{GetFistDamageDieCount(Level)}d{GetFistDamageDieSize(Level)}";
        }

        public static int GetFistHitBonus(int Level)
        {
            return -3 + (int)Math.Floor((double)Level / 2.0);
        }

        public static int GetHunchedOverAVModifier(int Level)
        {
            return 4;
        }

        public static int GetHunchedOverDVModifier(int Level)
        {
            return -6;
        }

        public static int GetHunchedOverMSModifier(int Level)
        {
            return -60 + (int)Math.Floor((double)Math.Min(Level,6) * 10.0);
        }

        public string BlueprintName => Variant.Coalesce("GiganticFist");
        
        [NonSerialized]
        protected GameObjectBlueprint _Blueprint;
        
        public GameObjectBlueprint Blueprint
        {
            get
            {
                if (_Blueprint == null)
                {
                    _Blueprint = GameObjectFactory.Factory.GetBlueprint(BlueprintName);
                }
                return _Blueprint;
            }
        }

        public GigantismPlus()
        {
            DisplayName = "Gigantism ({{r|D}})";
        }

        public override bool CanLevel() { return true; } // Enable leveling

        public override bool GeneratesEquipment()
        {
            return true;
        }

        public override bool ChangeLevel(int NewLevel)
        {
            // update the Fist properties.
            // updare the GiganticFist MeleeWeapon with new properties
            FistDamageDieCount = GetFistDamageDieCount(NewLevel);
            FistDamageDieSize = GetFistDamageDieSize(NewLevel);
            FistBaseDamage = GetFistBaseDamage(NewLevel);
            FistHitBonus = GetFistHitBonus(NewLevel);
            if (GiganticFistObject != null)
            {
                GiganticFistObject = GameObjectFactory.Factory.CreateObject(Blueprint);
                MeleeWeapon GiantFistWeapon = GiganticFistObject.GetPart<MeleeWeapon>();
                GiantFistWeapon.BaseDamage = FistBaseDamage;
                GiantFistWeapon.HitBonus = FistHitBonus;
                GiantFistWeapon.MaxStrengthBonus = FistMaxStrengthBonus;
            }

            // stand up straight.
            // update HunchOver ability stats.
            StraightenUp();
            HunchedOverAVModifier = GetHunchedOverAVModifier(NewLevel);
            HunchedOverDVModifier = GetHunchedOverDVModifier(NewLevel);
            HunchedOverMSModifier = GetHunchedOverMSModifier(NewLevel);

            // add code here to check whether the player was hunched over
            // before leveling up the mutation occurred.
            // If they were, flip a bool here so the energy cost of
            // hunching over should be reduced to 0, then hunch over
            // and flip the bool again immediately after the free bend.

            return base.ChangeLevel(NewLevel);
        }

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
        } //!--- private void SwapMutationCategory(bool Before = true)

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
            return "You are unusually large, will {{rules|struggle to enter small spaces}} without {{g|hunching over}}, and can typically {{rules|only}} use {{gigantic|gigantic}} equipment.\n"
                 + "You are {{rules|heavy}}, can carry {{rules|twice}} as much weight, and all your natural weapons are {{gigantic|gigantic}}.\n\n"
                 + "Your gigantic fists gain:\n"
                 + "{{rules|+1}} To-Hit every {{rules|2 mutation levels}}\n"
                 + "{{B|d1}} damage every {{B|3 mutation levels}}\n"
                 + "{{W|1d}} damage every {{W|5 mutation levels}}\n"
                 + "They have {{rules|uncapped penetration}}, but are harder {{rules|to hit}} with due to their size.";
        }

        // adjusted for readability and accuracy.
        // would like to put the variables used below into public properties so they can be used elsewhere.
        public override string GetLevelText(int Level)
        {
            string MSPenalty;
            if (GetHunchedOverMSModifier(Level) >= 0)
            {
                MSPenalty = "No}} MS pentalty";
            }
            else
            {
                MSPenalty = GetHunchedOverMSModifier(Level) + "}} MS";
            }
            return "Gigantic Fists {{rules|\x1A}}{{rules|4}}{{k|/\xEC}} {{r|\x03}}{{W|" + GetFistDamageDieCount(Level) + "}}{{rules|d}}{{B|" + GetFistDamageDieSize(Level) + "}}{{rules|+3}}\n"
                 + "and {{rules|" + GetFistHitBonus(Level) + "}} To-Hit\n"
                 + "{{rules|" + MSPenalty + " when {{g|Hunched Over}}\n";
        }

        // need to work out if there's another way to do this that includes adjustments to the creature's weight and carry cap.
        public override bool Mutate(GameObject GO, int Level)
        {
            Body body = GO.Body;
            if (body != null)
            {
                GO.IsGiganticCreature = true; // Enable the Gigantic flag
                ActivatedAbilityID = AddMyActivatedAbility("Hunch Over", "CommandHunchOver", "Physical Defects", null, "&#214");
            
                foreach (BodyPart hand in body.GetParts())
                {
                    if (hand.Type == "Hand")
                    {
                        AddGiganticFistTo(hand);
                    }
                }
            }
            return base.Mutate(GO, Level);
        }

        public override bool Unmutate(GameObject GO)
        {
            StraightenUp();
            if (GO != null)
            {
                GO.IsGiganticCreature = false; // Revert the Gigantic flag
            }
            CleanUpMutationEquipment(GO, ref GiganticFistObject);
            RemoveMyActivatedAbility(ref ActivatedAbilityID);
            return base.Unmutate(GO);
        }

        public void AddGiganticFistTo(BodyPart part)
        {
            if (part != null && part.Type == "Hand")
            {
                if (GiganticFistObject == null)
                {
                    GiganticFistObject = GameObjectFactory.Factory.CreateObject(Blueprint);
                }
                part.DefaultBehavior = GiganticFistObject;
            }
        } //!--- public void AddGiganticFistTo(BodyPart body)

        // would like to pull the gigantic fist game object out into a public child of the class similar to how the Carapace mutation does it.
        public override void OnRegenerateDefaultEquipment(Body body)
        {
            foreach (BodyPart hand in body.GetParts())
            {
                if (hand.Type == "Hand")
                {
                    AddGiganticFistTo(hand);
                }
            }

            base.OnRegenerateDefaultEquipment(body);
        } //!--- public override void OnRegenerateDefaultEquipment(Body body)

        // needs to be converted from Carapace.
        public void StraightenUp(bool Message = false)
        {
            if (!HunchedOver)
            {
                return;
            }
            /*
            ParentObject.Statistics["AV"].Bonus -= TightFactor;
            ParentObject.Statistics["DV"].Penalty -= 2;
            Tight = false;
            TightFactor = 0;
            ParentObject.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_negativeVitality");
            if (!Message)
            {
                return;
            }
            if (CarapaceObject == null)
            {
                MetricsManager.LogError(ParentObject.DebugName + " had no CarapaceObject for Carapace loosen message");
                if (ParentObject.IsPlayer())
                {
                    Popup.Show(ParentObject.Poss("carapace") + " loosens. Your AV decreases by {{R|" + ACModifier + "}}.");
                }
                else
                {
                    IComponent<GameObject>.EmitMessage(ParentObject, ParentObject.Poss("carapace") + " loosens.");
                }
            }
            else if (ParentObject.IsPlayer())
            {
                Popup.Show(CarapaceObject.Does("loosen", int.MaxValue, null, null, null, AsIfKnown: false, Single: false, NoConfusion: false, NoColor: false, Stripped: false, WithoutTitles: true, Short: true, BaseOnly: false, WithIndefiniteArticle: false, null, IndicateHidden: false, Pronoun: false, SecondPerson: true, null) + ". Your AV decreases by {{R|" + ACModifier + "}}.");
            }
            else
            {
                IComponent<GameObject>.EmitMessage(ParentObject, CarapaceObject.Does("loosen", int.MaxValue, null, null, null, AsIfKnown: false, Single: false, NoConfusion: false, NoColor: false, Stripped: false, WithoutTitles: true, Short: true, BaseOnly: false, WithIndefiniteArticle: false, null, IndicateHidden: false, Pronoun: false, SecondPerson: true, null) + ".");
            }
            */
        } //!--- public void StraightenUp(bool Message = false)

    } //!--- public class GigantismPlus : BaseDefaultEquipmentMutation
}