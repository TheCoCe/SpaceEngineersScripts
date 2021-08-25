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
        static readonly Dictionary<string, MyTuple<string, string>> itemWhitelist = new Dictionary<string, MyTuple<string, string>>()
        {
            // ore
            { "cobaltore", new MyTuple<string, string>("MyObjectBuilder_Ore", "Cobalt") },
            { "goldore", new MyTuple<string, string>("MyObjectBuilder_Ore", "Gold") },
            { "ice", new MyTuple<string, string>("MyObjectBuilder_Ore", "Ice") },
            { "ironore", new MyTuple<string, string>("MyObjectBuilder_Ore", "Iron") },
            { "magnesiumore", new MyTuple<string, string>("MyObjectBuilder_Ore", "Magnesium") },
            { "nickelore", new MyTuple<string, string>("MyObjectBuilder_Ore", "Nickel") },
            { "platinumore", new MyTuple<string, string>("MyObjectBuilder_Ore", "Platinum") },
            { "siliconore", new MyTuple<string, string>("MyObjectBuilder_Ore", "Silicon") },
            { "silverore", new MyTuple<string, string>("MyObjectBuilder_Ore", "Silver") },
            { "stoneore", new MyTuple<string, string>("MyObjectBuilder_Ore", "Stone") },
            { "uraniumore", new MyTuple<string, string>("MyObjectBuilder_Ore", "Uranium") },
            // ingots
            { "cobaltingot", new MyTuple<string, string>("MyObjectBuilder_Ingot", "Cobalt") },
            { "goldingot", new MyTuple<string, string>("MyObjectBuilder_Ingot", "Gold") },
            { "ironingot", new MyTuple<string, string>("MyObjectBuilder_Ingot", "Iron") },
            { "magnesiumingot", new MyTuple<string, string>("MyObjectBuilder_Ingot", "Magnesium") },
            { "nickelingot", new MyTuple<string, string>("MyObjectBuilder_Ingot", "Nickel") },
            { "platinumingot", new MyTuple<string, string>("MyObjectBuilder_Ingot", "Platinum") },
            { "scrap", new MyTuple<string, string>("MyObjectBuilder_Ingot", "Scrap") },
            { "siliconingot", new MyTuple<string, string>("MyObjectBuilder_Ingot", "Silicon") },
            { "silveringot", new MyTuple<string, string>("MyObjectBuilder_Ingot", "Silver") },
            { "stoneingot", new MyTuple<string, string>("MyObjectBuilder_Ingot", "Stone") },
            { "uraniumingot", new MyTuple<string, string>("MyObjectBuilder_Ingot", "Uranium") },
            // Component
            { "bulletproofglass", new MyTuple<string, string>("MyObjectBuilder_Component", "BulletproofGlass") },
            { "computer", new MyTuple<string, string>("MyObjectBuilder_Component", "Computer") },
            { "construction", new MyTuple<string, string>("MyObjectBuilder_Component", "Construction") },
            { "detector", new MyTuple<string, string>("MyObjectBuilder_Component", "Detector") },
            { "display", new MyTuple<string, string>("MyObjectBuilder_Component", "Display") },
            { "girder", new MyTuple<string, string>("MyObjectBuilder_Component", "Girder") },
            { "gravitygenerator", new MyTuple<string, string>("MyObjectBuilder_Component", "GravityGenerator") },
            { "interiorplate", new MyTuple<string, string>("MyObjectBuilder_Component", "InteriorPlate") },
            { "largetube", new MyTuple<string, string>("MyObjectBuilder_Component", "LargeTube") },
            { "Medical", new MyTuple<string, string>("MyObjectBuilder_Component", "Medical") },
            { "metalgrid", new MyTuple<string, string>("MyObjectBuilder_Component", "MetalGrid") },
            { "motor", new MyTuple<string, string>("MyObjectBuilder_Component", "Motor") },
            { "powercell", new MyTuple<string, string>("MyObjectBuilder_Component", "PowerCell") },
            { "radiocommunication", new MyTuple<string, string>("MyObjectBuilder_Component", "RadioCommunication") },
            { "reactor", new MyTuple<string, string>("MyObjectBuilder_Component", "Reactor") },
            { "smalltube", new MyTuple<string, string>("MyObjectBuilder_Component", "SmallTube") },
            { "solarcell", new MyTuple<string, string>("MyObjectBuilder_Component", "SolarCell") },
            { "steelplate", new MyTuple<string, string>("MyObjectBuilder_Component", "SteelPlate") },
            { "thrust", new MyTuple<string, string>("MyObjectBuilder_Component", "Thrust") },
            // gun objects
            { "welderitem", new MyTuple<string, string>("MyObjectBuilder_PhysicalGunObject", "WelderItem") },
            { "handdrillitem", new MyTuple<string, string>("MyObjectBuilder_PhysicalGunObject", "HandDrillItem") },
            { "anglegrinderitem", new MyTuple<string, string>("MyObjectBuilder_PhysicalGunObject", "AngleGrinderItem") },
            { "automaticrifleitem", new MyTuple<string, string>("MyObjectBuilder_PhysicalGunObject", "AutomaticRifleItem") },
            // ammo
            { "missle200mm", new MyTuple<string, string>("MyObjectBuilder_AmmoMagazine", "Missle200mm") },
            { "nato_25x184mm", new MyTuple<string, string>("MyObjectBuilder_AmmoMagazine", "NATO_25x184mm") },
            { "nato_5p56x45mm", new MyTuple<string, string>("MyObjectBuilder_AmmoMagazine", "NATO_5p56x45mm") },
            { "explosives", new MyTuple<string, string>("MyObjectBuilder_AmmoMagazine", "Explosives") },
        };

        public class InventoryCommand : BaseBlockCommand<IMyTerminalBlock>
        {
            List<IMyInventory> inventories;
            Dictionary<MyItemType, MyFixedPoint> items;

            private ComparisonModes _compareMode = ComparisonModes.none;

            public InventoryCommand()
            {
                items = new Dictionary<MyItemType, MyFixedPoint>();
                inventories = new List<IMyInventory>();
            }

            public override bool Execute(float deltaTime)
            {
                bool result = true;

                foreach (var item in items)
                {
                    MyFixedPoint count = 0;

                    foreach (var inventory in inventories)
                    {
                        count += inventory.GetItemAmount(item.Key);
                    }

                    result &= CheckProperty(count, item.Value);

                    if (!result)
                        break;
                }

                return result;
            }

            private bool CheckProperty(MyFixedPoint v1, MyFixedPoint v2)
            {
                switch (_compareMode)
                {
                    case ComparisonModes.equals:
                        _buffer.LogInfo($"Checking {v1} == {v2}");
                        return v1 == v2;
                    case ComparisonModes.notequals:
                        _buffer.LogInfo($"Checking {v1} != {v2}");
                        return v1 != v2;
                    case ComparisonModes.greater:
                        _buffer.LogInfo($"Checking {v1} > {v2}");
                        return v1 > v2;
                    case ComparisonModes.less:
                        _buffer.LogInfo($"Checking {v1} < {v2}");
                        return v1 < v2;
                    case ComparisonModes.greaterequals:
                        _buffer.LogInfo($"Checking {v1} >= {v2}");
                        return v1 >= v2;
                    case ComparisonModes.lessequals:
                        _buffer.LogInfo($"Checking {v1} <= {v2}");
                        return v1 <= v2;
                    default:
                        return true;
                }
            }

            public override bool TryParseCommand(MyCommandLine command)
            {
                if (!base.TryParseCommand(command))
                    return false;

                if(Mode == BlockMode.Block)
                {
                    if (!Block.HasInventory)
                    {
                        _buffer.LogError($"'{Block.CustomName}' doesn have a Inventory!");
                        return false;
                    }

                    inventories.Clear();
                    for (int i = 0; i < Block.InventoryCount; i++)
                    {
                        inventories.Add(Block.GetInventory(i));
                    }
                }
                else
                {
                    inventories.Clear();
                    foreach (var block in _groupBlocks)
                    {
                        if(block.HasInventory)
                        {
                            for (int i = 0; i < block.InventoryCount; i++)
                            {
                                inventories.Add(block.GetInventory(i));
                            }
                        }
                    }

                    if(inventories.Count == 0)
                    {
                        _buffer.LogError($"No inventories found in group {Group.Name}");
                        return false;
                    }
                }

                string comparison = command.Argument(2).ToLower();
                if (!Enum.TryParse(comparison, true, out _compareMode))
                {
                    switch (comparison)
                    {
                        case "==":
                            _compareMode = ComparisonModes.equals;
                            break;
                        case "!=":
                            _compareMode = ComparisonModes.notequals;
                            break;
                        case ">":
                            _compareMode = ComparisonModes.greater;
                            break;
                        case ">=":
                            _compareMode = ComparisonModes.greaterequals;
                            break;
                        case "<":
                            _compareMode = ComparisonModes.less;
                            break;
                        case "<=":
                            _compareMode = ComparisonModes.lessequals;
                            break;
                        default:
                            _buffer.LogError($"{_compareMode} is not a valid compare mode!");
                            return false;
                    }
                }

                // itemName:100,itemName2:200,itemName3:12
                string[] itemsList = command.Argument(3).Split(',');
                items.Clear();
                foreach (var item in itemsList)
                {
                    int idx = item.IndexOf(':');
                    if(idx > 0 && item.Length > idx + 1)
                    {
                        string itemName = item.Substring(0, idx).ToLower().Trim();
                        if(itemWhitelist.ContainsKey(itemName))
                        {
                            double itemAmount;
                            if (double.TryParse(item.Substring(idx + 1, item.Length - (idx + 1)), out itemAmount))
                            {
                                MyItemType itemType = new MyItemType(itemWhitelist[itemName].Item1, itemWhitelist[itemName].Item2);
                                if (items.ContainsKey(itemType))
                                {
                                   items[itemType] += (MyFixedPoint)itemAmount;
                                }
                                else
                                {
                                    items.Add(itemType, (MyFixedPoint)itemAmount);
                                }
                            }
                        }
                        else
                        {
                            _buffer.LogWarning($"'{itemName}' is not a valid item!");
                        }
                    }
                }

                if(items.Count == 0)
                {
                    _buffer.LogError($"No items defined!");
                    return false;
                }

                return true;
            }
        }
    }
}
