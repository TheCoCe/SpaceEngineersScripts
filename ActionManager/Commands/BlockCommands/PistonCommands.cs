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
        class PistonActionCommand : BaseBlockActionCommand<IMyPistonBase>
        {
            enum PistonAction
            {
                none,
                toggle,
                on,
                off,
                extend,
                retract,
                reverse,
                attach,
                detach
            }

            PistonAction _action = PistonAction.none;

            protected override bool GetAction(string action)
            {
                return Enum.TryParse(action, out _action);
            }

            protected override void ApplyAction(IMyPistonBase block)
            {
                switch (_action)
                {
                    case PistonAction.extend:
                        block.Extend();
                        break;
                    case PistonAction.retract:
                        block.Retract();
                        break;
                    case PistonAction.reverse:
                        block.Reverse();
                        break;
                    case PistonAction.toggle:
                        block.Enabled = !block.Enabled;
                        break;
                    case PistonAction.on:
                        block.Enabled = true;
                        break;
                    case PistonAction.off:
                        block.Enabled = false;
                        break;
                    case PistonAction.attach:
                        block.Attach();
                        break;
                    case PistonAction.detach:
                        block.Detach();
                        break;
                }
            }
        }

        class PistonPropertyCommand : BaseBlockPropertyCommand<IMyPistonBase>
        {
            enum PistonProperties
            {
                none,
                enabled,
                velocity,
                maxvelocity,
                minlimit,
                maxlimit,
                currentposition,
            }

            PistonProperties _property = PistonProperties.none;

            protected override bool HandleProperty(IMyPistonBase block)
            {
                if(PropertyMode == PropertyModes.set)
                {
                    switch (_property)
                    {
                        case PistonProperties.enabled:
                            block.Enabled = _boolValue;
                            return true;
                        case PistonProperties.velocity:
                            block.Velocity = _floatValue;
                            return true;
                        case PistonProperties.minlimit:
                            block.MinLimit = _floatValue;
                            return true;
                        case PistonProperties.maxlimit:
                            block.MaxLimit = _floatValue;
                            return true;
                        default:
                            return false;
                    }
                }
                else
                {
                    switch (_property)
                    {
                        case PistonProperties.enabled:
                            return CheckProperty(block.Enabled);
                        case PistonProperties.velocity:
                            return CheckProperty(block.Velocity);
                        case PistonProperties.maxvelocity:
                            return CheckProperty(block.MaxVelocity);
                        case PistonProperties.minlimit:
                            return CheckProperty(block.MinLimit);
                        case PistonProperties.maxlimit:
                            return CheckProperty(block.MaxLimit);
                        case PistonProperties.currentposition:
                            return CheckProperty(block.CurrentPosition);
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
                    case PistonProperties.velocity:
                    case PistonProperties.maxvelocity:
                    case PistonProperties.minlimit:
                    case PistonProperties.maxlimit:
                    case PistonProperties.currentposition:
                        return float.TryParse(value, out _floatValue);
                    case PistonProperties.enabled:
                        return bool.TryParse(value, out _boolValue);
                    default:
                        return false;
                }
            }
        }
    }
}
