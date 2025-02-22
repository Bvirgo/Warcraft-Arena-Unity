﻿using Core;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public sealed class TooltipItemSpell : TooltipItem<SpellInfo>
    {
        [SerializeField, UsedImplicitly] private RenderingReference rendering;
        [SerializeField, UsedImplicitly] private LocalizationReference localization;
        [SerializeField, UsedImplicitly] private TextMeshProUGUI spellDescription;
        [SerializeField, UsedImplicitly] private TextMeshProUGUI spellName;
        [SerializeField, UsedImplicitly] private Image spellIcon;

        private readonly object[] descriptionArguments = new object[SpellTooltipInfo.MaxArguments];
        private readonly object[] unknownArguments = { 'X', 'Y', 'Z', 'N', 'K' };

        public override void ModifyContent(SpellInfo spellInfo)
        {
            spellName.text = spellInfo.SpellName;
            spellIcon.sprite = rendering.SpellVisualSettingsById.ContainsKey(spellInfo.Id)
                ? rendering.SpellVisualSettingsById[spellInfo.Id].SpellIcon
                : rendering.DefaultSpellIcon;

            if (localization.TooltipInfoBySpell.TryGetValue(spellInfo, out SpellTooltipInfo tooltipInfo))
            {
                spellName.text = tooltipInfo.SpellNameString.Value;

                for (int i = 0; i < tooltipInfo.ArgumentSettings.Count; i++)
                    descriptionArguments[i] = tooltipInfo.ArgumentSettings[i].Resolve() ?? unknownArguments[i];

                spellDescription.text = string.Format(tooltipInfo.SpellDescriptionString.Value, descriptionArguments);
            }
        }
    }
}