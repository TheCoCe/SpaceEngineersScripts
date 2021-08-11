# SpaceEngineersScripts
A small collection of the Space Engineers Scripts I've written for my private game

## Airlock Script
Simple airlock script supporting a two way, lossless passage. Airlocks must consist out of two doors, a vent and sensor inside the airlock.
The Scripts also support two optional vertical 'Corner LCD Flat' Screens for airlock status.

Airlocks need to be added into the custom data of the Programmable Block in one of the following formats (Order of arguments does not matter):
[name=Airlock Right,innerDoor=ALR Door Inner,outerDoor=ALR Door Outer,airVent=ALR Air Vent,airlockSensor=ALR Sensor]
[name=Airlock Right,innerDoor=ALR Door Inner,outerDoor=ALR Door Outer,airVent=ALR Air Vent,airlockSensor=ALR Sensor,statusPanelInner=ALR Panel Inner,statusPanelOuter=ALR Panel Outer]

Airlock entry can be activated however you want. There are a bunch of commands to control it:

cycle "name" [in|out]	-> Activates the airlock cycle given a valid airlock name and an entry direction.
reset "name" {-all}		-> Resets an airlock given a valid name. Can reset all registered airlocks using the -all switch.
toggle "name" {-all}	-> Opens and disables an airlock given a valid name. Can disable all airlocks using the -all switch.
emergency "name" {-all}	-> Emergency opens an airlock given a valid name. Can open all using the -all switch.
