﻿using System.Collections.Generic;
using Client.Spells;
using Common;
using Core;
using JetBrains.Annotations;
using UnityEngine;

using EventHandler = Common.EventHandler;

namespace Client
{
    [CreateAssetMenu(fileName = "Rendering Reference", menuName = "Game Data/Scriptable/Rendering", order = 1)]
    public partial class RenderingReference : ScriptableReferenceClient
    { 
        [SerializeField, UsedImplicitly] private Sprite defaultSpellIcon;
        [SerializeField, UsedImplicitly] private UnitRenderer unitRendererPrototype;
        [SerializeField, UsedImplicitly] private BalanceReference balance;
        [SerializeField, UsedImplicitly] private NameplateController nameplateController;
        [SerializeField, UsedImplicitly] private FloatingTextController floatingTextController;
        [SerializeField, UsedImplicitly] private SpellVisualController spellVisualController;
        [SerializeField, UsedImplicitly] private SelectionCircleController selectionCircleController;
        [SerializeField, UsedImplicitly] private List<SpellEffectSettings> spellEffectSettings;
        [SerializeField, UsedImplicitly] private List<AuraEffectSettings> auraEffectSettings;
        [SerializeField, UsedImplicitly] private UnitModelSettingsContainer modelSettingsContainer;
        [SerializeField, UsedImplicitly] private ClassTypeSpriteDictionary classIconsByClassType;

        private readonly Dictionary<int, SpellEffectSettings> spellVisualSettingsById = new Dictionary<int, SpellEffectSettings>();
        private readonly Dictionary<int, AuraEffectSettings> auraVisualSettingsById = new Dictionary<int, AuraEffectSettings>();
        private readonly Dictionary<int, UnitModelSettings> modelSettingsById = new Dictionary<int, UnitModelSettings>();
        private readonly Dictionary<ulong, UnitRenderer> unitRenderersById = new Dictionary<ulong, UnitRenderer>();
        private readonly List<UnitRenderer> unitRenderers = new List<UnitRenderer>();
        private readonly List<IUnitRendererHandler> unitRendererHandlers = new List<IUnitRendererHandler>();

        public Sprite DefaultSpellIcon => defaultSpellIcon;
        public IReadOnlyDictionary<int, SpellEffectSettings> SpellVisualSettingsById => spellVisualSettingsById;
        public IReadOnlyDictionary<int, AuraEffectSettings> AuraVisualSettingsById => auraVisualSettingsById;
        public IReadOnlyDictionary<int, UnitModelSettings> ModelSettingsById => modelSettingsById;
        public IReadOnlySerializedDictionary<ClassType, Sprite> ClassIconsByClassType => classIconsByClassType;

        protected override void OnRegistered()
        {
            base.OnRegistered();

            classIconsByClassType.Populate();
            for (int i = 0; i < modelSettingsContainer.ItemList.Count; i++)
                modelSettingsById.Add(modelSettingsContainer.ItemList[i].Id, modelSettingsContainer.ItemList[i]);

            auraEffectSettings.ForEach(visual => auraVisualSettingsById.Add(visual.AuraInfo.Id, visual));
            spellEffectSettings.ForEach(visual => spellVisualSettingsById.Add(visual.SpellInfo.Id, visual));
            spellEffectSettings.ForEach(visual => visual.Initialize());
        }

        protected override void OnUnregister()
        {
            spellEffectSettings.ForEach(visual => visual.Deinitialize());
            spellVisualSettingsById.Clear();
            auraVisualSettingsById.Clear();
            modelSettingsById.Clear();
            classIconsByClassType.Clear();

            base.OnUnregister();
        }

        protected override void OnUpdate(float deltaTime)
        {
            foreach (var unitRenderer in unitRenderers)
                unitRenderer.DoUpdate(deltaTime);

            nameplateController.DoUpdate(deltaTime);
            floatingTextController.DoUpdate(deltaTime);
            spellVisualController.DoUpdate(deltaTime);
        }
        
        protected override void OnWorldInitialized(WorldManager world)
        {
            base.OnWorldInitialized(world);

            if (world.HasClientLogic)
            {
                world.UnitManager.EventEntityAttached += OnEventEntityAttached;
                world.UnitManager.EventEntityDetach += OnEventEntityDetach;

                EventHandler.RegisterEvent<Unit, Unit, int, HitType>(EventHandler.GlobalDispatcher, GameEvents.SpellDamageDone, OnSpellDamageDone);
                EventHandler.RegisterEvent<Unit, Unit, int, bool>(EventHandler.GlobalDispatcher, GameEvents.SpellHealingDone, OnSpellHealingDone);
                EventHandler.RegisterEvent<Unit, Unit, SpellMissType>(EventHandler.GlobalDispatcher, GameEvents.SpellMissDone, OnSpellMiss);
                EventHandler.RegisterEvent<Unit, int, SpellProcessingToken>(EventHandler.GlobalDispatcher, GameEvents.SpellLaunched, OnSpellLaunch);
                EventHandler.RegisterEvent<Unit, int>(EventHandler.GlobalDispatcher, GameEvents.SpellHit, OnSpellHit);

                nameplateController.Initialize();
                floatingTextController.Initialize();
                spellVisualController.Initialize();
                selectionCircleController.Initialize();
            }
        }

        protected override void OnWorldDeinitializing(WorldManager world)
        {
            if (world.HasClientLogic)
            {
                nameplateController.Deinitialize();
                selectionCircleController.Deinitialize();
                floatingTextController.Deinitialize();
                spellVisualController.Deinitialize();

                EventHandler.UnregisterEvent<Unit, Unit, int, HitType>(EventHandler.GlobalDispatcher, GameEvents.SpellDamageDone, OnSpellDamageDone);
                EventHandler.UnregisterEvent<Unit, Unit, int, bool>(EventHandler.GlobalDispatcher, GameEvents.SpellHealingDone, OnSpellHealingDone);
                EventHandler.UnregisterEvent<Unit, Unit, SpellMissType>(EventHandler.GlobalDispatcher, GameEvents.SpellMissDone, OnSpellMiss);
                EventHandler.UnregisterEvent<Unit, int, SpellProcessingToken>(EventHandler.GlobalDispatcher, GameEvents.SpellLaunched, OnSpellLaunch);
                EventHandler.UnregisterEvent<Unit, int>(EventHandler.GlobalDispatcher, GameEvents.SpellHit, OnSpellHit);

                world.UnitManager.EventEntityAttached -= OnEventEntityAttached;
                world.UnitManager.EventEntityDetach -= OnEventEntityDetach;

                foreach (UnitRenderer unitRenderer in unitRenderers)
                    unitRenderer.Deinitialize();

                unitRenderersById.Clear();
                unitRenderers.Clear();
            }

            base.OnWorldDeinitializing(world);
        }

        protected override void OnPlayerControlGained(Player player)
        {
            base.OnPlayerControlGained(player);

            nameplateController.HandlePlayerControlGained();
            selectionCircleController.HandlePlayerControlGained();
        }

        protected override void OnPlayerControlLost(Player player)
        {
            nameplateController.HandlePlayerControlLost();
            selectionCircleController.HandlePlayerControlLost();

            base.OnPlayerControlLost(player);
        }

        private bool TryFind(Unit unit, out UnitRenderer unitRenderer)
        {
            return unitRenderersById.TryGetValue(unit.Id, out unitRenderer);
        }

        private void OnSpellDamageDone(Unit caster, Unit target, int damageAmount, HitType hitType)
        {
            if (!caster.IsController)
                return;

            if (!unitRenderersById.TryGetValue(target.Id, out UnitRenderer targetRenderer))
                return;

            floatingTextController.SpawnDamageText(targetRenderer, damageAmount, hitType);
        }

        private void OnSpellMiss(Unit caster, Unit target, SpellMissType missType)
        {
            if (!caster.IsController)
                return;

            if (!unitRenderersById.TryGetValue(target.Id, out UnitRenderer targetRenderer))
                return;

            floatingTextController.SpawnMissText(targetRenderer, missType);
        }

        private void OnSpellHealingDone(Unit caster, Unit target, int healingAmount, bool isCrit)
        {
            if (!caster.IsController)
                return;

            if (!unitRenderersById.TryGetValue(target.Id, out UnitRenderer targetRenderer))
                return;

            floatingTextController.SpawnHealingText(targetRenderer, healingAmount, isCrit);
        }

        private void OnSpellLaunch(Unit caster, int spellId, SpellProcessingToken processingToken)
        {
            if (!balance.SpellInfosById.TryGetValue(spellId, out SpellInfo spellInfo))
                return;

            if (!unitRenderersById.TryGetValue(caster.Id, out UnitRenderer casterRenderer))
                return;

            if (!spellInfo.HasAttribute(SpellCustomAttributes.CastWithoutAnimation))
                casterRenderer.TriggerInstantCast();

            if (!SpellVisualSettingsById.TryGetValue(spellId, out SpellEffectSettings spellVisuals))
                return;

            if (spellVisuals.VisualsByUsage.TryGetValue(EffectSpellSettings.UsageType.Projectile, out EffectSpellSettings settings))
                foreach (var entry in processingToken.ProcessingEntries)
                    if (unitRenderersById.TryGetValue(entry.Item1, out UnitRenderer targetRenderer))
                        spellVisualController.SpawnVisual(casterRenderer, targetRenderer, settings, processingToken.ServerFrame, entry.Item2);

            if (spellVisuals.VisualsByUsage.TryGetValue(EffectSpellSettings.UsageType.Cast, out EffectSpellSettings spellVisualEffect))
            {
                IEffectEntity effectEntity = spellVisualEffect.EffectSettings.PlayEffect(processingToken.Source + Vector3.up, caster.Rotation);
                if (effectEntity != null && !spellInfo.HasAttribute(SpellCustomAttributes.LaunchSourceIsExplicit))
                    effectEntity.ApplyPositioning(casterRenderer.TagContainer, spellVisualEffect);
            }

            if (spellInfo.ExplicitTargetType == SpellExplicitTargetType.Destination)
                if (spellVisuals.VisualsByUsage.TryGetValue(EffectSpellSettings.UsageType.Destination, out EffectSpellSettings destinationEffect))
                    destinationEffect.EffectSettings.PlayEffect(processingToken.Destination + Vector3.up, caster.Rotation);
        }

        private void OnSpellHit(Unit target, int spellId)
        {
            if (!unitRenderersById.TryGetValue(target.Id, out UnitRenderer targetRenderer))
                return;

            if (!SpellVisualSettingsById.TryGetValue(spellId, out SpellEffectSettings spellVisuals))
                return;

            if (spellVisuals.VisualsByUsage.TryGetValue(EffectSpellSettings.UsageType.Impact, out EffectSpellSettings spellVisualEffect))
            {
                IEffectEntity effectEntity = spellVisualEffect.EffectSettings.PlayEffect(target.Position, target.Rotation);
                effectEntity?.ApplyPositioning(targetRenderer.TagContainer, spellVisualEffect);
            }
        }

        private void OnEventEntityAttached(WorldEntity worldEntity)
        {
            if (worldEntity is Unit unitEntity)
            {
                var unitRenderer = GameObjectPool.Take(unitRendererPrototype);
                unitRenderer.Initialize(unitEntity);
                unitRenderersById.Add(unitEntity.Id, unitRenderer);
                unitRenderers.Add(unitRenderer);

                selectionCircleController.HandleRendererAttach(unitRenderer);

                foreach (IUnitRendererHandler handler in unitRendererHandlers)
                    handler.HandleUnitRendererAttach(unitRenderer);
            }
        }

        private void OnEventEntityDetach(WorldEntity worldEntity)
        {
            if (worldEntity is Unit unitEntity && unitRenderersById.TryGetValue(unitEntity.Id, out UnitRenderer unitRenderer))
            {
                spellVisualController.HandleRendererDetach(unitRenderer);
                selectionCircleController.HandleRendererDetach(unitRenderer);

                foreach (IUnitRendererHandler handler in unitRendererHandlers)
                    handler.HandleUnitRendererDetach(unitRenderer);

                unitRenderer.Deinitialize();
                unitRenderersById.Remove(unitEntity.Id);
                unitRenderers.Remove(unitRenderer);

                GameObjectPool.Return(unitRenderer, unitRenderer.gameObject == null);
            }
        }

        private void RegisterHandler(IUnitRendererHandler unitRendererHandler)
        {
            unitRendererHandlers.Add(unitRendererHandler);

            foreach (UnitRenderer unitRenderer in unitRenderers)
                unitRendererHandler.HandleUnitRendererAttach(unitRenderer);
        }

        private void UnregisterHandler(IUnitRendererHandler unitRendererHandler)
        {
            foreach (UnitRenderer unitRenderer in unitRenderers)
                unitRendererHandler.HandleUnitRendererDetach(unitRenderer);

            unitRendererHandlers.Remove(unitRendererHandler);
        }

#if UNITY_EDITOR
        [ContextMenu("Collect Spell Effects"), UsedImplicitly]
        private void CollectSpellEffectSettings()
        {
            spellEffectSettings.Clear();

            foreach (string guid in UnityEditor.AssetDatabase.FindAssets($"t:{nameof(SpellEffectSettings)}", null))
                spellEffectSettings.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<SpellEffectSettings>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)));
        }

        [ContextMenu("Collect Aura Effects"), UsedImplicitly]
        private void CollectAuraEffectSettings()
        {
            auraEffectSettings.Clear();

            foreach (string guid in UnityEditor.AssetDatabase.FindAssets($"t:{nameof(AuraEffectSettings)}", null))
                auraEffectSettings.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<AuraEffectSettings>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)));
        }

        [ContextMenu("Collect Everything"), UsedImplicitly]
        private void CollectEverything()
        {
            CollectAuraEffectSettings();
            CollectSpellEffectSettings();
        }
#endif
    }
}
