using System;
using System.Collections.Generic;
using System.Linq;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;
using Mods.GigantismPlus;

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

        public static readonly string HUNCH_OVER_COMMAND_NAME = "CommandToggleGigantismPlusHunchOver";

        public Guid EnableActivatedAbilityID = Guid.Empty;

        public int HunchedOverAVModifier;

        public int HunchedOverDVModifier;

        public int HunchedOverQNModifier;

        public int HunchedOverMSModifier;

        private int _GiganticBodyWeightCache = -1;

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

        public static int GetHunchedOverQNModifier(int Level)
        {
            return Math.Min(-70 + (int)Math.Floor((double)Level * 10.0),-10);
        }

        public static int GetHunchedOverMSModifier(int Level)
        {
            return Math.Min(-70 + (int)Math.Floor((double)Level * 10.0),-10);
        }

        public bool IsHunchedGiant
        {
            get
            {
                return ParentObject.HasPart<HunchedGiant>();
            }
            set
            {
                if (value)
                {
                    ParentObject.RequirePart<HunchedGiant>();
                }
                else
                {
                    ParentObject.RemovePart<HunchedGiant>();
                }
            }
        }

        private bool IsHunchFree = false;

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
            base.Type = "Physical";
        }

        public override bool CanLevel() { return true; } // Enable leveling

        public override bool GeneratesEquipment() { return true; }

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

        public override void CollectStats(Templates.StatCollector stats, int Level)
        {
            
            int HunchedOverAV = GetHunchedOverAVModifier(Level);
            int HunchedOverDV = GetHunchedOverDVModifier(Level);
            int HunchedOverMS = GetHunchedOverMSModifier(Level);
            stats.Set("HunchedOverAV", "+" + HunchedOverAV);
            stats.Set("HunchedOverDV", HunchedOverDV);
            stats.Set("HunchedOverMS", HunchedOverMS);
        }

        public override bool WantEvent(int ID, int cascade)
        {
            /*
            if (!base.WantEvent(ID, cascade) && ID != SingletonEvent<AfterGameLoadedEvent>.ID && ID != PooledEvent<PartSupportEvent>.ID && ID != PooledEvent<PreferDefaultBehaviorEvent>.ID)
            {
                return ID == SingletonEvent<BeforeAbilityManagerOpenEvent>.ID;
            }
            */
            // Check if the ID parameter matches
            // or if a Wanted Event.ID comes through
            // SingletonEvent<BeforeAbilityManagerOpenEvent>.
            return base.WantEvent(ID, cascade)
                || ID == BeforeLevelGainedEvent.ID
                || ID == AfterLevelGainedEvent.ID
                || ID == GetMaxCarriedWeightEvent.ID
                || ID == CanEnterInteriorEvent.ID
                || ID == InventoryActionEvent.ID;
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

        public override bool HandleEvent(GetMaxCarriedWeightEvent E)
        {
            if (!ParentObject.IsGiganticCreature && IsHunchedGiant)
            {
                E.AdjustWeight(2.0);
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(CanEnterInteriorEvent E)
        {
            GameObject actor = E.Actor;
            if (actor.IsGiganticCreature) 
            {
                IsHunchFree = true;
                CommandEvent.Send(actor, HUNCH_OVER_COMMAND_NAME);
                bool check = CanEnterInteriorEvent.Check(E.Actor, E.Object, E.Interior, ref E.Status, ref E.Action, ref E.ShowMessage);
                E.Status = check ? 0 : E.Status;

                XRL.UI.Popup.Show("You try to squeeze into the space.");
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(InventoryActionEvent E)
        {
            if (E.Command == "EnterInterior")
            {
                GameObject actor = E.Actor;
                if (actor.IsGiganticCreature)
                {
                    IsHunchFree = true;
                    CommandEvent.Send(actor, HUNCH_OVER_COMMAND_NAME);
                }
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(BeforeAbilityManagerOpenEvent E)
        {
            // DescribeMyActivatedAbility(EnableActivatedAbilityID, CollectStats);
            return base.HandleEvent(E);
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
                 + "and {{rules|" + GetFistHitBonus(Level) + "}} To-Hit\n"; /*
                 + "{{rules|" + GetHunchedOverQNModifier(Level) + " QN}} and {{rules|" + GetHunchedOverMSModifier(Level) + " MS}} when {{g|Hunched Over}}"; */
        }

        public override bool Mutate(GameObject GO, int Level)
        {
            Body body = GO.Body;
            if (body != null)
            {
                GO.IsGiganticCreature = true; // Enable the Gigantic flag
                _GiganticBodyWeightCache = GO.GetBodyWeight();
                
                foreach (BodyPart hand in body.GetParts())
                {
                    if (hand.Type == "Hand")
                    {
                        AddGiganticFistTo(hand);
                    }
                }
            }

            EnableActivatedAbilityID = AddMyActivatedAbility("Hunch Over", HUNCH_OVER_COMMAND_NAME, "Physical Mutations", null, "&#214", null, Toggleable: true, DefaultToggleState: false, ActiveToggle: true, IsAttack: false, IsRealityDistortionBased: false, IsWorldMapUsable: false);
            
            return base.Mutate(GO, Level);
        }

        public override bool Unmutate(GameObject GO)
        {
            if (GO != null)
            {
                GO.IsGiganticCreature = false; // Revert the Gigantic flag
            }

            CleanUpMutationEquipment(GO, ref GiganticFistObject);
            RemoveMyActivatedAbility(ref EnableActivatedAbilityID);
            ParentObject.RemovePart<HunchedGiant>();
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

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(HUNCH_OVER_COMMAND_NAME);
            base.Register(Object, Registrar);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == HUNCH_OVER_COMMAND_NAME)
            {
                GameObject actor = this.ParentObject;
                if (actor.CurrentZone.GetBlueprint().Name == "Control pit" && !actor.IsGiganticCreature)
                {
                    XRL.UI.Popup.Show("This space is too small for you to stand up upright!");
                    return base.FireEvent(E);
                }
                ToggleMyActivatedAbility(EnableActivatedAbilityID, null, Silent: true, null);
                int EnergyCost = IsHunchFree ? 0 : 1000;
                if (IsMyActivatedAbilityToggledOn(EnableActivatedAbilityID))
                {
                    // Hunching
                    UseEnergy(EnergyCost, "Physical Defect Mutation Gigantism Hunch Over");
                    IsHunchedGiant = true;
                    actor.RequirePart<HunchedGiant>();
                    _GiganticBodyWeightCache = actor.GetBodyWeight();
                    actor.IsGiganticCreature = false;
                    if (!actor.IsGiganticCreature && IsHunchedGiant)
                    {
                        int baseWeight = actor.GetBodyWeight();
                        int weightFactor = (int)Math.Floor((double)_GiganticBodyWeightCache / baseWeight);
                        int _Weight = actor.Physics._Weight;
                        actor.Physics._Weight = _Weight + (int)Math.Round((double)((baseWeight * weightFactor) - baseWeight));
                        Debug.Entry(3,"baseWeight",baseWeight.ToString());
                        Debug.Entry(3,"_Weight", _Weight.ToString());
                        Debug.Entry(3,"weightFactor", weightFactor.ToString());
                        Debug.Entry(3,"Adjustment", Math.Round((double)(baseWeight * weightFactor) - baseWeight).ToString());
                        Debug.Entry(3,"New Weight", actor.Physics._Weight.ToString());

                        ParentObject.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_positiveVitality");
                        XRL.UI.Popup.Show("You hunch over, allowing you access to smaller spaces.");

                        ActivatedAbilityEntry abilityEntry = actor.ActivatedAbilities.GetAbility(EnableActivatedAbilityID);
                        abilityEntry.DisplayName = "Hunched Over";
                    }
                    
                    Debug.Entry(1,"Should be Hunched Over");
                }
                else
                {
                    // Standing upright
                    UseEnergy(EnergyCost, "Physical Defect Mutation Gigantism Stand Tall");
                    actor.IsGiganticCreature = true;
                    actor.RemovePart<HunchedGiant>();
                    IsHunchedGiant = false;
                    if (actor.IsGiganticCreature && !IsHunchedGiant)
                    {
                        int baseWeight = actor.GetBodyWeight();
                        int WeightAdjustment = baseWeight - (int)Math.Floor((double)baseWeight / 5);
                        int _Weight = actor.Physics._Weight;
                        actor.Physics._Weight = _Weight - WeightAdjustment;
                        Debug.Entry(3,"baseWeight", baseWeight.ToString());
                        Debug.Entry(3,"_Weight", _Weight.ToString());
                        Debug.Entry(3,"Adjustment", WeightAdjustment.ToString());
                        Debug.Entry(3,"New Weight", actor.Physics._Weight.ToString());

                        ParentObject.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_negativeVitality");
                        XRL.UI.Popup.Show("You stand tall, relaxing into your immense stature.");

                        ActivatedAbilityEntry abilityEntry = actor.ActivatedAbilities.GetAbility(EnableActivatedAbilityID);
                        abilityEntry.DisplayName = "Hunch Over";
                    }
                    
                    Debug.Entry(1, "Should be Standing Tall");
                }
                IsHunchFree = false;
                Debug.Entry(2,"IsHunchedGiant", (IsHunchedGiant ? "true" : "false"));
                Debug.Entry(2,"HasPart<HunchedGiant>", (ParentObject.HasPart<HunchedGiant>() ? "true" : "false"));
                Debug.Entry(2,"IsGiganticCreature", (ParentObject.IsGiganticCreature ? "true" : "false"));
            }
            // The.Core.RenderBase();
            return base.FireEvent(E);
        }

        // Want to move the bulk of the Active Ability here.
        public void HunchOver()
        {
            return;
        } //!--- public void HunchOver(bool Message = false)

        // Want to move the bulk of the Active Ability here.
        public void StraightenUp()
        {
            return;
        } //!--- public void StraightenUp(bool Message = false)

    } //!--- public class GigantismPlus : BaseDefaultEquipmentMutation

}