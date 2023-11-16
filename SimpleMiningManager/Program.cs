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
        const float stopStoragePercent = 1.0F;
        const float startStoragePercent = 0.5F;

        MyIni _ini;
        IMyBlockGroup _storage;
        List<IMyCargoContainer> _cargoBlocks;
        IMyBlockGroup _mining;
        List<IMyFunctionalBlock> _miningBlocks;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            _cargoBlocks = new List<IMyCargoContainer>();
            _miningBlocks = new List<IMyFunctionalBlock>();

            _ini = new MyIni();

            if (_ini.TryParse(Me.CustomData))
            {
                var miningGroupName = _ini.Get("SMM", "miningGroup").ToString();
                var storageGroup = _ini.Get("SMM", "storageGroup").ToString();

                _mining = GridTerminalSystem.GetBlockGroupWithName(miningGroupName);
                _storage = GridTerminalSystem.GetBlockGroupWithName(storageGroup);
            }
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (_storage != null)
            {
                _storage.GetBlocksOfType(_cargoBlocks);

                MyFixedPoint maxVolume = 0;
                MyFixedPoint currentVolume = 0;

                foreach (var storageContainer in _cargoBlocks)
                {
                    Echo($"Container: {storageContainer.Name}");
                    maxVolume += storageContainer.GetInventory().MaxVolume;
                    currentVolume += storageContainer.GetInventory().CurrentVolume;
                }

                Echo($"MaxVolume: {maxVolume}");
                Echo($"CurrentVolume: {currentVolume}");

                if (maxVolume > 0)
                {
                    double percent = (double)currentVolume / (double)maxVolume;

                    if (percent >= stopStoragePercent)
                    {
                        DeActivateMining(false);
                    }
                    else if (percent <= startStoragePercent)
                    {
                        DeActivateMining(true);
                    }
                }
            }
        }

        void DeActivateMining(bool active)
        {
            _mining.GetBlocksOfType(_miningBlocks);

            foreach (var block in _miningBlocks)
            {
                block.Enabled = active;
            }
        }
    }
}
