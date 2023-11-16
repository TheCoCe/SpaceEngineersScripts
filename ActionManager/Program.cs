using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
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
    partial class Program : MyGridProgram
    {
        private static MyCommandLine _commandLine = new MyCommandLine();
        static Program program = null;
        
        string _customData = "";
        Dictionary<string, CommandBuffer> _buffers;

        const int _customDataParseInterval = 100;
        int _customDataParseTicks = 0;

        static readonly string[] procSigns = { "—", " \\ ", " | ", " / " };
        int procSignIdx = 0;

        // statistics data
        int lastInstructions = 0;
        int maxInstructions = 0;
        float lastMs = 0F;
        float maxMs = 0F;


        public Program()
        {
            if (program == null)
                program = this;
            _buffers = new Dictionary<string, CommandBuffer>();

            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            ParseCustomData();
            maxMs = 0F;
            maxInstructions = 0;
        }

        public void Save() { }

        public void Main(string argument, UpdateType updateSource)
        {
            switch (updateSource)
            {
                case UpdateType.Update1:
                case UpdateType.Update10:
                case UpdateType.Update100:
                    CheckCustomData();
                    ExecuteBuffers((float)Runtime.TimeSinceLastRun.TotalSeconds);
                    PrintInfo();
                    GatherStats((float)Runtime.TimeSinceLastRun.TotalSeconds);
                    break;
                default:
                    HandleCommands(argument);
                    break;
            }
        }

        void PrintInfo()
        {
            string info = "";

            info += $"Registered Buffers ({_buffers.Count} total):\n\n";

            foreach (var buffer in _buffers)
            {
                if (buffer.Value.BufferState == CommandBuffer.BufferStates.Idle)
                {
                    info += $"'{buffer.Key}':\n";
                    info += $"  Status: {buffer.Value.BufferState} ({buffer.Value.CommandCount} Commands)\n";
                }
                else
                {
                    info += $"-> '{buffer.Key}':\n";
                    info += $"  Status: {buffer.Value.BufferState} (Command: {buffer.Value.CurrentBufferIndex + 1}/{buffer.Value.CommandCount}) {procSigns[procSignIdx / 10]}\n";
                }

                string bufferMessages = buffer.Value.GetMessages();
                if (bufferMessages.Length > 0)
                {
                    info += "  Log Messages: \n";
                    info += buffer.Value.GetMessages();
                }

                info += "--------------------------------------------------\n\n";
            }

            info += "Script Statistics:\n";

            info += $"Last runtime: {lastMs} ms\n";
            info += $"Max runtime: {maxMs} ms\n";
            info += $"Last instructions: {lastInstructions}/{Runtime.MaxInstructionCount}\n";
            info += $"Max instructions: {maxInstructions}/{Runtime.MaxInstructionCount}\n";

            Echo(info);

            procSignIdx++;
            procSignIdx %= 40;
        }

        void GatherStats(float deltaTime)
        {
            lastMs = (float)Runtime.LastRunTimeMs;
            if (lastMs > maxMs)
                maxMs = lastMs;

            lastInstructions = Runtime.CurrentInstructionCount;
            if (lastInstructions > maxInstructions)
                maxInstructions = lastInstructions;
        }

        void ExecuteBuffers(float deltaTime)
        {
            foreach (var buffer in _buffers.Values)
            {
                buffer.ExecuteCommandBuffer(deltaTime);
            }
        }

        void HandleCommands(string argument)
        {
            if (_commandLine.TryParse(argument) && _commandLine.ArgumentCount == 2)
            {
                string buffer = _commandLine.Argument(1);
                if (!_buffers.ContainsKey(buffer))
                {
                    Echo($"CommandBuffer '{buffer}' does not exist!");
                    return;
                }

                string command = _commandLine.Argument(0);
                switch (command.ToLower())
                {
                    case "run":
                        if (_buffers[buffer].Run())
                            _buffers[buffer].LogWarning($"{buffer} now running!");
                        else
                            _buffers[buffer].LogWarning($"Run failed. {buffer} already running!");
                        break;
                    case "pause":
                        if (_buffers[buffer].Pause())
                            _buffers[buffer].LogWarning($"{buffer} paused!");
                        else
                            _buffers[buffer].LogWarning($"Pause failed. {buffer} not running!");
                        break;
                    case "resume":
                        if (_buffers[buffer].Resume())
                            _buffers[buffer].LogWarning($"{buffer} resumed!");
                        else
                            _buffers[buffer].LogWarning($"Resume failed. {buffer} not paused!");
                        break;
                    case "restart":
                        _buffers[buffer].Restart();
                        _buffers[buffer].LogWarning($"{buffer} restarted!");
                        break;
                    case "stop":
                        if (_buffers[buffer].Stop())
                            _buffers[buffer].LogWarning($"{buffer} stopped!");
                        else
                            _buffers[buffer].LogWarning($"Stop failed. {buffer} not running!");
                        break;
                    default:
                        Echo($"Invalid argument: {command}");
                        break;
                }
            }
        }

        void CheckCustomData()
        {
            _customDataParseTicks++;
            if (_customDataParseTicks >= _customDataParseInterval)
            {
                ParseCustomData();
                _customDataParseTicks = 0;
            }
        }

        /// <summary>
        /// Parse the CustomData of the programmable block and create sequences and commands from them.
        /// </summary>
        /// <returns>Return true if data is new and parsed, false if the data didn't change from the last parse.</returns>

        bool ParseCustomData()
        {
            if (_customData.Equals(Me.CustomData))
            {
                return false;
            }
            // Data changed

            _customData = string.Copy(Me.CustomData);

            var customDataLines = _customData.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var keys = _buffers.Keys.ToList();

            for (int i = 0; i < customDataLines.Length; i++)
            {
                var line = customDataLines[i].Trim();

                // [sequence]
                if (line.Length > 2 && line[0] == '[' && line[line.Length - 1] == ']')
                {
                    var sequenceName = line.Substring(1, line.Length - 2);

                    int cmdIdx = i + 1;
                    for (; cmdIdx < customDataLines.Length; cmdIdx++)
                    {
                        line = customDataLines[cmdIdx] = customDataLines[cmdIdx].Trim();

                        if (line.StartsWith("//")) continue;                                            // ignore comments
                        if (line.Length > 2 && line[0] == '[' && line[line.Length - 1] == ']') break;   // stop on next sequence
                    }
                    cmdIdx--;

                    // 4, [a]   <- i
                    // 5, aaaaa
                    // 6, aaaaa <- cmdIdx
                    // 7, [b]
                    int length = cmdIdx - i;
                    string[] commands = new string[length];
                    Array.Copy(customDataLines, i + 1, commands, 0, length);
                    i = cmdIdx;

                    if (_buffers.ContainsKey(sequenceName))
                    {
                        if (_buffers[sequenceName].BuildCommandBuffer(commands))
                            Echo($"{sequenceName} updated successfully");
                        // Remove updated buffer keys
                        keys.Remove(sequenceName);
                    }
                    else
                    {
                        var cmdbuffer = new CommandBuffer();
                        _buffers.Add(sequenceName, cmdbuffer);
                        cmdbuffer.BuildCommandBuffer(commands);
                    }
                }
            }

            // Remove buffers that are no longer defined
            foreach (var key in keys) _buffers.Remove(key);

            return true;
        }

        /// <summary>
        /// CommandBuffer holds a list of commands to execute. Needs to be ticked.
        /// </summary>

        public class CommandBuffer
        {
            public enum BufferStates
            {
                Paused,
                Idle,
                Running,
            }

            List<Command> _commandBuffer;

            public BufferStates BufferState { get; private set; } = BufferStates.Idle;
            public int CurrentBufferIndex { get; private set; } = 0;

            string[] _commands = { };
            MessageManager messageManager;

            public CommandBuffer()
            {
                _commandBuffer = new List<Command>();
                messageManager = new MessageManager();
            }

            public int CommandCount
            {
                get { return _commandBuffer.Count; }
            }

            public bool BuildCommandBuffer(string[] commands)
            {
                if (_commands == commands)
                    return false;

                _commands = commands;
                _commandBuffer.Clear();
                CurrentBufferIndex = 0;
                messageManager.Clear();

                foreach (var c in _commands)
                {
                    if (c.StartsWith("//")) continue;

                    if (_commandLine.TryParse(c))
                    {
                        string commandString = _commandLine.Argument(0).ToLower();
                        Command cmd = null;

                        switch (commandString)
                        {
                            // Check general commands
                            case "action":
                                cmd = new TerminalActionCommand();
                                break;
                            case "property":
                                cmd = new TerminalPropertyCommand();
                                break;
                            case "delay":
                                cmd = new DelayCommand();
                                break;
                            case "print":
                                cmd = new PrintCommand();
                                break;
                            case "inventory":
                                cmd = new InventoryCommand();
                                break;
                            default:
                                {
                                    // Check block specific commands
                                    bool property = _commandLine.ArgumentCount > 3;
                                    switch (commandString)
                                    {
                                        case "piston":
                                            if (property)
                                                cmd = new PistonPropertyCommand();
                                            else
                                                cmd = new PistonActionCommand();
                                            break;
                                        case "rotor":
                                            if (property)
                                                cmd = new RotorPropertyCommand();
                                            else
                                                cmd = new RotorActionCommand();
                                            break;
                                        case "connector":
                                            if (property)
                                                cmd = new ConnectorPropertyCommand();
                                            else
                                                cmd = new ConnectorActionCommand();
                                            break;
                                        case "merge":
                                            if (property)
                                                cmd = new MergePropertyCommand();
                                            else
                                                cmd = new MergeActionCommand();
                                            break;
                                        case "drill":
                                            if (property)
                                                cmd = new DrillPropertyCommand();
                                            else
                                                cmd = new DrillActionCommand();
                                            break;
                                        case "self":
                                            if (property)
                                                cmd = new SelfPropertyCommand();
                                            else
                                                cmd = new SelfActionCommand();
                                            break;
                                        default:
                                            LogWarning($"Parsing invalid: {_commandLine.Argument(0)} is not a valid command");
                                            break;
                                    }
                                    break;
                                }
                        }

                        if (cmd != null)
                        {
                            cmd.SetCommandBuffer(this);
                            if (cmd.TryParseCommand(_commandLine))
                            {
                                _commandBuffer.Add(cmd);
                            }
                        }
                    }
                }

                return true;
            }

            public void ExecuteCommandBuffer(float deltaTime)
            {
                messageManager.Tick(deltaTime);

                if (BufferState == BufferStates.Running)
                {
                    if (CurrentBufferIndex < _commandBuffer.Count())
                    {
                        if (_commandBuffer[CurrentBufferIndex].Execute(deltaTime))
                        {
                            CurrentBufferIndex++;
                        }
                    }
                    else
                    {
                        BufferState = BufferStates.Idle;
                    }
                }
            }

            public bool Run()
            {
                if (BufferState != BufferStates.Running)
                {
                    CurrentBufferIndex = 0;
                    BufferState = BufferStates.Running;
                    return true;
                }
                return false;
            }

            public bool Stop()
            {
                if (BufferState != BufferStates.Idle)
                {
                    BufferState = BufferStates.Idle;
                    return true;
                }
                return false;
            }

            public bool Pause()
            {
                if (BufferState == BufferStates.Running)
                {
                    BufferState = BufferStates.Paused;
                    return true;
                }
                return false;
            }

            public bool Resume()
            {
                if (BufferState == BufferStates.Paused)
                {
                    BufferState = BufferStates.Running;
                    return true;
                }
                return false;
            }

            public void Restart()
            {
                CurrentBufferIndex = 0;
                BufferState = BufferStates.Running;
            }

            public void LogMessage(MessageManager.MessageType type, string message, float duration)
            {
                messageManager.AddMessage(type, message, duration);
            }

            public void LogError(string error)
            {
                messageManager.AddMessage(MessageManager.MessageType.Error, error);
            }

            public void LogWarning(string warning)
            {
                messageManager.AddMessage(MessageManager.MessageType.Warning, warning);
            }

            public void LogInfo(string info)
            {
                messageManager.AddMessage(MessageManager.MessageType.Info, info);
            }

            public string GetMessages()
            {
                return messageManager.GetMessages();
            }
        }

        /// <summary>
        /// MessageManager can recieve and handle different message types.
        /// </summary>

        public class MessageManager
        {
            public enum MessageType
            {
                Error,
                Warning,
                Info,
            }

            private class Message
            {
                public MessageType messageType;
                public string message;
                public float Time { get; set; }

                public Message(MessageType type, string msg, float time)
                {
                    messageType = type;
                    message = msg;
                    Time = time;
                }
            }

            public static readonly float[] kDefaultMessageDuration = { -1.0F, 10.0F, 0.0F };

            private List<Message> _messages;

            public MessageManager()
            {
                _messages = new List<Message>();
            }

            public void Tick(float deltaTime)
            {
                for (int i = _messages.Count - 1; i >= 0; i--)
                {
                    if (_messages[i].messageType == MessageType.Error)
                        continue;

                    if ((_messages[i].Time = _messages[i].Time - deltaTime) < 0F)
                        _messages.RemoveAt(i);
                }
            }

            public void AddMessage(MessageType type, string message, float duration = -1.0F)
            {
                _messages.Add(new Message(type, message, duration == -1.0F ? kDefaultMessageDuration[(int)type] : duration));
            }

            public void Clear()
            {
                _messages.Clear();
            }

            public string GetMessages()
            {
                string result = "";

                for (int i = _messages.Count - 1; i >= 0; i--)
                {
                    result += $"    {_messages[i].messageType}: {_messages[i].message}\n";
                }

                return result;
            }
        }
    }
}
