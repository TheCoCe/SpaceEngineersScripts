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
        IMyFunctionalBlock block;
        IMyPistonBase piston;

        IMyBlockGroup group;
        int _hash = 0;
        List<IMyTerminalBlock> groupBlocks = new List<IMyTerminalBlock>();

        IMyCockpit cockpit;

        public Program()
        {
            block = GridTerminalSystem.GetBlockWithName("TestPiston") as IMyFunctionalBlock;
            piston = GridTerminalSystem.GetBlockWithName("TestPiston") as IMyPistonBase;
            cockpit = GridTerminalSystem.GetBlockWithName("TestCockpit") as IMyCockpit;

            group = GridTerminalSystem.GetBlockGroupWithName("TestGroup");
            _hash = group.GetHashCode();

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            /*
            Echo(block.Name);

            List<ITerminalAction> actions = new List<ITerminalAction>();

            piston.GetActions(actions);

            foreach (var action in actions)
            {
                Echo(action.Name.ToString());
            }

            piston.ApplyAction("Extend");

            if(group == GridTerminalSystem.GetBlockGroupWithName("TestGroup"))
            {
                
                Echo("The same");
            }
            else
            {
                Echo("Different");
            }
            */
            //groupBlocks.Clear();
            //group.GetBlocksOfType(groupBlocks);
            if (cockpit != null)
            {
                Vector3 vector = cockpit.MoveIndicator;
                float roll = cockpit.RollIndicator;
                Vector2 rotation = cockpit.RotationIndicator;

                cockpit.GetSurface(0).WriteText($"move: {vector}", false);
                cockpit.GetSurface(0).WriteText($"\nroll: {roll}", true);
                cockpit.GetSurface(0).WriteText($"\nrota: {rotation}", true);

                Echo("found!");
            }
            else
            {
                Echo($"Not found");
            }

            Echo($"{Runtime.LastRunTimeMs} ms");
        }
    }
}
