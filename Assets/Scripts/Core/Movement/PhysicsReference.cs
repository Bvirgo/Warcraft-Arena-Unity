﻿using Common;
using JetBrains.Annotations;
using UnityEngine;

namespace Core
{
    [CreateAssetMenu(fileName = "Physics Reference", menuName = "Game Data/Scriptable/Physics", order = 3)]
    public class PhysicsReference : ScriptableReference
    {
        public static class Mask
        {
            public static int Characters { get; internal set; }
            public static int Ground { get; internal set; }
        }

        public static class Layer
        {
            public static int Characters { get; internal set; }
            public static int Ground { get; internal set; }
        }

        [SerializeField, UsedImplicitly]
        private PhysicMaterial groundedUnitMaterial;
        [SerializeField, UsedImplicitly]
        private PhysicMaterial slidingUnitMaterial;

        public PhysicMaterial GroundedMaterial => groundedUnitMaterial;
        public PhysicMaterial SlidingMaterial => slidingUnitMaterial;

        protected override void OnRegistered()
        {
            Layer.Characters = LayerMask.NameToLayer("Characters");
            Layer.Ground = LayerMask.NameToLayer("Ground");

            Mask.Characters = 1 << Layer.Characters;
            Mask.Ground = 1 << Layer.Ground;
        }

        protected override void OnUnregister()
        {
            Layer.Characters = 0;
            Layer.Ground = 0;

            Mask.Characters = 0;
            Mask.Ground = 0;
        }
    }
}
