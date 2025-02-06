using System;
using System.Collections.Generic;
using XRL.World;
using XRL.World.Anatomy;
using XRL.World.Parts;

namespace Mods.GigantismPlus
{
    [Serializable]
    public class HunchedGiant : IActivePart
    {
        public HunchedGiant()
        {
            WorksOnSelf = true;
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
            
            if (!E.Actor.IsGiganticCreature && IsObjectActivePartSubject(E.Actor) && IsReady(UseCharge: true, IgnoreCharge: false, IgnoreLiquid: false, IgnoreBootSequence: false, IgnoreBreakage: false, IgnoreRust: false, IgnoreEMP: false, IgnoreRealityStabilization: false, IgnoreSubject: false, IgnoreLocallyDefinedFailure: false, 1, null, UseChargeIfUnpowered: false, 0L, null))
            {
                E.Decreases++;
                if (!E.Object.IsGiganticEquipment)
                {
                    E.CanBeTooSmall = true;
                }
            }
            return base.HandleEvent(E);
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
    } //!--- public class HunchedGiant : IActivePart
}