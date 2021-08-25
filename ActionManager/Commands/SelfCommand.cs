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
                    if (!Enum.TryParse(actionName, true, out _action))
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
                CommandBuffer buffer = program._buffers[_bufferName];
                switch (_action)
                {
                    case SelfAction.run:
                        buffer?.Run();
                        break;
                    case SelfAction.stop:
                        buffer?.Stop();
                        break;
                    case SelfAction.pause:
                        buffer?.Pause();
                        break;
                    case SelfAction.resume:
                        buffer?.Resume();
                        break;
                    case SelfAction.restart:
                        buffer?.Restart();
                        break;
                    default:
                        break;
                }
                // if we are calling our own buffer we need to return false so that we don't increment the buffer index
                if (_buffer == buffer)
                    return false;
                return true;
            }
        }

        public class SelfPropertyCommand : Command
        {
            enum SelfProperties
            {
                none,
                status,
                currentcommandidx,
            }

            private SelfProperties _selfProperty = SelfProperties.none;
            private ComparisonModes _compareMode = ComparisonModes.none;
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
                    if (!Enum.TryParse(property, true, out _selfProperty))
                    {
                        _buffer.LogError($"{property} is not a valid property.");
                        return false;
                    }

                    string comparison = command.Argument(3).ToLower();
                    if(!Enum.TryParse(comparison, true, out _compareMode))
                    {
                        switch (comparison)
                        {
                            case "==":
                                _compareMode = ComparisonModes.equals;
                                break;
                            case "!=":
                                _compareMode = ComparisonModes.notequals;
                                break;
                            case ">":
                                _compareMode = ComparisonModes.greater;
                                break;
                            case ">=":
                                _compareMode = ComparisonModes.greaterequals;
                                break;
                            case "<":
                                _compareMode = ComparisonModes.less;
                                break;
                            case "<=":
                                _compareMode = ComparisonModes.lessequals;
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
                            if(!Enum.TryParse(value, true, out _state))
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
                    case ComparisonModes.equals:
                        _buffer.LogInfo($"Checking {state} == {_state}");
                        return state == _state;
                    case ComparisonModes.notequals:
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
                    case ComparisonModes.equals:
                        _buffer.LogInfo($"Checking {value} == {_floatValue}");
                        return value == _floatValue;
                    case ComparisonModes.notequals:
                        _buffer.LogInfo($"Checking {value} != {_floatValue}");
                        return value != _floatValue;
                    case ComparisonModes.greater:
                        _buffer.LogInfo($"Checking {value} > {_floatValue}");
                        return value > _floatValue;
                    case ComparisonModes.less:
                        _buffer.LogInfo($"Checking {value} < {_floatValue}");
                        return value < _floatValue;
                    case ComparisonModes.greaterequals:
                        _buffer.LogInfo($"Checking {value} >= {_floatValue}");
                        return value >= _floatValue;
                    case ComparisonModes.lessequals:
                        _buffer.LogInfo($"Checking {value} <= {_floatValue}");
                        return value <= _floatValue;
                    default:
                        return true;
                }
            }
        }
    }
}
