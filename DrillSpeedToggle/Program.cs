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
        const float minMiningSpeedMS = 0.05F;
        const float maxMiningSpeedMS = 0.5F;
        const float movingSpeedMS = 1.0F;

        const float outputSmoothing = 0.99F;
        const float invMaxSmoothedVolumeM3S = 1.0F / 0.01F;

        class Drill
        {
            List<IMyPistonBase> _pistons;
            List<IMyShipDrill> _drills;
            Program _program;

            bool extend = true;
            bool inContact = false;
            MyFixedPoint trackingVolume = 0;

            public Drill(List<IMyPistonBase> pistons, List<IMyShipDrill> drills, Program program)
            {
                _pistons = pistons;
                _drills = drills;
                _program = program;
            }

            public void Update(float deltaTime)
            {
                // TODO: remove this part
                if(!extend && !inContact)
                {
                    foreach (var drill in _drills)
                    {
                        if(drill.GetInventory()?.CurrentVolume > 100)
                        {
                            inContact = true;
                            break;
                        }
                    }

                    SetPistonsVelocity(minMiningSpeedMS);
                }

                // TODO: this should look at every drill individually (seems expensive though)
                if(!extend)
                {
                    MyFixedPoint volume = 0;
                    foreach (var drill in _drills)
                    {
                        var drillVolume = drill.GetInventory().CurrentVolume;
                        if(drillVolume > volume)
                        {
                            volume = drillVolume;
                        }
                    }

                    //float inverseDrillCount = 1.0F / _drills.Count;

                    _program.Echo($"deltaTime: {deltaTime}");
                    _program.Echo($"volume this tick: {volume}");
                    trackingVolume = (trackingVolume * outputSmoothing) + ((volume * deltaTime) * (1.0F - outputSmoothing));
                    _program.Echo($"Volume/s: {trackingVolume}");
                    float velocity = 1.0F - Math.Min(((float)trackingVolume * invMaxSmoothedVolumeM3S), 1.0F);
                    _program.Echo($"Normalized: {velocity}");
                    velocity = (minMiningSpeedMS + velocity * (maxMiningSpeedMS - minMiningSpeedMS));
                    _program.Echo($"Velocity m/s: {velocity}");

                    SetPistonsVelocity(velocity);
                }
            }

            public void Toggle()
            {
                _program.Echo("Toggle Drill");
                SetPistonsVelocity(movingSpeedMS);
                
                if (extend)
                {
                    trackingVolume = 0;
                    _program.Echo("Extend");
                    inContact = false;
                    SetExtendPistons(true);
                    SetDrillsEnabled(true);
                }
                else
                {
                    _program.Echo("Retract");
                    SetExtendPistons(false);
                    SetDrillsEnabled(false);
                }

                extend = !extend;
            }

            void SetPistonsVelocity(float velocity)
            {
                foreach (var piston in _pistons)
                {
                    piston.Velocity = velocity;
                }
            }

            void SetDrillsEnabled(bool enabled)
            {
                foreach (var drill in _drills)
                {
                    drill.Enabled = enabled;
                }
            }

            void SetExtendPistons(bool extend)
            {
                foreach (var piston in _pistons)
                {
                    if (extend)
                        piston.Extend();
                    else
                        piston.Retract();
                }
            }
        }

        readonly MyCommandLine _myCommandLine = new MyCommandLine();

        // Runtime
        MyIni _ini;
        Dictionary<string, Drill> _drillsDictionary;

        // Construct
        public Program()
        {
            _ini = new MyIni();
            _drillsDictionary = new Dictionary<string, Drill>();

            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            MyIniParseResult result;
            if(!_ini.TryParse(Me.CustomData, out result))
            {
                Echo($"CustomData error:\nLine {result}");
                return;
            }

            List<string> sections = new List<string>();
            _ini.GetSections(sections);

            foreach (var section in sections)
            {
                if(_drillsDictionary.ContainsKey(section))
                {
                    Echo($"{section} is a duplicate. Groups need a unique name!");
                    continue;
                }

                var pistonsGroup = _ini.Get(section, "pistons").ToString();
                var drillsGroup = _ini.Get(section, "drills").ToString();

                if(pistonsGroup != null && drillsGroup != null)
                {
                    List<IMyPistonBase> pistons = new List<IMyPistonBase>();
                    List<IMyShipDrill> drills = new List<IMyShipDrill>();

                    var group = GridTerminalSystem.GetBlockGroupWithName(pistonsGroup);
                    group?.GetBlocksOfType(pistons);
                    group = GridTerminalSystem.GetBlockGroupWithName(drillsGroup);
                    group?.GetBlocksOfType(drills);

                    Echo($"Found {pistons.Count} pistons and {drills.Count} drills in {section}");

                    if(pistons.Count > 0 && drills.Count > 0)
                    {
                        _drillsDictionary.Add(section, new Drill(pistons, drills, this));
                    }
                }
            }
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            float deltaTime = (float)Runtime.TimeSinceLastRun.TotalSeconds;

            if(updateSource == UpdateType.Update1)
            {
                foreach (var drill in _drillsDictionary.Values)
                {
                    drill.Update(deltaTime);
                }
            }
            else if(_myCommandLine.TryParse(argument))
            {
                HandleCommands(_myCommandLine.Argument(0));
            }
        }

        void HandleCommands(string command)
        {
            if(command == null)
            {
                Echo("No command specified");
            }
            else if(command.Equals("toggle", StringComparison.OrdinalIgnoreCase))
            {
                ToggleDrill();
            }
            else
            {
                Echo($"Unknown command {command}");
            }
        }

        void ToggleDrill()
        {
            string drillKey = _myCommandLine.Argument(1);

            if (drillKey != null)
            {
                Drill drill;
                if (_drillsDictionary.TryGetValue(_myCommandLine.Argument(1), out drill))
                {
                    Echo($"Drill {drillKey} toggeled");
                    drill.Toggle();
                }
                else
                {
                    Echo($"Drill {drillKey} does not exist!");
                }
            }
            else
            {
                Echo("No Drill specified!");
            }
        }
    }
}
