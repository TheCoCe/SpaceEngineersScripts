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
        public abstract class Command
        {
            protected CommandBuffer _buffer;

            public abstract bool TryParseCommand(MyCommandLine command);
            public abstract bool Execute(float deltaTime);
            public void SetCommandBuffer(CommandBuffer buffer)
            {
                _buffer = buffer;
            }
        }

        public abstract class BaseBlockCommand<T> : Command where T : class, IMyTerminalBlock
        {
            public enum BlockMode
            {
                Block,
                Group,
            }

            public T Block { get; private set; }
            public IMyBlockGroup Group { get; private set; }
            protected List<T> _groupBlocks = null;

            protected BlockMode Mode { get; set; } = BlockMode.Block;

            public override bool TryParseCommand(MyCommandLine command)
            {
                if (command.ArgumentCount > 1)
                {
                    string name = command.Argument(1).Trim();

                    if (name.StartsWith("G:"))
                    {
                        Mode = BlockMode.Group;

                        name = name.Substring(2);
                        Group = program.GridTerminalSystem.GetBlockGroupWithName(name);
                        if (Group == null)
                        {
                            _buffer.LogError($"Group '{name}' not found!");
                            return false;
                        }

                        _groupBlocks = new List<T>();
                        CollectGroupBlocks();

                        if(_groupBlocks.Count == 0)
                        {
                            _buffer.LogError($"Group '{name}' has no compatible blocks!");
                        }
                    }
                    else
                    {
                        if (name.StartsWith("T:"))
                            name = name.Substring(2);
                        Block = program.GridTerminalSystem.GetBlockWithName(name) as T;
                        if (Block == null)
                        {
                            _buffer.LogError($"Block '{name}' not found!");
                            return false;
                        }
                    }
                }
                else
                {
                    _buffer.LogError($"No arguments for {GetType()} found!");
                    return false;
                }

                return true;
            }

            public void CollectGroupBlocks()
            {
                _groupBlocks.Clear();
                Group.GetBlocksOfType(_groupBlocks);
            }
        }

        public abstract class BasePropertyCommand<T> : BaseBlockCommand<T> where T : class, IMyTerminalBlock
        {
            public enum PropertyModes
            {
                none,
                equals,
                notequals,
                greater,
                less,
                greaterequals,
                lessequals,
                set
            }

            private PropertyModes _propertyMode = PropertyModes.none;
            protected PropertyModes PropertyMode
            {
                get { return _propertyMode; }
                set { _propertyMode = value; }
            }

            public override bool TryParseCommand(MyCommandLine command)
            {
                if (!base.TryParseCommand(command))
                    return false;

                if (command.ArgumentCount >= 5)
                {
                    string propertyMode = command.Argument(3);
                    if (!Enum.TryParse(propertyMode.ToLower(), out _propertyMode))
                    {
                        switch (propertyMode)
                        {
                            case "==":
                                PropertyMode = PropertyModes.equals;
                                break;
                            case "!=":
                                PropertyMode = PropertyModes.notequals;
                                break;
                            case ">":
                                PropertyMode = PropertyModes.greater;
                                break;
                            case "<":
                                PropertyMode = PropertyModes.less;
                                break;
                            case ">=":
                                PropertyMode = PropertyModes.greaterequals;
                                break;
                            case "<=":
                                PropertyMode = PropertyModes.lessequals;
                                break;
                            case "=":
                                PropertyMode = PropertyModes.set;
                                break;
                        }

                        if (PropertyMode == PropertyModes.none)
                        {
                            _buffer.LogError($"'{propertyMode}' is not a valid comparison mode.");
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        public abstract class BaseBlockActionCommand<T> : BaseBlockCommand<T> where T : class, IMyTerminalBlock
        {
            public override bool TryParseCommand(MyCommandLine command)
            {
                if (!base.TryParseCommand(command))
                    return false;

                string actionString = command.Argument(2).ToLower();
                if (!GetAction(actionString))
                {
                    _buffer.LogError($"{actionString} is an unknown action!");
                    return false;
                }

                return true;
            }

            public override bool Execute(float deltaTime)
            {
                if (Mode == BlockMode.Block)
                {
                    ApplyAction(Block);
                }
                else
                {
                    CollectGroupBlocks();
                    foreach (var block in _groupBlocks)
                    {
                        ApplyAction(block);
                    }
                }

                return true;
            }

            protected abstract bool GetAction(string action);
            protected abstract void ApplyAction(T block);
        }

        public abstract class BaseBlockPropertyCommand<T> : BasePropertyCommand<T> where T : class, IMyTerminalBlock
        {
            protected float _floatValue = 0.0F;
            protected bool _boolValue = false;
            protected Color _colorValue = Color.Black;

            public override bool TryParseCommand(MyCommandLine command)
            {
                if (!base.TryParseCommand(command))
                    return false;

                if (command.ArgumentCount < 5)
                {
                    _buffer.LogError($"{GetType()} invalid: Not enough arguments!");
                    return false;
                }

                string property = command.Argument(2).ToLower();
                if (!GetProperty(property))
                {
                    _buffer.LogError($"{GetType()} invalid: {property} is not a valid property!");
                    return false;
                }

                string value = command.Argument(4);
                if (!GetValue(value))
                {
                    _buffer.LogError($"{GetType()} invalid: '{value}' is not a valid float for '{property}'!");
                    return false;
                }

                return true;
            }

            public override bool Execute(float deltaTime)
            {
                if (Mode == BlockMode.Block)
                {
                    return HandleProperty(Block);
                }
                else
                {
                    CollectGroupBlocks();
                    bool result = true;
                    foreach (var block in _groupBlocks)
                    {
                        result &= HandleProperty(block);
                    }

                    return result;
                }
            }

            protected abstract bool HandleProperty(T block);
            protected abstract bool GetProperty(string property);
            protected abstract bool GetValue(string value);

            protected bool CheckProperty(float value)
            {
                switch (PropertyMode)
                {
                    case PropertyModes.equals:
                        _buffer.LogInfo($"Checking {value} == {_floatValue}");
                        return value == _floatValue;
                    case PropertyModes.notequals:
                        _buffer.LogInfo($"Checking {value} != {_floatValue}");
                        return value != _floatValue;
                    case PropertyModes.greater:
                        _buffer.LogInfo($"Checking {value} > {_floatValue}");
                        return value > _floatValue;
                    case PropertyModes.less:
                        _buffer.LogInfo($"Checking {value} < {_floatValue}");
                        return value < _floatValue;
                    case PropertyModes.greaterequals:
                        _buffer.LogInfo($"Checking {value} >= {_floatValue}");
                        return value >= _floatValue;
                    case PropertyModes.lessequals:
                        _buffer.LogInfo($"Checking {value} <= {_floatValue}");
                        return value <= _floatValue;
                    default:
                        return true;
                }
            }

            protected bool CheckProperty(bool value)
            {
                switch (PropertyMode)
                {
                    case PropertyModes.equals:
                        _buffer.LogInfo($"Checking {value} == {_boolValue}");
                        return value == _boolValue;
                    case PropertyModes.notequals:
                        _buffer.LogInfo($"Checking {value} != {_boolValue}");
                        return value != _boolValue;
                    default:
                        return false;
                }
            }

            protected bool CheckProperty(Color value)
            {
                switch (PropertyMode)
                {
                    case PropertyModes.equals:
                        _buffer.LogInfo($"Checking {value} == {_colorValue}");
                        return value == _colorValue;
                    case PropertyModes.notequals:
                        _buffer.LogInfo($"Checking {value} != {_colorValue}");
                        return value != _colorValue;
                    default:
                        return false;
                }
            }
        }
    }
}
