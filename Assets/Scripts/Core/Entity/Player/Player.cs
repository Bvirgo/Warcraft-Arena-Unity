﻿using Bolt;
using JetBrains.Annotations;
using UdpKit;
using UnityEngine;

namespace Core
{
    public sealed partial class Player : Unit
    {
        public new class CreateToken : Unit.CreateToken
        {
            public string PlayerName { private get; set; }

            public override void Read(UdpPacket packet)
            {
                base.Read(packet);

                PlayerName = packet.ReadString();
            }

            public override void Write(UdpPacket packet)
            {
                base.Write(packet);

                packet.WriteString(PlayerName);
            }

            public void Attached(Player player)
            {
                base.Attached(player);

                player.Name = PlayerName;
            }
        }

        public class ControlGainToken : IProtocolToken
        {
            public bool HasMovementControl { get; set; }

            public void Read(UdpPacket packet)
            {
                HasMovementControl = packet.ReadBool();
            }

            public void Write(UdpPacket packet)
            {
                packet.WriteBool(HasMovementControl);
            }

            public void ControlGained(Player player)
            {
                player.CharacterController.UpdateMovementControl(HasMovementControl);
            }
        }

        [SerializeField, UsedImplicitly, Header(nameof(Player)), Space(10)]
        private PlayerAI playerAI;

        private CreateToken createToken;
        private IPlayerState playerState;
        private string playerName;

        internal SpellController PlayerSpells { get; } = new SpellController();
        internal ClassInfo CurrentClass { get; private set; }
        internal new PlayerMovementInfo MovementInfo { get; private set; }

        internal PlayerAI PlayerAI => playerAI;
        internal override UnitAI AI => playerAI;
        internal override bool AutoScoped => true;

        public override string Name
        {
            get => playerName;
            internal set
            {
                playerName = value;

                if (IsOwner)
                {
                    playerState.PlayerName = value;
                    createToken.PlayerName = value;
                }
            } 
        }

        public IControllerInputProvider InputProvider { set => CharacterController.InputProvider = value; }

        protected override void HandleAttach()
        {
            base.HandleAttach();

            playerState = entity.GetState<IPlayerState>();
            createToken = (CreateToken)entity.AttachToken;
            createToken.Attached(this);

            MovementInfo = (PlayerMovementInfo)base.MovementInfo;
            CurrentClass = Balance.ClassesByType[ClassType];

            if(IsOwner)
                PlayerSpells.AddClassSpells(CurrentClass);

            HandleStateCallbacks(true);
        }

        protected override void HandleDetach()
        {
            HandleStateCallbacks(false);

            createToken = null;
            playerState = null;
            MovementInfo = null;

            base.HandleDetach();
        }

        protected override void HandleControlGained()
        {
            base.HandleControlGained();

            if (!IsOwner && IsController)
            {
                BoltEntity localClientMoveState = BoltNetwork.Instantiate(BoltPrefabs.Movement);
                localClientMoveState.SetScopeAll(false);
                localClientMoveState.SetScope(BoltNetwork.Server, true);
                localClientMoveState.AssignControl(BoltNetwork.Server);

                MovementInfo.AttachMoveState(localClientMoveState);
            }

            if (BoltEntity.ControlGainedToken is ControlGainToken controlGainToken)
                controlGainToken.ControlGained(this);
        }

        protected override void HandleControlLost()
        {
            MovementInfo.DetachMoveState(true);

            base.HandleControlLost();
        }

        protected override MovementInfo CreateMovementInfo(IUnitState unitState) => new PlayerMovementInfo(this, unitState);

        protected override void AddBehaviours(BehaviourController unitBehaviourController)
        {
            base.AddBehaviours(unitBehaviourController);

            unitBehaviourController.TryAddBehaviour(PlayerSpells);
        }

        public void Accept(IUnitVisitor visitor) => visitor.Visit(this);

        public void SetTarget(Unit target)
        {
            Attributes.UpdateTarget(newTarget: target, updateState: World.HasServerLogic);
        }

        public void Handle(SpellPlayerTeleportEvent teleportEvent)
        {
            Position = teleportEvent.TargetPosition;
            MovementInfo.RemoveMovementFlag(MovementFlags.Ascending);
        }

        public void Handle(PlayerSpeedRateChangedEvent speedChangeEvent)
        {
            Attributes.UpdateSpeedRate((UnitMoveType) speedChangeEvent.MoveType, speedChangeEvent.SpeedRate);
        }

        public void Handle(PlayerRootChangedEvent rootChangeEvent)
        {
            if (rootChangeEvent.Applied)
            {
                StopMoving();

                AddState(UnitControlState.Root);
            }
            else
                RemoveState(UnitControlState.Root);
        }

        public void Handle(PlayerMovementControlChanged movementControlChangeEvent)
        {
            CharacterController.UpdateMovementControl(movementControlChangeEvent.PlayerHasControl);
        }

        internal void AssignControl(BoltConnection boltConnection = null)
        {
            var controlToken = new ControlGainToken { HasMovementControl = MovementInfo.HasMovementControl };
            if (boltConnection == null)
                BoltEntity.TakeControl(controlToken);
            else
                BoltEntity.AssignControl(boltConnection, controlToken);
        }

        private void HandleStateCallbacks(bool add)
        {
            if (add)
            {
                if (!IsOwner)
                    playerState.AddCallback(nameof(playerState.PlayerName), OnPlayerNameChanged);
            }
            else
            {
                if (!IsOwner)
                    playerState.RemoveCallback(nameof(playerState.PlayerName), OnPlayerNameChanged);
            }
        }

        private void OnPlayerNameChanged()
        {
            Name = playerState.PlayerName;
        }
    }
}