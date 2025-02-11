using System;
using System.Collections.Generic;
using XRL.World.Anatomy;
using XRL.World.Parts.Mutation;
using XRL.World;
using Mods.GigantismPlus;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class ElongatedPaws : BaseDefaultEquipmentMutation
    {
        private static readonly string[] AffectedSlotTypes = new string[3] { "Hand", "Hands", "Missile Weapon" };

        public GameObject ElongatedPawObject;

        public int StrengthModifier => CalculateStrengthModifier(ParentObject.Statistics["Strength"].BaseValue);

        public ElongatedPaws()
        {
            DisplayName = "{{giant|Elongated Paws}}";
            base.Type = "Physical";
        }

        public override bool CanLevel()
        {
            return false;
        }

        public override string GetDescription()
        {
            return "An array of long, slender, digits fan from your paws, fluttering with composed and expert precision.\n\n"
                 + "You have {{giant|elongated paws}}, which are unusually large and end in spindly fingers.\n"
                 + "Their odd shape and size allow you to {{rules|equip}} equipment {{rules|on your hands}} and {{rules|wield}} melee and missile weapons {{gigantic|a size bigger}} than you are as though they were your size.\n\n"
                 + "Your {{giant|elongated paws}} count as natural short blades {{rules|\x1A}}{{rules|4}}{{k|/\xEC}} {{r|\x03}}{{z|1}}{{w|d}}{{z|4}}{{w|+}}{{rules|Current Strength Modifier}}\n\n"
                 + "+{{rules|100}} reputation with {{w|Barathrumites}}";
        }

        public override string GetLevelText(int Level)
        {
            return "";
        }

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade)
                || ID == PooledEvent<GetSlotsRequiredEvent>.ID
                || ID == GetExtraPhysicalFeaturesEvent.ID
                || ID == StatChangeEvent.ID;
        }

        public override bool HandleEvent(GetSlotsRequiredEvent E)
        {
            if (Array.IndexOf(AffectedSlotTypes, E.SlotType) >= 0 && E.Actor == ParentObject)
            {
                E.Decreases++;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(GetExtraPhysicalFeaturesEvent E)
        {
            E.Features.Add("{{giant|elongated paws}}");
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(StatChangeEvent E)
        {
            Body body = E.Object.Body;
            foreach (BodyPart hand in body.GetParts())
            {
                if (hand.Type == "Hand")
                {
                    AddElongatedPawTo(hand);
                }
            }
            return base.HandleEvent(E);
        }


        public override bool Mutate(GameObject GO, int Level)
        {
            Body body = GO.Body;
            if (body != null)
            {
                foreach (BodyPart hand in body.GetParts())
                {
                    if (hand.Type == "Hand")
                    {
                        AddElongatedPawTo(hand);
                    }
                }
            }
            return base.Mutate(GO, Level);
        }

        public override bool Unmutate(GameObject GO)
        {
            Body body = GO.Body;
            if (body != null)
            {
                foreach (BodyPart hand in body.GetParts())
                {
                    if (hand.Type == "Hand" && hand.DefaultBehavior != null && hand.DefaultBehavior == ElongatedPawObject)
                    {
                        hand.DefaultBehavior = null;
                    }
                }
            }
            CheckAffected(GO, body);
            CleanUpMutationEquipment(GO, ref ElongatedPawObject);
            return base.Unmutate(GO);
        }

        public void CheckAffected(GameObject Actor, Body Body)
        {
            if (Actor == null || Body == null)
            {
                return;
            }
            List<GameObject> list = Event.NewGameObjectList();
            foreach (BodyPart item in Body.LoopParts())
            {
                if (Array.IndexOf(AffectedSlotTypes, item.Type) < 0)
                {
                    continue;
                }
                GameObject equipped = item.Equipped;
                if (equipped != null && !list.Contains(equipped))
                {
                    list.Add(equipped);
                    int partCountEquippedOn = Body.GetPartCountEquippedOn(equipped);
                    int slotsRequiredFor = equipped.GetSlotsRequiredFor(Actor, item.Type);
                    if (partCountEquippedOn != slotsRequiredFor && item.TryUnequip(Silent: true, SemiForced: true) && partCountEquippedOn > slotsRequiredFor)
                    {
                        equipped.SplitFromStack();
                        item.Equip(equipped, 0, Silent: true, ForDeepCopy: false, Forced: false, SemiForced: true);
                    }
                }
            }
        }

        public int CalculateStrengthModifier(int value)
        {
            return (int)Math.Floor((double)(value - 16 + ((value >= 16) ? 0 : 1)) / 2);
        }

        public void AddElongatedPawTo(BodyPart part)
        {
            if (part != null && part.Type == "Hand")
            {
                if (ElongatedPawObject == null)
                {
                    ElongatedPawObject = GameObjectFactory.Factory.CreateObject("ElongatedPaw");
                }
                part.DefaultBehavior = ElongatedPawObject;
                MeleeWeapon elongatedPawWeapon = ElongatedPawObject.GetPart<MeleeWeapon>();
                elongatedPawWeapon.BaseDamage = $"1d4+{StrengthModifier}";
            }
        }

        public override void OnRegenerateDefaultEquipment(Body body)
        {
            foreach (BodyPart hand in body.GetParts())
            {
                if (hand.Type == "Hand")
                {
                    AddElongatedPawTo(hand);
                }
            }

            base.OnRegenerateDefaultEquipment(body);
        }

        public override bool AllowStaticRegistration()
        {
            return true;
        }
    }
}