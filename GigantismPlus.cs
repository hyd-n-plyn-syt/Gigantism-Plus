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
                /*
                if (ParentObject.HasTag("HunchedGiant"))
                {
                    return true;
                }
                int intProperty = ParentObject.GetIntProperty("HunchedGiant");
                if (intProperty > 0)
                {
                    return true;
                }
                if (intProperty < 0)
                {
                    return false;
                }
                */
                return ParentObject.HasPart<HunchedGiant>();
            }
            set
            {
                //ParentObject.SetIntProperty("HunchedGiant", value ? 1 : (-1));
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

        // needs to convert from Burrow Claws.
        public override void CollectStats(Templates.StatCollector stats, int Level)
        {
            
            int HunchedOverAV = GetHunchedOverAVModifier(Level);
            int HunchedOverDV = GetHunchedOverDVModifier(Level);
            int HunchedOverMS = GetHunchedOverMSModifier(Level);
            stats.Set("HunchedOverAV", "+" + HunchedOverAV);
            stats.Set("HunchedOverDV", HunchedOverDV);
            stats.Set("HunchedOverMS", HunchedOverMS);
        }

        // #############################
        // Something is bugging out here it seems that prevents the world from loading.
        public override bool WantEvent(int ID, int cascade)
        {
            /*
            if (!base.WantEvent(ID, cascade) && ID != SingletonEvent<AfterGameLoadedEvent>.ID && ID != PooledEvent<PartSupportEvent>.ID && ID != PooledEvent<PreferDefaultBehaviorEvent>.ID)
            {
                return ID == SingletonEvent<BeforeAbilityManagerOpenEvent>.ID;
            }
            */
            // Check if the ID parameter matches
            // BeforeLevelGainedEvent or
            // AfterLevelGainedEvent or
            // SingletonEvent<BeforeAbilityManagerOpenEvent>.
            return base.WantEvent(ID, cascade)
                || ID == BeforeLevelGainedEvent.ID
                || ID == AfterLevelGainedEvent.ID
                || ID == GetMaxCarriedWeightEvent.ID;
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
            return true;
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
                 + "and {{rules|" + GetFistHitBonus(Level) + "}} To-Hit\n"
                 + "{{rules|" + GetHunchedOverQNModifier(Level) + " QN}} and {{rules|" + GetHunchedOverMSModifier(Level) + " MS}} when {{g|Hunched Over}}";
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

            EnableActivatedAbilityID = AddMyActivatedAbility("Hunch Over", "CommandToggleGigantismPlusHunchOver", "Physical Mutations", null, "&#214", null, Toggleable: true, DefaultToggleState: false, ActiveToggle: true, IsAttack: false, IsRealityDistortionBased: false, IsWorldMapUsable: false);
            
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
            Registrar.Register("CommandToggleGigantismPlusHunchOver");
            base.Register(Object, Registrar);
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == "CommandToggleGigantismPlusHunchOver")
            {
                GameObject actor = this.ParentObject;
                ToggleMyActivatedAbility(EnableActivatedAbilityID, null, Silent: false, null);
                if (IsMyActivatedAbilityToggledOn(EnableActivatedAbilityID))
                {
                    UseEnergy(1000, "Physical Defect Mutation Gigantism Hunch Over");
                    IsHunchedGiant = true;
                    actor.RequirePart<HunchedGiant>();
                    actor.IsGiganticCreature = false;
                    if (!actor.IsGiganticCreature && IsHunchedGiant)
                    {
                        int baseWeight = actor.GetBodyWeight();
                        int weightFactor = (int)Math.Floor((double)_GiganticBodyWeightCache / baseWeight);
                        int _Weight = actor.Physics._Weight;
                        actor.Physics._Weight = _Weight + (int)Math.Round((double)((baseWeight * weightFactor) - baseWeight));
                        Debug.Message("baseWeight",baseWeight.ToString());
                        Debug.Message("_Weight", _Weight.ToString());
                        Debug.Message("weightFactor", weightFactor.ToString());
                        Debug.Message("Adjustment", Math.Round((double)(baseWeight * weightFactor) - baseWeight).ToString());
                        Debug.Message("New Weight", actor.Physics._Weight.ToString());
                    }
                    ParentObject.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_positiveVitality");
                    Debug.Message("Should be Hunched Over");
                }
                else
                {
                    UseEnergy(1000, "Physical Defect Mutation Gigantism Straighten Up");
                    actor.IsGiganticCreature = true;
                    actor.RemovePart<HunchedGiant>();
                    IsHunchedGiant = false;
                    if (actor.IsGiganticCreature && !IsHunchedGiant)
                    {
                        int baseWeight = actor.GetBodyWeight();
                        int WeightAdjustment = baseWeight - (int)Math.Floor((double)baseWeight / 5);
                        int _Weight = actor.Physics._Weight;
                        actor.Physics._Weight = _Weight - WeightAdjustment;
                        Debug.Message("baseWeight", baseWeight.ToString());
                        Debug.Message("_Weight", _Weight.ToString());
                        Debug.Message("Adjustment", WeightAdjustment.ToString());
                        Debug.Message("New Weight", actor.Physics._Weight.ToString());
                    }
                    Debug.Message("Should be Standing Tall");
                    ParentObject.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_negativeVitality");
                }
                Debug.Message("IsHunchedGiant", (IsHunchedGiant ? "true" : "false"));
                Debug.Message("HasPart<HunchedGiant>", (ParentObject.HasPart<HunchedGiant>() ? "true" : "false"));
                Debug.Message("IsGiganticCreature", (ParentObject.IsGiganticCreature ? "true" : "false"));
            }
            
            /*
            if (E.ID == "CommandToggleGigantismPlusHunchOver")
            {
                XRL.Messages.MessageQueue.AddPlayerMessage("Whoa! Someone Wants to Hunch!");
                StraightenUp();
                XRL.Messages.MessageQueue.AddPlayerMessage("Hey now. We were already standing tall!");
                UseEnergy(1000, "Physical Defect Mutation Gigantism Hunch Over");
                HunchOver(Message: true);
                XRL.Messages.MessageQueue.AddPlayerMessage("Whoa! Someone Wanted to Hunch!");
                The.Core.RenderBase();
            }*/

            return base.FireEvent(E);
        }

        // needs to be converted from Carapace.
        public void HunchOver(bool Message = false)
        {
            return;
            /*
            if (IsHunchedGiant)
            {
                return;
            }
            IsHunchedGiant = true;
            ParentObject.IsGiganticCreature = false;
            XRL.Messages.MessageQueue.AddPlayerMessage("Bendin' Ova!");

            /*
            TightFactor = ACModifier;
            ParentObject.Statistics["AV"].Bonus += TightFactor;
            ParentObject.Statistics["DV"].Penalty += 2;
            ParentObject.PlayWorldSound("Sounds/StatusEffects/sfx_statusEffect_positiveVitality");
            if (!Message)
            {
                return;
            }
            if (CarapaceObject == null)
            {
                MetricsManager.LogError(ParentObject.DebugName + " had no CarapaceObject for Carapace tighten message");
                if (ParentObject.IsPlayer())
                {
                    Popup.Show("You tighten your carapace. Your AV increases by {{G|" + TightFactor + "}}.");
                }
                else
                {
                    DidX("tighten", ParentObject.its + " carapace", null, null, null, null, null, UseFullNames: false, IndefiniteSubject: false, null, null, DescribeSubjectDirection: false, DescribeSubjectDirectionLate: false, AlwaysVisible: false, FromDialog: true);
                }
            }
            else if (ParentObject.IsPlayer())
            {
                Popup.Show("You tighten " + ParentObject.poss(CarapaceObject, Definite: true, null) + ". Your AV increases by {{G|" + TightFactor + "}}.");
            }
            else
            {
                DidXToY("tighten", CarapaceObject, null, null, null, null, null, null, UseFullNames: false, IndefiniteSubject: false, IndefiniteObject: false, IndefiniteObjectForOthers: false, PossessiveObject: false, null, ParentObject, null, DescribeSubjectDirection: false, DescribeSubjectDirectionLate: false, AlwaysVisible: false, FromDialog: true);
            }
            */

        } //!--- public void HunchOver(bool Message = false)

        // needs to be converted from Carapace.
        public void StraightenUp(bool Message = false)
        {
            return;
            /*
            if (!IsHunchedGiant)
            {
                return;
            }
            IsHunchedGiant = false;
            ParentObject.IsGiganticCreature = true;
            
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