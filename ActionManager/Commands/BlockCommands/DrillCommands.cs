using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        class DrillActionCommand : BaseBlockActionCommand<IMyShipDrill>
        {
            enum DrillAction
            {
                none,
                toggle,
                on,
                off,
            }

            DrillAction _action = DrillAction.none;

            protected override bool GetAction(string action)
            {
                return Enum.TryParse(action, out _action);
            }

            protected override void ApplyAction(IMyShipDrill block)
            {
                switch (_action)
                {
                    case DrillAction.toggle:
                        block.Enabled = !block.Enabled;
                        break;
                    case DrillAction.on:
                        block.Enabled = true;
                        break;
                    case DrillAction.off:
                        block.Enabled = false;
                        break;
                }
            }
        }

        class DrillPropertyCommand : BaseBlockPropertyCommand<IMyShipDrill>
        {
            enum DrillProperties
            {
                none,
                enabled,
                activated,
            }

            DrillProperties _property = DrillProperties.none;

            protected override bool HandleProperty(IMyShipDrill block)
            {
                if (CompareMode == ComparisonModes.set)
                {
                    switch (_property)
                    {
                        case DrillProperties.enabled:
                            block.Enabled = _boolValue;
                            return true;
                        default:
                            return false;
                    }
                }
                else
                {
                    switch (_property)
                    {
                        case DrillProperties.enabled:
                            return CheckProperty(block.Enabled);
                        case DrillProperties.activated:
                            return CheckProperty(block.IsActivated);
                        default:
                            return false;
                    }
                }
            }

            protected override bool GetProperty(string property)
            {
                return Enum.TryParse(property, out _property);
            }

            protected override bool GetValue(string value)
            {
                switch (_property)
                {
                    case DrillProperties.enabled:
                    case DrillProperties.activated:
                        return bool.TryParse(value, out _boolValue);
                    default:
                        return false;
                }
            }
        }
    }
}
