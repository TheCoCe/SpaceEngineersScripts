# SpaceEngineersScripts
A small collection of the Space Engineers Scripts I've written for my private game using https://github.com/malware-dev/MDK-SE

## **Airlock Script**
Simple airlock script supporting a two way, lossless passage. Airlocks must consist out of two doors, a vent and sensor inside the airlock.
The Scripts also support two optional vertical 'Corner LCD Flat' Screens for airlock status.

## **Drill Speed Toggle**
Simple script for automating piston drill operation. The Script checks the current input and adjusts the piston speed to match the drill load.

## **Action Manager**
The action manager script is a simple way to program simple action sequences without the need for timer blocks. The script supports multiple sequences per programmable block that you can run via the agruments of the block. You can execute any action you can execute via the terminal and set or check properties of the block or group. 

### **Commands:**
The two general action and property commands are:
|Command|Arguments|Description|
|---|---|---|
|Action|`<T:Block\|G:Group> <action>`|**T:Block:** `T:` followed by the name of the block <br> **G:Group:** `G:` followed by the name of the group <br> **action:** the action to execute, if blank will print a list of the possible actions <br>|
|Property|`<T:Block\|G:Group> <property> <comparer> <value>`|**T:Block:** `T:` followed by the name of the block <br> **G:Group:** `G:` followed by the name of the group <br> **property:** name of the property, if blank will print a list of possible properties <br> **comparer:** `=`, `==`, `!=`, `<`, `<=`, `>`, `>=` <br> **value:** value to set or compare against|


As the `Action` and `Property` commands are very general they are not the fastest and easiest to use. Some common blocks have fast custom commands to accommodate for that. These commands will only consider one specific block or block type. Other blocks (part of a group e.g.) will be ignored:
#### **Block Actions:**
```
command <T:Block|G:Group> <action>
```
| Command | Actions |
|---|---|
|Piston|toggle, on, off, extend, retract, reverse, attach, detach|
|Rotor|toggle, reverse, on, off, attach, detach|
|Connector|toggle, on, off, toggleconnect, connect, disconnect|
|Merge|toggle, on, off|
|Drill|toggle, on, off|
#### **Block Properties:**
```
command <T:Block|G:Group> <property> <comparer> <value>
```
| Command | Properties |
|---|---|
|Piston|enabled, velocity, maxvelocity, minlimit, maxlimit, currentposition|
|Rotor|enabled, angle, torque, breakingtorque, velocity, lowerlimit, upperlimit, displacement, rotorlock|
|Connector|enabled, throwout, collectall, pullstrength, isparkingenabled, connectionallowed, connected|
|Merge|enabled, connected|
|Drill|enabled, activated|

#### **Delay:**
```
Delay <seconds>
```
Waits for `<seconds>` and then continues execution of the next command.

#### **Self:**
With the self command you can execute and check the status of sequences on the same programmable block.

```
Self <sequence> <action> 
Self <sequence> <propery> <comparer> <value>
```
`<sequence>`: name of the sequence <br>
`<action>`: run, pause, resume, stop, restart <br>
`<property>`: status, currentCommandIdx

#### **Inventory Command:**
With the inventory command you can compare the contents of different containers against a list provided via the arguments.

```
Inventory <T:Block|G:Group> <comparer> <{item:count,...}>
```
**Possible Items**:
```
Ores:
cobaltore, goldore, ice, ironore, magnesiumore, nickelore, platinumore, siliconore, silverore, stoneore, uraniumore

Ingots:
cobaltingot, goldingot, ironingot, magnesiumingot, nickelingot, platinumingot, scrap, siliconingot, silveringot, stoneingot, uraniumingot

Components:
bulletproofglass, computer, construction, detector, display, girder, gravitygenerator, interiorplate, largetube, Medical, metalgrid, motor, powercell, radiocommunication, reactor, smalltube, solarcell, steelplate, thrust

Tools:
welderitem, handdrillitem, anglegrinderitem, automaticrifleitem

Ammo:
missle200mm, nato_25x184mm, nato_5p56x45mm, explosives
```
Example:

```
Inventory T:MyContainer >= {steelplate:100,computer:10,display:3,interiorplate:50}
```

<br>

### **Sequence:**
To create a sequence just add a `[Your sequence name]` tag in the programmable blocks CustomData and add the commands after that. You can also use comments inside of a sequence by starting the line with `//`.

#### Example:
```
[MyFirstSequence]
// This is a comment
Piston "Piston 1" extend
Piston "Piston 1" CurrentPosition == 10.0
Piston "Piston 1" retract
```

Adding multiple sequences is as easy as adding another sequence tag below:
```
[MyFirstSequence]
...
Piston "Piston 1" retract

[MySecondSequence]
Delay 10
...
```

The script will automatically parse the new sequence and show it in the DetailedInfo. 

Adding or removing commands or sequences will trigger a reparse of the CustomData. The script will try to keep as much of the unchanged data as possible, which means that it will try to keep all sequences intact and running as long as they are not the sequence that was changed.

### **Status:**
The script will always show the current status of your sequences along with some runtime data in the DetailedInfo of the porgrammable block. Any infos, warnings or errors with a specific sequence will be shown right below the specific sequences status info.

### **Running a sequence:**
To run a sequence you can run the programmable block with the following arguments:
- `<action> <sequence>`

Available actions are:
- `run` will start a sequence
- `pause` will pause a running sequence
- `resume` will resume a pause sequence
- `stop` will stop a running sequence
- `restart` will restart any sequence no matter what
