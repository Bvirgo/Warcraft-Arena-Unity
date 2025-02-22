﻿using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using Common;
using Core.Conditions;

namespace Client
{
    [UsedImplicitly, CreateAssetMenu(fileName = "Input Action Global", menuName = "Player Data/Input/Input Action Global", order = 1)]
    public class InputActionGlobal : ScriptableObject
    {
        [SerializeField, UsedImplicitly] private InputReference input;
        [SerializeField, UsedImplicitly] private InputAction action;
        [SerializeField, UsedImplicitly] private HotkeyInputItem hotkey;
        [SerializeField, UsedImplicitly] private List<InputActionGlobal> blockedBy;
        [SerializeField, UsedImplicitly] private List<Condition> inactiveWhen;

        private bool IsApplicable
        {
            get
            {
                foreach (Condition condition in inactiveWhen)
                    if (condition.IsApplicableAndValid(input.Player))
                        return false;

                return true;
            }
        }

        public void Register()
        {
            EventHandler.RegisterEvent<HotkeyState>(hotkey, GameEvents.HotkeyStateChanged, OnHotkeyStateChanged);
        }

        public void Unregister()
        {
            EventHandler.UnregisterEvent<HotkeyState>(hotkey, GameEvents.HotkeyStateChanged, OnHotkeyStateChanged);
        }

        private void OnHotkeyStateChanged(HotkeyState state)
        {
            if (state == HotkeyState.Released || blockedBy.Exists(blocker => blocker.IsApplicable))
                return;

            action.Execute();
        }
    }
}
