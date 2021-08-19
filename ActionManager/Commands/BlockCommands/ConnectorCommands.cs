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
        class ConnectorActionCommand : BaseBlockActionCommand<IMyShipConnector>
        {
            enum ConnectorActions
            {
                none,
                toggle,
                on,
                off,
                toggleconnect,
                connect,
                disconnect,
            }

            ConnectorActions _action = ConnectorActions.none;

            protected override bool GetAction(string action)
            {
                return Enum.TryParse(action, out _action);
            }

            protected override void ApplyAction(IMyShipConnector block)
            {
                switch (_action)
                {
                    case ConnectorActions.toggle:
                        block.Enabled = !block.Enabled;
                        break;
                    case ConnectorActions.on:
                        block.Enabled = true;
                        break;
                    case ConnectorActions.off:
                        block.Enabled = false;
                        break;
                    case ConnectorActions.toggleconnect:
                        block.ToggleConnect();
                        break;
                    case ConnectorActions.connect:
                        block.Connect();
                        break;
                    case ConnectorActions.disconnect:
                        block.Disconnect();
                        break;
                }
            }
        }

        class ConnectorPropertyCommand : BaseBlockPropertyCommand<IMyShipConnector>
        {
            enum ConnectorProperties
            {
                none,
                enabled,
                throwout,
                collectall,
                pullstrength,
                isparkingenabled,
                connectionallowed,
                connected,
            }

            ConnectorProperties _property = ConnectorProperties.none;

            protected override bool HandleProperty(IMyShipConnector block)
            {
                if(PropertyMode == PropertyModes.set)
                {
                    switch (_property)
                    {
                        case ConnectorProperties.enabled:
                            block.Enabled = _boolValue;
                            return true; ;
                        case ConnectorProperties.throwout:
                            block.ThrowOut = _boolValue;
                            return true;
                        case ConnectorProperties.collectall:
                            block.CollectAll = _boolValue;
                            return true;
                        case ConnectorProperties.pullstrength:
                            block.PullStrength = _floatValue;
                            return true;
                        case ConnectorProperties.isparkingenabled:
                            block.IsParkingEnabled = _boolValue;
                            return true;
                        default:
                            return false;
                    }
                }
                else
                {
                    switch (_property)
                    {
                        case ConnectorProperties.enabled:
                            return CheckProperty(block.Enabled);
                        case ConnectorProperties.throwout:
                            return CheckProperty(block.ThrowOut);
                        case ConnectorProperties.collectall:
                            return CheckProperty(block.CollectAll);
                        case ConnectorProperties.pullstrength:
                            return CheckProperty(block.PullStrength);
                        case ConnectorProperties.isparkingenabled:
                            return CheckProperty(block.IsParkingEnabled);
                        case ConnectorProperties.connectionallowed:
                            return CheckProperty(block.CheckConnectionAllowed);
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
                    case ConnectorProperties.enabled:
                    case ConnectorProperties.throwout:
                    case ConnectorProperties.collectall:
                    case ConnectorProperties.isparkingenabled:
                    case ConnectorProperties.connectionallowed:
                    case ConnectorProperties.connected:
                        return bool.TryParse(value, out _boolValue);
                    case ConnectorProperties.pullstrength:
                        return float.TryParse(value, out _floatValue);
                    default:
                        return false;
                }
            }
        }
    }
}
