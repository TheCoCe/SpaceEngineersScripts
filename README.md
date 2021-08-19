# SpaceEngineersScripts
A small collection of the Space Engineers Scripts I've written for my private game using https://github.com/malware-dev/MDK-SE

## Airlock Script
___
Simple airlock script supporting a two way, lossless passage. Airlocks must consist out of two doors, a vent and sensor inside the airlock.
The Scripts also support two optional vertical 'Corner LCD Flat' Screens for airlock status.

## Drill Speed Toggle
___
Simple script for automating piston drill operation. The Script checks the current input and adjusts the piston speed to match the drill load.

## Action Manager
___
The action manager script is a simple way to program simple action sequences without the need for timer blocks. The script supports multiple sequences per programmable block that you can run via the agruments of the block. You can execute any action you can execute via the terminal and set or check properties of the block or group. 

### Commands:
The two general commands for this are:
- `Action <T:Block|G:Group> <action>` 
- `Property <T:Block|G:Group> <property> <comparer> <value>`

Using those with just the Block argument will print the possible actions. Other than that you can use the following commands:
- `Delay <seconds>`
- `Self <sequence> <action>`
- `Self <sequence> <propery> <comparer> <value>`

As the `Action` and `Property` commands are very general they are not the fastest and easiest to use. Some common blocks have fast custom commands to accommodate for that. These commands will only consider one specific block or block type. Other blocks (part of a group e.g.) will be ignored:
#### Actions:
- `Piston <T:Block|G:Group> <action>` 
    - ##### Actions: `toggle, on, off, extend, retract, reverse, attach, detach`
- `Rotor <T:Block|G:Group> <action>`
    - ##### Actions: `toggle, reverse, on, off, attach, detach`
- `Connector <T:Block|G:Group> <action>`
    - ##### Actions: `toggle, on, off, toggleconnect, connect, disconnect,`
- `Merge <T:Block|G:Group> <action>`
    - ##### Actions: `toggle, on, off,`
#### Properties
- `Piston <T:Block|G:Group> <property> <comparer> <value>`
    - ##### Properties: `enabled, velocity, maxvelocity, minlimit, maxlimit, currentposition`
- `Rotor <T:Block|G:Group> <property> <comparer> <value>`
    - ##### Properties: `enabled, angle, torque, breakingtorque, velocity, lowerlimit, upperlimit, displacement, rotorlock`
- `Connector <T:Block|G:Group> <property> <comparer> <value>`
    - ##### Properties: `enabled, throwout, collectall, pullstrength, isparkingenabled, connectionallowed, connected`
- `Merge <T:Block|G:Group> <property> <comparer> <value>`
    - ##### Properties: `enabled, connected`

### Sequence:
To create a sequence just add a `[Your sequence name]` tag in the programmable blocks CustomData and add the commands after that. You can also use comments inside of a sequence by starting the line with `'//'`.

#### Example:
```
[MyFirstSequence]
// This is a comment
Piston "Piston 1" extend
Piston "Piston 1" CurrentPosition == 10.0
Piston "Piston 1" retract
```

Adding multiple sequences is as easy as adding another sequence tag below.
```
[MyFirstSequence]
...
Piston "Piston 1" retract

[MySecondSequence]
Delay 10
...
```

The script will automatically parse the new sequence and show it in the DetailedInfo. 

Adding or removing commands or sequences will trigger a reparse of the CustomData. The script will try to keep as much of the unchanged data as possible, which means that it will try to keep any running sequences intact as long as they are not the sequence that was changed.

### Status:
The script will always show the current status of your sequences along with some runtime data in the DetailedInfo of the porgrammable block. Any infos, warnings or errors with a specific sequence will be shown right below the specific sequences status info.

### Running a sequence:
To run a sequence you can run the programmable block with the following arguments:
- `<action> <sequence>`

Available actions are:
- `run` will start a sequence
- `pause` will pause a running sequence
- `resume` will resume a pause sequence
- `stop` will stop a running sequence
- `restart` will restart any sequence no matter what
