﻿using JetBrains.Annotations;
using UnityEngine;

namespace Core
{
    public abstract class AuraEffectInfo : ScriptableObject
    {
        [Header("Aura Effect")]
        [SerializeField, UsedImplicitly] private int maxEffectiveCharges;
        [SerializeField, UsedImplicitly] private SpellMechanics mechanics;

        public abstract float Value { get; }
        public abstract AuraEffectType AuraEffectType { get; }
        public SpellMechanics Mechanics => mechanics;

        internal abstract AuraEffect CreateEffect(Aura aura, Unit caster, int index);
    }
}
