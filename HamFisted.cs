using System;
using System.Collections.Generic;
using XRL.World.Anatomy;
using XRL.World.Parts.Mutation;
using XRL.World;

namespace XRL.World.Parts.Mutation
{
    [Serializable]
    public class HamFisted : BaseMutation
    {
        private static readonly string[] AffectedSlotTypes = new string[3] { "Hand", "Hands", "Missile Weapon" };

        public HamFisted()
        {
            DisplayName = "{{giant|Ham Fisted}}";
            base.Type = "Physical";
        }

        public override bool WantEvent(int ID, int cascade)
        {
            if (!base.WantEvent(ID, cascade) && ID != PooledEvent<GetSlotsRequiredEvent>.ID)
            {
                return false;
            }
            return true;
        }

        public override bool HandleEvent(GetSlotsRequiredEvent E)
        {
            if (Array.IndexOf(AffectedSlotTypes, E.SlotType) >= 0 && E.Actor == ParentObject)
            {
                E.Decreases++;
            }
            return base.HandleEvent(E);
        }

        public override bool Mutate(GameObject GO, int Level)
        {
            CheckAffected(GO, GO.Body);
            return base.Mutate(GO, Level);
        }

        public override bool Unmutate(GameObject GO)
        {
            CheckAffected(GO, GO.Body);
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

        public override bool AllowStaticRegistration()
        {
            return true;
        }
    }
}