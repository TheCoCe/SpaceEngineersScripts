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
        class RotorActionCommand : BaseBlockActionCommand<IMyMotorStator>
        {
            enum RotorAction
            {
                none,
                toggle,
                reverse,
                on,
                off,
                attach,
                detach
            }

            RotorAction _action = RotorAction.none;

            protected override bool GetAction(string action)
            {
                return Enum.TryParse(action, out _action);
            }

            protected override void ApplyAction(IMyMotorStator block)
            {
                switch (_action)
                {
                    case RotorAction.reverse:
                        block.TargetVelocityRad = -block.TargetVelocityRad;
                        break;
                    case RotorAction.on:
                        block.Enabled = true;
                        break;
                    case RotorAction.off:
                        block.Enabled = false;
                        break;
                    case RotorAction.attach:
                        block.Attach();
                        break;
                    case RotorAction.detach:
                        block.Detach();
                        break;
                }
            }
        }

        class RotorPropertyCommand : BaseBlockPropertyCommand<IMyMotorStator>
        {
            enum RotorProperties
            {
                none,
                enabled,
                angle,
                torque,
                breakingtorque,
                velocity,
                lowerlimit,
                upperlimit,
                displacement,
                rotorlock,
            }

            RotorProperties _property = RotorProperties.none;

            protected override bool HandleProperty(IMyMotorStator block)
            {
                if(PropertyMode == PropertyModes.set)
                {
                    switch (_property)
                    {
                        case RotorProperties.enabled:
                            block.Enabled = _boolValue;
                            return true;
                        case RotorProperties.torque:
                            block.Torque = _floatValue;
                            return true;
                        case RotorProperties.breakingtorque:
                            block.BrakingTorque = _floatValue;
                            return true;
                        case RotorProperties.velocity:
                            block.TargetVelocityRPM = _floatValue;
                            return true;
                        case RotorProperties.lowerlimit:
                            block.LowerLimitDeg = _floatValue;
                            return true;
                        case RotorProperties.upperlimit:
                            block.UpperLimitDeg = _floatValue;
                            return true;
                        case RotorProperties.displacement:
                            block.Displacement = _floatValue;
                            return true;
                        case RotorProperties.rotorlock:
                            block.RotorLock = _boolValue;
                            return true;
                        default:
                            return false;
                    }
                }
                else
                {
                    switch (_property)
                    {
                        case RotorProperties.enabled:
                            return CheckProperty(block.Enabled);
                        case RotorProperties.angle:
                            return CheckProperty(block.Angle);
                        case RotorProperties.torque:
                            return CheckProperty(block.Torque);
                        case RotorProperties.breakingtorque:
                            return CheckProperty(block.BrakingTorque);
                        case RotorProperties.velocity:
                            return CheckProperty(block.TargetVelocityRPM);
                        case RotorProperties.lowerlimit:
                            return CheckProperty(block.LowerLimitDeg);
                        case RotorProperties.upperlimit:
                            return CheckProperty(block.UpperLimitDeg);
                        case RotorProperties.displacement:
                            return CheckProperty(block.Displacement);
                        case RotorProperties.rotorlock:
                            return CheckProperty(block.RotorLock);
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
                    case RotorProperties.enabled:
                    case RotorProperties.rotorlock:
                        return bool.TryParse(value, out _boolValue);
                    case RotorProperties.angle:
                    case RotorProperties.torque:
                    case RotorProperties.breakingtorque:
                    case RotorProperties.velocity:
                    case RotorProperties.lowerlimit:
                    case RotorProperties.upperlimit:
                    case RotorProperties.displacement:
                        return float.TryParse(value, out _floatValue);
                    default:
                        return false;
                }
            }
        }
    }
}
