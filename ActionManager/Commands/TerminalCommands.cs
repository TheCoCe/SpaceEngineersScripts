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
        class TerminalActionCommand : BaseBlockCommand<IMyTerminalBlock> 
        {
            protected ITerminalAction terminalAction = null;

            public override bool TryParseCommand(MyCommandLine command)
            {
                if (!base.TryParseCommand(command))
                    return false;

                string actionString = command.Argument(2);

                return CollectTerminalActionAndBlocks(actionString);
            }

            public override bool Execute(float deltaTime)
            {
                return ApplyGenericAction();
            }

            private bool CollectTerminalActionAndBlocks(string action)
            {
                if(action == null)
                    return true;

                if (Mode == BlockMode.Block)
                {
                    if (Block.HasAction(action))
                    {
                        terminalAction = Block.GetActionWithName(action);
                    }

                    if (terminalAction == null)
                    {
                        _buffer?.LogError($"Action invalid: '{action}' is not a valid action to perform on '{Block.GetType()}:{Block.CustomName}'");
                        return false;
                    }
                }
                else
                {
                    for (int i = _groupBlocks.Count - 1; i >= 0; i--)
                    {
                        if(!_groupBlocks[i].HasAction(action))
                        {
                            _groupBlocks.RemoveAt(i);
                        }
                    }

                    if (_groupBlocks.Count > 0)
                    {
                        terminalAction = _groupBlocks.First().GetActionWithName(action);
                    }
                    else
                    {
                        _buffer?.LogError($"Action invalid: No blocks in group '{Group.Name}' supports the specified action '{action}'");
                        return false;
                    }
                }

                return true;
            }
            
            private bool ApplyGenericAction()
            {
                if(terminalAction == null)
                {
                    List<ITerminalAction> actions = new List<ITerminalAction>();
                    if (Mode == BlockMode.Block)
                    {
                        _buffer?.LogMessage(MessageManager.MessageType.Info, $"{Block.CustomName} Actions:", 30.0F);
                        Block.GetActions(actions);
                        foreach (var action in actions) _buffer?.LogMessage(MessageManager.MessageType.Info, action.Id, 30.0F);
                    }
                    else
                    {
                        foreach (var block in _groupBlocks)
                        {
                            _buffer?.LogMessage(MessageManager.MessageType.Info, $"{block.CustomName} Actions:", 30.0F);
                            block.GetActions(actions);
                            foreach (var action in actions) _buffer?.LogMessage(MessageManager.MessageType.Info, action.Id, 30.0F);
                            actions.Clear();
                        }
                    }
                }
                else
                {
                    if (Mode == BlockMode.Block)
                    {
                        try
                        {
                            terminalAction.Apply(Block);
                            return true;
                        }
                        catch (Exception e)
                        {
                            _buffer?.LogError(e.Message);
                        }
                    }
                    else
                    {
                        try
                        {
                            foreach (var block in _groupBlocks)
                            {
                                terminalAction.Apply(block);
                            }
                            return true;
                        }
                        catch (Exception e)
                        {
                            _buffer?.LogError(e.Message);
                        }
                    }
                }

                return true;
            }
        }

        public abstract class BaseTerminalProperty
        {
            public abstract bool TryParse(string value);
            public abstract void Set(IMyTerminalBlock block);
            public abstract bool Equal(IMyTerminalBlock block);
            public virtual bool GreaterThan(IMyTerminalBlock block) { return false; }
            public virtual bool LessThan(IMyTerminalBlock block) { return false; }
            public virtual bool GreaterThanOrEqual(IMyTerminalBlock block) { return false; }
            public virtual bool LessThanOrEqual(IMyTerminalBlock block) { return false; }
        }

        public abstract class TerminalProperty<T> : BaseTerminalProperty
        {
            protected T _value;
            public T Value { get; }

            private ITerminalProperty<T> terminalProperty;
            public ITerminalProperty<T> Property
            {
                get { return terminalProperty; }
                set { terminalProperty = value; }
            }

            public TerminalProperty(ITerminalProperty<T> property)
            {
                terminalProperty = property;
            }
        }

        public class TerminalFloatProperty : TerminalProperty<float>
        {
            public TerminalFloatProperty(ITerminalProperty<float> property) : base(property)
            {
                _value = 0.0F;
            }

            public override bool TryParse(string value)
            {
                return float.TryParse(value, out _value);
            }

            public override bool Equal(IMyTerminalBlock block)
            {
                return Property.GetValue(block) == Value;
            }

            public override bool GreaterThan(IMyTerminalBlock block)
            {
                return Property.GetValue(block) > Value;
            }

            public override bool LessThan(IMyTerminalBlock block)
            {
                return Property.GetValue(block) < Value;
            }

            public override bool GreaterThanOrEqual(IMyTerminalBlock block)
            {
                return Property.GetValue(block) >= Value;
            }

            public override bool LessThanOrEqual(IMyTerminalBlock block)
            {
                return Property.GetValue(block) <= Value;
            }

            public override void Set(IMyTerminalBlock block)
            {
                Property.SetValue(block, _value);
            }
        }

        public class TerminalBoolProperty : TerminalProperty<bool>
        {
            public TerminalBoolProperty(ITerminalProperty<bool> property) : base(property)
            {
                _value = false;
            }

            public override bool TryParse(string value)
            {
                return bool.TryParse(value, out _value);
            }

            public override bool Equal(IMyTerminalBlock block)
            {
                return Property.GetValue(block) == Value;
            }

            public override void Set(IMyTerminalBlock block)
            {
                Property.SetValue(block, _value);
            }
        }

        public class TerminalColorProperty : TerminalProperty<Color>
        {
            public TerminalColorProperty(ITerminalProperty<Color> property) : base(property)
            {
                _value = Color.White;
            }

            public override bool TryParse(string value)
            {
                var values = value.Split(':');
                if(values.Length == 3)
                {
                    float r, g, b;
                    if(float.TryParse(values[0], out r) && float.TryParse(values[1], out g) && float.TryParse(values[2], out b))
                    {
                        _value = new Color(r, g, b);
                    }
                }
                return false;
            }

            public override bool Equal(IMyTerminalBlock block)
            {
                return Property.GetValue(block) == Value;
            }

            public override void Set(IMyTerminalBlock block)
            {
                Property.SetValue(block, _value);
            }
        }

        public class TerminalPropertyCommand : BasePropertyCommand<IMyTerminalBlock>
        {
            private ITerminalProperty _terminalProperty = null;
            private BaseTerminalProperty _property;

            public override bool TryParseCommand(MyCommandLine command)
            {
                if (!base.TryParseCommand(command))
                    return false;

                if (command.ArgumentCount >= 5)
                {
                    string propertyString = command.Argument(2);
                    if (!CollectTerminalPropertyAndBlocks(propertyString))
                        return false;

                    string value = command.Argument(4);
                    if (!_property.TryParse(value))
                    {
                        _buffer?.LogError($"Property invalid: '{value}' is not a valid type for {propertyString}");
                        return false;
                    }
                }
                return true;
            }

            public override bool Execute(float deltaTime)
            {
                return HandleTerminalProperty();
            }

            private bool HandleTerminalProperty()
            {
                if (Mode == BlockMode.Block)
                {
                    return HandlePropertyMode(Block);
                }
                else
                {
                    bool result = true;
                    foreach (var block in _groupBlocks)
                    {
                        result = HandlePropertyMode(block);
                        if (!result) break;
                    }

                    return result;
                }
            }

            private bool HandlePropertyMode(IMyTerminalBlock block)
            {
                switch (PropertyMode)
                {
                    case PropertyModes.none:
                        {
                            List<ITerminalProperty> properties = new List<ITerminalProperty>();
                            block.GetProperties(properties);
                            foreach (var property in properties)
                            {
                                _buffer?.LogMessage(MessageManager.MessageType.Info, property.Id, 30.0F);
                            }
                            return true;
                        }
                    case PropertyModes.equals:
                        return _property.Equal(block);
                    case PropertyModes.notequals:
                        return !_property.Equal(block);
                    case PropertyModes.greater:
                        return _property.GreaterThan(block);
                    case PropertyModes.less:
                        return _property.LessThan(block);
                    case PropertyModes.greaterequals:
                        return _property.GreaterThanOrEqual(block);
                    case PropertyModes.lessequals:
                        return _property.LessThanOrEqual(block);
                    case PropertyModes.set:
                        _property.Set(block);
                        return true;
                    default:
                        return false;
                }
            }

            private bool CollectTerminalPropertyAndBlocks(string propertyString)
            {
                if (Mode == BlockMode.Block)
                {
                    _terminalProperty = Block.GetProperty(propertyString);
                    if (_terminalProperty == null)
                    {
                        _buffer?.LogError($"Property invalid: '{propertyString}' is not a valid property of '{Block.GetType()}:{Block.CustomName}'");
                        return false;
                    }

                    switch (_terminalProperty.TypeName)
                    {
                        case "Boolean":
                            _property = new TerminalBoolProperty(_terminalProperty.AsBool());
                            break;
                        case "Single":
                            _property = new TerminalFloatProperty(_terminalProperty.AsFloat());
                            break;
                        case "Color":
                            _property = new TerminalColorProperty(_terminalProperty.AsColor());
                            break;
                    }
                }
                else
                {
                    ITerminalProperty prop;
                    for (int i = _groupBlocks.Count - 1; i >= 0; i--)
                    {
                        prop = _groupBlocks[i].GetProperty(propertyString);
                        if(prop == null)
                        {
                            _groupBlocks.RemoveAt(i);
                        }
                    }

                    if (_groupBlocks.Count == 0)
                    {
                        _buffer?.LogError($"Property invalid: No blocks in group '{Group.Name}' supports the specified property '{propertyString}'");
                        return false;
                    }
                    else
                    {
                        prop = _groupBlocks.First().GetProperty(propertyString);

                        switch (prop.TypeName)
                        {
                            case "Boolean":
                                _property = new TerminalBoolProperty(prop.AsBool());
                                break;
                            case "Single":
                                _property = new TerminalFloatProperty(prop.AsFloat());
                                break;
                            case "Color":
                                _property = new TerminalColorProperty(prop.AsColor());
                                break;
                        }
                    }
                }

                return true;
            }
        }
    }
}
