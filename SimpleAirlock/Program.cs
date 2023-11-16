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
    partial class Program : MyGridProgram
    {
        const float airlockEntryResetTimeS = 5.0F;
        const float airlockMaxDepressurizationTimeS = 2.0F;

        enum AirlockState
        {
            In,
            Out,
            Resetting,
            Emergency,
            None,
        }

        enum AirlockStep
        {
            Opening,
            PreparePressure,
            Pressure,
            PrepareExit,
            WaitingForExit,
            Closing,
            Done,
        }

        enum AirlockMessage
        {
            none,
            ready,
            inuse,
            enter,
            wait,
            emergency,
            disabled,
        }

        static readonly Dictionary<AirlockMessage, MyTuple<string, Color, Color>> messages = new Dictionary<AirlockMessage, MyTuple<string, Color, Color>>
        {
            { AirlockMessage.ready,     new MyTuple<string, Color, Color>("\n\n\n\nR\ne\na\nd\ny",                  Color.White, Color.Green    )},
            { AirlockMessage.inuse,     new MyTuple<string, Color, Color>("\n\n\n\nI\nn\n \nU\ns\ne",               Color.White, Color.Red      )},
            { AirlockMessage.enter,     new MyTuple<string, Color, Color>("\nP\nl\ne\na\ns\ne\n \nE\nn\nt\ne\nr",   Color.White, Color.Blue     )},
            { AirlockMessage.wait,      new MyTuple<string, Color, Color>("\n\nP\nl\ne\na\ns\ne\n \nW\na\ni\nt",    Color.White, Color.Red      )},
            { AirlockMessage.emergency, new MyTuple<string, Color, Color>("\n\nE\nm\ne\nr\ng\ne\nn\nc\ny",          Color.White, Color.Red      )},
            { AirlockMessage.disabled,  new MyTuple<string, Color, Color>("\n\n\nD\ni\ns\na\nb\nl\ne\nd",           Color.White, Color.Black    )},
        };

        class Airlock
        {
            Program _parent;

            IMyDoor _doorInner;
            IMyDoor _doorOuter;
            IMyAirVent _airlockVent;
            IMySensorBlock _innerSensor;
            IMyTextPanel _textPanelInner;
            IMyTextPanel _textPanelOuter;

            float timer;
            float currentTimerValue;

            public string AirlockName { get; private set; }
            public bool Disabled { get; private set; } = false;
            public AirlockState _currentAirlockState { get; private set; } = AirlockState.None;
            public AirlockStep _currentAirlockStep { get; private set; } = AirlockStep.Done;

            public Airlock(string airlockName, IMyDoor doorInner, IMyDoor doorOuter, IMyAirVent airlockVent, IMySensorBlock innerSensor, IMyTextPanel textPanelInner, IMyTextPanel textPanelOuter, Program parent)
            {
                _parent = parent;
                AirlockName = airlockName;
                _doorInner = doorInner;
                _doorOuter = doorOuter;
                _airlockVent = airlockVent;
                _innerSensor = innerSensor;
                _textPanelInner = textPanelInner;
                _textPanelOuter = textPanelOuter;
                timer = 0.0F;
                currentTimerValue = 0.0F;
                Reset();
            }

            public void ActivateAirlock(AirlockState state)
            {
                if (!Disabled && _currentAirlockState == AirlockState.None)
                {
                    _currentAirlockState = state;
                    SetWorking();
                }
            }

            void SetWorking()
            {
                SwitchAirlockStep(AirlockStep.Opening, AirlockMessage.enter, AirlockMessage.inuse);
            }

            void ResetWorking()
            {
                SwitchAirlockStep(AirlockStep.Done, AirlockMessage.ready, AirlockMessage.ready);
                _currentAirlockState = AirlockState.None;
            }

            public void Update(TimeSpan elapsedTime)
            {
                if (!Disabled && _currentAirlockState != AirlockState.None)
                {
                    currentTimerValue += (float)elapsedTime.TotalSeconds;

                    switch (_currentAirlockState)
                    {
                        case AirlockState.In:
                            HandleAirlock(ref _doorInner, ref _doorOuter, true);
                            break;
                        case AirlockState.Out:
                            HandleAirlock(ref _doorOuter, ref _doorInner, false);
                            break;
                        case AirlockState.Resetting:
                            HandleReset();
                            break;
                    }
                }
            }

            void HandleAirlock(ref IMyDoor entering, ref IMyDoor leaving, bool depressurize)
            {
                switch (_currentAirlockStep)
                {
                    case AirlockStep.Opening:
                        {
                            if (leaving.Status == DoorStatus.Open)
                            {
                                leaving.Enabled = true;
                                leaving.CloseDoor();
                            }
                            else if (leaving.Status == DoorStatus.Closed)
                            {
                                leaving.Enabled = false;
                                // Enable and open inner door
                                if (entering.Status == DoorStatus.Closed)
                                {
                                    entering.Enabled = true;
                                    entering.OpenDoor();
                                }
                                else if (entering.Status == DoorStatus.Open)
                                {
                                    SwitchAirlockStep(AirlockStep.PreparePressure, AirlockMessage.enter, AirlockMessage.inuse);
                                    SetTimer(airlockEntryResetTimeS);
                                }
                            }
                            break;
                        }
                    case AirlockStep.PreparePressure:
                        {
                            if (_innerSensor.IsActive)
                            {
                                if (entering.Status == DoorStatus.Open)
                                {
                                    entering.CloseDoor();
                                }
                                else if (entering.Status == DoorStatus.Closed)
                                {
                                    entering.Enabled = false;
                                    _airlockVent.Enabled = true;
                                    _airlockVent.Depressurize = depressurize;
                                    SwitchAirlockStep(AirlockStep.Pressure, AirlockMessage.inuse);
                                    SetTimer(airlockMaxDepressurizationTimeS);
                                }
                            }
                            else if (CheckTimer())
                            {
                                Reset();
                            }
                            break;
                        }
                    case AirlockStep.Pressure:
                        {
                            if ((depressurize ? _airlockVent.GetOxygenLevel() <= 0.0F : _airlockVent.GetOxygenLevel() >= 0.95F) || CheckTimer())
                            {
                                _airlockVent.Enabled = false;
                                SwitchAirlockStep(AirlockStep.PrepareExit);
                            }
                            break;
                        }
                    case AirlockStep.PrepareExit:
                        {
                            if (leaving.Status == DoorStatus.Closed)
                            {
                                leaving.Enabled = true;
                                leaving.OpenDoor();
                            }
                            else if (leaving.Status == DoorStatus.Open)
                            {
                                leaving.Enabled = false;
                                SwitchAirlockStep(AirlockStep.WaitingForExit);
                            }
                            break;
                        }
                    case AirlockStep.WaitingForExit:
                        {
                            if (!_innerSensor.IsActive)
                            {
                                SwitchAirlockStep(AirlockStep.Closing);
                            }
                            break;
                        }
                    case AirlockStep.Closing:
                        {
                            if (leaving.Status == DoorStatus.Open)
                            {
                                leaving.Enabled = true;
                                leaving.CloseDoor();
                            }
                            else if (leaving.Status == DoorStatus.Closed)
                            {
                                leaving.Enabled = false;
                                ResetWorking();
                            }
                            break;
                        }
                }
            }

            bool CheckTimer()
            {
                return currentTimerValue >= timer;
            }

            void SetTimer(float time = 1.0F)
            {
                timer = time;
                currentTimerValue = 0.0F;
            }

            public void Reset()
            {
                _currentAirlockState = AirlockState.Resetting;

                if (_doorInner.Status != DoorStatus.Closed)
                {
                    _doorInner.Enabled = true;
                    _doorInner.CloseDoor();
                }

                if (_doorOuter.Status != DoorStatus.Closed)
                {
                    _doorOuter.Enabled = true;
                    _doorOuter.CloseDoor();
                }
            }

            void HandleReset()
            {
                if (_doorInner.Status == DoorStatus.Closed)
                {
                    _doorInner.Enabled = false;
                }

                if (_doorOuter.Status == DoorStatus.Closed)
                {
                    _doorOuter.Enabled = false;
                }

                if (_doorInner.Status == DoorStatus.Closed && _doorOuter.Status == DoorStatus.Closed)
                {
                    _currentAirlockState = AirlockState.None;
                    SwitchAirlockStep(AirlockStep.Done, AirlockMessage.ready, AirlockMessage.ready);
                }
            }

            void SwitchAirlockStep(AirlockStep newStep, AirlockMessage enterMessage = AirlockMessage.none, AirlockMessage leavingMessage = AirlockMessage.none)
            {
                _currentAirlockStep = newStep;

                SetPanelMessage(_currentAirlockState == AirlockState.In ? _textPanelInner : _textPanelOuter, enterMessage);
                SetPanelMessage(_currentAirlockState == AirlockState.In ? _textPanelOuter : _textPanelInner, leavingMessage);
            }

            void SetPanelMessage(IMyTextPanel panel, AirlockMessage message)
            {
                MyTuple<string, Color, Color> t;
                if (panel != null && message != AirlockMessage.none && messages.TryGetValue(message, out t))
                {
                    panel.WriteText(t.Item1);
                    panel.FontColor = t.Item2;
                    panel.BackgroundColor = t.Item3;
                }
            }

            /// <summary>
            /// Sets the message on all airlock text panels
            /// </summary>
            public void SetMessage(AirlockMessage message)
            {
                SetPanelMessage(_textPanelInner, message);
                SetPanelMessage(_textPanelOuter, message);
            }

            public void EmergencyOpen()
            {
                _currentAirlockState = AirlockState.Emergency;
                _doorInner.Enabled = true;
                _doorOuter.Enabled = true;

                if (_doorInner.Status != DoorStatus.Open)
                {
                    _doorInner.OpenDoor();
                }

                if (_doorOuter.Status != DoorStatus.Open)
                {
                    _doorOuter.OpenDoor();
                }

                SetPanelMessage(_textPanelInner, AirlockMessage.emergency);
                SetPanelMessage(_textPanelOuter, AirlockMessage.emergency);
            }

            public void ToggleDisable()
            {
                if (Disabled)
                {
                    Disabled = false;
                    Reset();
                }
                else
                {
                    Disabled = true;

                    _currentAirlockState = AirlockState.None;
                    _doorInner.Enabled = true;
                    _doorOuter.Enabled = true;
                    _airlockVent.Enabled = false;

                    if (_doorInner.Status != DoorStatus.Open)
                    {
                        _doorInner.OpenDoor();
                    }

                    if (_doorOuter.Status != DoorStatus.Open)
                    {
                        _doorOuter.OpenDoor();
                    }

                    SetPanelMessage(_textPanelInner, AirlockMessage.disabled);
                    SetPanelMessage(_textPanelOuter, AirlockMessage.disabled);
                }
            }
        }

        // Runtime stuff
        MyCommandLine _commandLine;
        Dictionary<string, Action> _commands;
        Dictionary<string, Airlock> airlocks;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            _commands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
            _commands.Add("cycle", CycleAirlock);
            _commands.Add("reset", ResetAirlock);
            _commands.Add("emergency", EmergencyOpenAirlock);
            _commands.Add("toggle", ToggleDisableAirlock);
            _commands.Add("setMessage", SetMessage);

            _commandLine = new MyCommandLine();

            airlocks = new Dictionary<string, Airlock>();

            var customData = Me.CustomData;
            var matches = System.Text.RegularExpressions.Regex.Matches(customData, @"\[(.*?)\]");

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                string airlockData = match.Groups[1].Value;

                string airlockName = "airlock";
                IMyDoor doorInner = null;
                IMyDoor doorOuter = null;
                IMyAirVent airlockVent = null;
                IMySensorBlock innerSensor = null;
                IMyTextPanel textPanelInner = null;
                IMyTextPanel textPanelOuter = null;

                string[] customParams = airlockData.Split(',');
                foreach (var p in customParams)
                {
                    var pA = p.Trim().Split('=');
                    if (pA.Length == 2)
                    {
                        var parameter = pA[0];
                        var value = pA[1];

                        if (parameter.ToLower().Equals("name") && airlocks.ContainsKey(value))
                        {
                            break;
                        }

                        switch (parameter.ToLower())
                        {
                            case "name":
                                airlockName = value;
                                break;
                            case "innerdoor":
                                doorInner = GridTerminalSystem.GetBlockWithName(value) as IMyDoor;
                                break;
                            case "outerdoor":
                                doorOuter = GridTerminalSystem.GetBlockWithName(value) as IMyDoor;
                                break;
                            case "airvent":
                                airlockVent = GridTerminalSystem.GetBlockWithName(value) as IMyAirVent;
                                break;
                            case "airlocksensor":
                                innerSensor = GridTerminalSystem.GetBlockWithName(value) as IMySensorBlock;
                                break;
                            case "statuspanelinner":
                                textPanelInner = GridTerminalSystem.GetBlockWithName(value) as IMyTextPanel;
                                break;
                            case "statuspanelouter":
                                textPanelOuter = GridTerminalSystem.GetBlockWithName(value) as IMyTextPanel;
                                break;
                        }
                    }
                }

                if (!airlocks.ContainsKey(airlockName))
                {
                    if (doorInner == null)
                    {
                        Echo($"Parameter or Block for 'doorInner' in airlock '{airlockName}' missing!");
                        continue;
                    }
                    if (doorOuter == null)
                    {
                        Echo($"Parameter or Block for 'doorOuter' in airlock '{airlockName}' missing!");
                        continue;
                    }
                    if (airlockVent == null)
                    {
                        Echo($"Parameter or Block for 'airVent' in airlock '{airlockName}' missing!");
                        continue;
                    }
                    if (innerSensor == null)
                    {
                        Echo($"Parameter or Block for 'airlockSensor' in airlock '{airlockName}' missing!");
                        continue;
                    }

                    if (doorInner != null && doorOuter != null && airlockVent != null && innerSensor != null)
                    {
                        Airlock airlock = new Airlock(airlockName, doorInner, doorOuter, airlockVent, innerSensor, textPanelInner, textPanelOuter, this);
                        airlocks.Add(airlockName, airlock);
                        Echo($"Successfully registered {airlockName}");
                    }
                }
                else
                {
                    Echo($"Airlocks need unique names. '{airlockName}' is a duplicate.");
                }
            }
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Update10)
            {
                foreach (var al in airlocks.Values)
                {
                    if (al._currentAirlockState != AirlockState.None)
                    {
                        al.Update(Runtime.TimeSinceLastRun);
                    }
                }
            }
            else if (_commandLine.TryParse(argument))
            {
                Action commandAction;

                string command = _commandLine.Argument(0);

                if (command == null)
                {
                    Echo("No command specified!");
                }
                else if (_commands.TryGetValue(command, out commandAction))
                {
                    commandAction();
                }
                else
                {
                    Echo($"Unknown command {command}");
                }
            }
        }

        void CycleAirlock()
        {
            switch (_commandLine.Argument(2).ToLower())
            {
                case null:
                case "":
                    Echo("No airlock direction specified!");
                    break;
                case "in":
                    GetAirlockByName(_commandLine.Argument(1))?.ActivateAirlock(AirlockState.In);
                    break;
                case "out":
                    GetAirlockByName(_commandLine.Argument(1))?.ActivateAirlock(AirlockState.Out);
                    break;
                default:
                    Echo("Direction needs to be either 'in' or 'out'!");
                    break;
            }
        }

        void ResetAirlock()
        {
            if (_commandLine.Switch("all"))
            {
                foreach (var airlock in airlocks)
                {
                    airlock.Value.Reset();
                }
            }
            else
            {
                GetAirlockByName(_commandLine.Argument(1))?.Reset();
            }
        }

        void EmergencyOpenAirlock()
        {
            if (_commandLine.Switch("all"))
            {
                foreach (var airlock in airlocks)
                {
                    airlock.Value.Reset();
                }
            }
            else
            {
                GetAirlockByName(_commandLine.Argument(1))?.EmergencyOpen();
            }
        }

        void ToggleDisableAirlock()
        {
            if (_commandLine.Switch("all"))
            {
                foreach (var airlock in airlocks)
                {
                    airlock.Value.ToggleDisable();
                }
            }
            else
            {
                GetAirlockByName(_commandLine.Argument(1))?.ToggleDisable();
            }
        }

        void SetMessage()
        {
            bool all = _commandLine.Switch("all");
            string message = _commandLine.Argument(all ? 1 : 2).ToLower();

            AirlockMessage airlockMessage = AirlockMessage.none;
            if (!Enum.TryParse(message, out airlockMessage))
            {
                Echo("No valid message specified!");
                return;
            }

            if (all)
            {
                foreach (var airlock in airlocks)
                {
                    airlock.Value.SetMessage(airlockMessage);
                }
            }
            else
            {
                GetAirlockByName(_commandLine.Argument(1))?.SetMessage(airlockMessage);
            }
        }

        /// <summary>
        /// Returns the Airlock with the specified name. Returns null if no airlock with that name is registered
        /// </summary>
        Airlock GetAirlockByName(string name)
        {
            Airlock airlock;

            if (name == null)
                Echo("No Airlock specified");
            else if (airlocks.TryGetValue(name, out airlock))
                return airlock;
            else
                Echo($"{name} is not a valid airlock!");

            return null;
        }
    }
}
