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
        public class SelfActionCommand : Command
        {
            public enum SelfAction
            {
                none,
                run,
                stop,
                pause,
                resume,
                restart,
            }

            SelfAction _action = SelfAction.none;
            string _bufferName = "";

            public override bool TryParseCommand(MyCommandLine command)
            {
                // self bufferName run
                if (command.ArgumentCount > 2)
                {
                    _bufferName = command.Argument(1);
                    if (!program._buffers.ContainsKey(_bufferName))
                    {
                        _buffer.LogError($"{_bufferName} does not exist in this block.");
                        return false;
                    }
                    
                    string actionName = command.Argument(2).ToLower();
                    if (!Enum.TryParse(actionName, out _action))
                    {
                        _buffer.LogError($"{actionName} is not a valid action!");
                        return false;
                    }

                }
                else
                {
                    _buffer.LogError($"SelfAction invalid: Not enough arguments.");
                    return false;
                }

                return true;
            }

            public override bool Execute(float deltaTime)
            {
                switch (_action)
                {
                    case SelfAction.run:
                        program._buffers[_bufferName]?.Run();
                        break;
                    case SelfAction.stop:
                        program._buffers[_bufferName]?.Stop();
                        break;
                    case SelfAction.pause:
                        program._buffers[_bufferName]?.Pause();
                        break;
                    case SelfAction.resume:
                        program._buffers[_bufferName]?.Resume();
                        break;
                    case SelfAction.restart:
                        program._buffers[_bufferName]?.Restart();
                        break;
                    default:
                        break;
                }

                return true;
            }
        }

        public class SelfPropertyCommand : Command
        {
            enum CompareModes
            {
                none,
                equals,
                notequals,
                greater,
                less,
                greaterequals,
                lessequals,
            }

            enum SelfProperties
            {
                none,
                status,
                currentcommandidx,
            }

            private SelfProperties _selfProperty = SelfProperties.none;
            private CompareModes _compareMode = CompareModes.none;
            CommandBuffer.BufferStates _state;
            string _bufferName = "";
            float _floatValue;

            public override bool TryParseCommand(MyCommandLine command)
            {
                // self group1 status == bla
                if(command.ArgumentCount > 3)
                {
                    _bufferName = command.Argument(1);
                    if (!program._buffers.ContainsKey(_bufferName))
                    {
                        _buffer.LogError($"{_bufferName} does not exist in this block.");
                        return false;
                    }

                    string property = command.Argument(2).ToLower();
                    if (!Enum.TryParse(property, out _selfProperty))
                    {
                        _buffer.LogError($"{property} is not a valid property.");
                        return false;
                    }

                    string comparison = command.Argument(3).ToLower();
                    if(!Enum.TryParse(comparison, out _compareMode))
                    {
                        switch (comparison)
                        {
                            case "==":
                                _compareMode = CompareModes.equals;
                                break;
                            case "!=":
                                _compareMode = CompareModes.notequals;
                                break;
                            case ">":
                                _compareMode = CompareModes.greater;
                                break;
                            case ">=":
                                _compareMode = CompareModes.greaterequals;
                                break;
                            case "<":
                                _compareMode = CompareModes.less;
                                break;
                            case "<=":
                                _compareMode = CompareModes.lessequals;
                                break;
                            default:
                                _buffer.LogError($"{_compareMode} is not a valid compare mode!");
                                return false;
                        }
                    }

                    string value = command.Argument(4);
                    switch (_selfProperty)
                    {
                        case SelfProperties.status:
                            if(!Enum.TryParse(value, out _state))
                            {
                                _buffer.LogError($"{value} is not a valid state!");
                                return false;
                            }
                            break;
                        case SelfProperties.currentcommandidx:
                            if(!float.TryParse(value, out _floatValue))
                            {
                                _buffer.LogError($"{value} is not a number!");
                                return false;
                            }
                            break;
                        default:
                            return false;
                    }

                    return true;
                }
                else
                {
                    _buffer.LogError($"SelfProperty invalid: Not enough arguments.");
                    return false;
                }
            }

            public override bool Execute(float deltaTime)
            {
                switch (_selfProperty)
                {
                    case SelfProperties.status:
                        return CheckProperty(program._buffers[_bufferName].BufferState);
                    case SelfProperties.currentcommandidx:
                        return CheckProperty(program._buffers[_bufferName].CurrentBufferIndex);
                    default:
                        return true;
                }
            }

            private bool CheckProperty(CommandBuffer.BufferStates state)
            {
                switch (_compareMode)
                {
                    case CompareModes.equals:
                        _buffer.LogInfo($"Checking {state} == {_state}");
                        return state == _state;
                    case CompareModes.notequals:
                        _buffer.LogInfo($"Checking {state} != {_state}");
                        return state != _state;
                    default:
                        return true;
                }
            }

            private bool CheckProperty(float value)
            {
                switch (_compareMode)
                {
                    case CompareModes.equals:
                        _buffer.LogInfo($"Checking {value} == {_floatValue}");
                        return value == _floatValue;
                    case CompareModes.notequals:
                        _buffer.LogInfo($"Checking {value} != {_floatValue}");
                        return value != _floatValue;
                    case CompareModes.greater:
                        _buffer.LogInfo($"Checking {value} > {_floatValue}");
                        return value > _floatValue;
                    case CompareModes.less:
                        _buffer.LogInfo($"Checking {value} < {_floatValue}");
                        return value < _floatValue;
                    case CompareModes.greaterequals:
                        _buffer.LogInfo($"Checking {value} >= {_floatValue}");
                        return value >= _floatValue;
                    case CompareModes.lessequals:
                        _buffer.LogInfo($"Checking {value} <= {_floatValue}");
                        return value <= _floatValue;
                    default:
                        return true;
                }
            }
        }
    }
}
