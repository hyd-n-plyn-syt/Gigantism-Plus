using System;
using System.Collections.Generic;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Anatomy;

namespace XRL.World.Parts
{
    [Serializable]
    public class MassiveExoframe : IActivePart
    {
        [Serializable]
        private class CompactedExoframe : IPart { }  // Inner marker class

        public new bool WorksOnSelf = true;
        public new bool WorksOnImplantee = true;

        public static readonly string COMPACT_MODE_COMMAND_NAME = "CommandToggleExoframeCompactMode";
        public Guid EnableActivatedAbilityID = Guid.Empty;

        private bool IsCompactModeFree = false;
        private int _compactModeEnergyCost = 500;
        
        public int CompactModeEnergyCost
        {
            get
            {
                if (IsCompactModeFree)
                {
                    IsCompactModeFree = false;
                    return 0;
                }
                return _compactModeEnergyCost;
            }
            private set => _compactModeEnergyCost = value;
        }

        // Stats modified by compact mode
        public int CompactModeAVModifier = 4;
        public int CompactModeDVModifier = -6;
        public int CompactModeQNModifier = -50;
        public int CompactModeMSModifier = -50;

        private bool _isGiganticCreature = false;
        public bool IsGiganticCreature
        {
            get => _isGiganticCreature;
            private set
            {
                _isGiganticCreature = value;
                ParentObject.IsGiganticCreature = value;
                if (IsPseudoGiganticCreature == value)
                {
                    IsPseudoGiganticCreature = !value;
                }
            }
        }

        private bool _isPseudoGiganticCreature = false;
        public bool IsPseudoGiganticCreature
        {
            get => _isPseudoGiganticCreature;
            set
            {
                _isPseudoGiganticCreature = value;
                if (value) ParentObject.RequirePart<CompactedExoframe>();
                else ParentObject.RemovePart<CompactedExoframe>();

                if (IsGiganticCreature == value)
                {
                    IsGiganticCreature = !value;
                }
            }
        }

        public GameObject _manipulatorObject;

        public override bool WantEvent(int ID, int cascade)
        {
            return base.WantEvent(ID, cascade)
                || ID == ImplantedEvent.ID
                || ID == UnimplantedEvent.ID
                || ID == CanEnterInteriorEvent.ID
                || ID == GetSlotsRequiredEvent.ID;
        }

        public override bool HandleEvent(GetSlotsRequiredEvent E)
        {
            if (IsObjectActivePartSubject(E.Actor) && base.IsReady(true, false, false, false, false, false, false, false, false, false, 1, null, false, 0L, null))
            {
                E.Decreases++;
            }
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(ImplantedEvent E)
        {
            E.Implantee.IsGiganticCreature = true;

            // Create manipulator weapon if needed
            if (_manipulatorObject == null)
            {
                _manipulatorObject = GameObjectFactory.Factory.CreateObject("MassiveExoframeManipulatorA");
            }
            
            // Apply to hands
            Body body = E.Part?.ParentBody;
            if (body != null)
            {
                foreach (BodyPart part in body.GetParts())
                {
                    if (part.Type == "Hand")
                    {
                        part.DefaultBehavior = _manipulatorObject;
                    }
                }
            }

            if (!ParentObject.HasPart<Vehicle>())
            {
                EnableActivatedAbilityID = E.Implantee.AddActivatedAbility(
                    "{{C|{{W|[}}Standard{{W|]}}/Compact}}",
                    COMPACT_MODE_COMMAND_NAME,
                    "Cybernetics",
                    "Toggle between standard and compact configurations",
                    "&#214",
                    null,
                    true,
                    false,
                    true,
                    false,
                    false,
                    false
                );

                var abilityEntry = E.Implantee.ActivatedAbilities.GetAbility(EnableActivatedAbilityID);
                abilityEntry.DisplayName = "{{C|{{W|[}}Standard{{W|]}}\nCompact\n}}";
            }

            CheckEquipment(E.Implantee, E.Part?.ParentBody);
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(UnimplantedEvent E)
        {
            // Remove manipulator from hands
            Body body = E.Part?.ParentBody;
            if (body != null)
            {
                foreach (BodyPart part in body.GetParts())
                {
                    if (part.Type == "Hand" && part.DefaultBehavior == _manipulatorObject)
                    {
                        part.DefaultBehavior = null;
                    }
                }
            }

            DisengageCompactMode();
            E.Implantee.IsGiganticCreature = false;

            if (EnableActivatedAbilityID != Guid.Empty)
                E.Implantee.RemoveActivatedAbility(ref EnableActivatedAbilityID);

            CheckEquipment(E.Implantee, E.Part?.ParentBody);
            return base.HandleEvent(E);
        }

        public override bool HandleEvent(CanEnterInteriorEvent E)
        {
            if (ParentObject == E.Object)
                return base.HandleEvent(E);

            GameObject actor = E.Actor;
            if (actor != null && actor.IsGiganticCreature && !ParentObject.HasPart<Vehicle>())
            {
                IsCompactModeFree = true;
                CommandEvent.Send(actor, COMPACT_MODE_COMMAND_NAME);
                bool check = CanEnterInteriorEvent.Check(E.Actor, E.Object, E.Interior, ref E.Status, ref E.Action, ref E.ShowMessage);
                E.Status = check ? 0 : E.Status;
                Popup.Show("Your exoframe compacts itself to fit into the space.");
            }
            return base.HandleEvent(E);
        }

        public void CheckEquipment(GameObject Actor, Body Body)
        {
            if (Actor == null || Body == null)
                return;

            List<GameObject> list = Event.NewGameObjectList();
            foreach (BodyPart bodyPart in Body.LoopParts())
            {
                GameObject equipped = bodyPart.Equipped;
                if (equipped != null && !list.Contains(equipped))
                {
                    list.Add(equipped);
                    int partCountEquippedOn = Body.GetPartCountEquippedOn(equipped);
                    int slotsRequiredFor = equipped.GetSlotsRequiredFor(Actor, bodyPart.Type, true);
                    if (partCountEquippedOn != slotsRequiredFor && bodyPart.TryUnequip(true, true, false, false) && partCountEquippedOn > slotsRequiredFor)
                    {
                        equipped.SplitFromStack();
                        bodyPart.Equip(equipped, new int?(0), true, false, false, true);
                    }
                }
            }
        }

        public void EngageCompactMode(bool Message = false)
        {
            GameObject actor = ParentObject;
            if (IsPseudoGiganticCreature)
                return;

            IsPseudoGiganticCreature = true;

            if (!IsGiganticCreature && IsPseudoGiganticCreature)
            {
                ParentObject.UseEnergy(CompactModeEnergyCost, "Cybernetic Exoframe Compact Mode");

                // Apply stat modifications
                actor.ModIntProperty("AV", CompactModeAVModifier);
                actor.ModIntProperty("DV", CompactModeDVModifier);
                actor.ModIntProperty("Quickness", CompactModeQNModifier);
                actor.ModIntProperty("MoveSpeed", CompactModeMSModifier);

                actor.PlayWorldSound("Sounds/Machines/sfx_machine_hydraulics");
                if (Message)
                    Popup.Show("Your exoframe compacts with hydraulic whirs and mechanical clicks.");

                var abilityEntry = actor.ActivatedAbilities.GetAbility(EnableActivatedAbilityID);
                abilityEntry.DisplayName = "{{C|Standard\n{{W|[}}Compact{{W|]}}\n}}";
            }
        }

        public void DisengageCompactMode(bool Message = false)
        {
            GameObject actor = ParentObject;
            if (!IsPseudoGiganticCreature)
                return;

            IsPseudoGiganticCreature = false;

            if (IsGiganticCreature && !IsPseudoGiganticCreature)
            {
                ParentObject.UseEnergy(CompactModeEnergyCost, "Cybernetic Exoframe Standard Mode");

                // Revert stat modifications
                actor.ModIntProperty("AV", -CompactModeAVModifier);
                actor.ModIntProperty("DV", -CompactModeDVModifier);
                actor.ModIntProperty("Quickness", -CompactModeQNModifier);
                actor.ModIntProperty("MoveSpeed", -CompactModeMSModifier);

                actor.PlayWorldSound("Sounds/Machines/sfx_machine_hydraulics");
                if (Message)
                    Popup.Show("Your exoframe expands with hydraulic whirs and mechanical clicks.");

                var abilityEntry = actor.ActivatedAbilities.GetAbility(EnableActivatedAbilityID);
                abilityEntry.DisplayName = "{{C|{{W|[}}Standard{{W|]}}\nCompact\n}}";
            }
        }

        public override bool FireEvent(Event E)
        {
            if (E.ID == COMPACT_MODE_COMMAND_NAME)
            {
                GameObject actor = ParentObject;
                if (actor.CurrentZone.ZoneWorld == "Interior" && !actor.IsGiganticCreature)
                {
                    Popup.Show("This space is too small for you to stand upright!");
                    return base.FireEvent(E);
                }

                if (ParentObject.HasPart<Vehicle>())
                    return base.FireEvent(E);

                // Use IActivePart's toggle methods
                if (ToggleMyActivatedAbility(EnableActivatedAbilityID))
                    EngageCompactMode(true);
                else
                    DisengageCompactMode(true);

                The.Core.RenderBase();
            }
            return base.FireEvent(E);
        }

        public override bool AllowStaticRegistration()
        {
            return true;
        }

        public override void Register(GameObject Object, IEventRegistrar Registrar)
        {
            Registrar.Register(COMPACT_MODE_COMMAND_NAME);
            base.Register(Object, Registrar);
        }
    }
}
