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
        public class DelayCommand : Command
        {
            float delayInSeconds = 0.0F;
            float currentTimer = 0.0F;

            public override bool TryParseCommand(MyCommandLine command)
            {
                if (command.ArgumentCount > 0)
                {
                    string value = command.Argument(1);
                    if(!float.TryParse(value, out delayInSeconds))
                    {
                        _buffer.LogError($"Delay invalid: {value} is not a float");
                    }
                    return true;
                }
                else
                {
                    _buffer.LogError("Delay invalid: No arguments found!");
                    return false;
                }
            }

            public override bool Execute(float deltaTime)
            {
                currentTimer += deltaTime;
                _buffer.LogInfo($"Delay: {Math.Round(currentTimer, 1)}/{delayInSeconds} s");
                if(currentTimer >= delayInSeconds)
                {
                    currentTimer = 0F;
                    return true;
                }
                return false;
            }

        }
    }
}
