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
        class MergeActionCommand : BaseBlockActionCommand<IMyShipMergeBlock>
        {
            enum MergeAction
            {
                none,
                toggle,
                on,
                off,
            }

            MergeAction _action = MergeAction.none;

            protected override bool GetAction(string action)
            {
                return Enum.TryParse(action, out _action);
            }

            protected override void ApplyAction(IMyShipMergeBlock block)
            {
                switch (_action)
                {
                    case MergeAction.toggle:
                        block.Enabled = !block.Enabled;
                        break;
                    case MergeAction.on:
                        block.Enabled = true;
                        break;
                    case MergeAction.off:
                        block.Enabled = false;
                        break;
                }
            }
        }

        class MergePropertyCommand : BaseBlockPropertyCommand<IMyShipMergeBlock>
        {
            enum MergeProperties
            {
                none,
                enabled,
                connected,
            }

            MergeProperties _property = MergeProperties.none;

            protected override bool HandleProperty(IMyShipMergeBlock block)
            {
                if (CompareMode == ComparisonModes.set)
                {
                    switch (_property)
                    {
                        case MergeProperties.enabled:
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
                        case MergeProperties.enabled:
                            return CheckProperty(block.Enabled);
                        case MergeProperties.connected:
                            return CheckProperty(block.IsConnected);
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
                    case MergeProperties.enabled:
                    case MergeProperties.connected:
                        return bool.TryParse(value, out _boolValue);
                    default:
                        return false;
                }
            }
        }
    }
}
