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
        public class PrintCommand : Command
        {
            string message = "";

            public override bool TryParseCommand(MyCommandLine command)
            {
                if(command.ArgumentCount > 1)
                {
                    for (int i = 1; i < command.ArgumentCount; i++)
                    {
                        message += " " + command.Argument(i);
                    }
                }
                else
                {
                    _buffer?.LogError($"PrintCommand invalid: No message defined!");
                    return false;
                }

                return true;
            }
            
            public override bool Execute(float deltaTime)
            {
                program.Echo(message);
                return true;
            }
        }
    }
}
