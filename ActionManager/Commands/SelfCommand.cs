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
            }

            enum SelfProperties
            {
                none,
                status,
            }

            private SelfProperties _selfProperty = SelfProperties.none;
            private CompareModes _compareMode = CompareModes.none;
            CommandBuffer.BufferStates _state;
            string _bufferName = "";


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
                            default:
                                _buffer.LogError($"{_compareMode} is not a valid compare mode!");
                                return false;
                        }
                    }

                    switch (_selfProperty)
                    {
                        case SelfProperties.status:
                            string bufferState = command.Argument(4);
                            if(!program._buffers.ContainsKey(_bufferName))
                            {
                                _buffer.LogError($"{_bufferName} does not exist in this block!");
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
                        return CheckProperty(_state);
                    default:
                        return true;
                }
            }

            private bool CheckProperty(CommandBuffer.BufferStates state)
            {
                CommandBuffer.BufferStates bufferState = program._buffers[_bufferName].BufferState; 
                switch (_compareMode)
                {
                    case CompareModes.equals:
                        _buffer.LogInfo($"Checking {bufferState} == {state}");
                        return bufferState == state;
                    case CompareModes.notequals:
                        _buffer.LogInfo($"Checking {bufferState} != {state}");
                        return bufferState != state;
                    default:
                        return true;
                }
            }
        }
    }
}
