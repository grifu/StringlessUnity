Stringless - Remote Control for Unity 3D
-----------------------------
* authors: Grifu (Luis Leite)
* forum: http://www.grifu.com/vmforum/
* source code:
* first release: 23.04.2016
* this version: 04.12.2016
* version: 2.1 Beta


Stringless vs Remote Control
------------
Remote Control for Unity is now STRINGLESS.
The first version, which is avaiable for download at https://github.com/grifu/RemoteControlUnity was built with Jorge Garcia UnityOSC.
There were some performance issues with UnityOSC and Stringless is now built over Thomas Fredericks UnityOSC.
I had to adapt Fredericks OSC plugin to fit in Stringless.

Compatibility with Unity versions 4 and 5 (Windows / Mac)


ABSTRACT
--------
Stringless is a Remote Control extension for Unity 3D that allows you to control objects inside Unity from the outside. 
A simple, flexible and scalable plugin that exposes any property that exists in the objects to the outside making it possible to control other applications or to be used as a debugging tool.
The plugin is very easy to use, with just two steps: create a network port and add OSC send or receive objects. This plugin was developed as a part of a digital EcoSystem to connect and orchestrate digital data in real-time for performance animation, a digital puppetry environment. A set of Digital Puppet Tools (DPT) developed during a PhD research (UTAustin|Portugal / UPorto) on Digital Puppetry. The first release was in July 2015.


OBJECTIVE
---------
The goal of this plugin is to facilitate the way you map or connect objects. There is no need for coding, just attach the behavior to any object, camera, or light.

There are just 4 behaviors
- OSC: setup OSC communication ports (adapted from Thomas Fredericks UnityOSC)
- RCSender: send messages to the outside exposing properties
- RCReceiver: receive messages from the outside that control the objects


INSTALLATION
------------
Copy the Stringless to your Assets Folder (The folder Editor should be inside Assets),
or import stringless packadge into your Unity project


HOW TO USE
----------
1-> Initialize Stringless by dragging OSC to any GameObject and setup ports with valid numbers (i.e. 7010)

2-> Establish the mapping between the action/object and the message by dragging the RCReceiver or the RCSender (RCReceiver: to control the object with a OSC message such as a TouchOSC fader from your Ipad) 

3-> Setup mapping between the input and the result using enable mapping. You can manually setup or use the learn button for interactively setup. When learning the values change automatically by modifying the objects. For instance, moving the object in the viewport to the extreme positions. Remember to deactivate the learn buttom after the setup. 

Open OSC tracer window (Window->Stringless) to trace OSC in/out ports


CONTACT / +INFO
---------------
*video (first version): https://vimeo.com/grifu/remotecontrol0
*mail: virtual.marionette@grifu.com
*web: http://www.virtualmarionette.grifu.com


LICENSE
-------
GNU GENERAL PUBLIC LICENSE Version 3
See License.txt for more details.


WHATS NEW 
-----
- Ported to the Thomas Fredericks UnityOSC
- Solved the performance issues
- Included a mapping feature (just for the output values from RCreceiver)
- Identify incoming OSC messages (a learn button to capture the OSC address)
- Added field support (now you can expose your own variable trhough OSC)
- Solved the offset problem

ISSUES
------
This is still a beta version with many issues
- Include the mapping scheme for RCsender and input values of RCreceiver
- only the animated parameters are sent
- methods are limited to blend shapes
- some issues within the inspector of the RCCReceiver and RCCSender

TODO
----
- Scale the input mapping (include fields to scale values)
- Optimize performance
- Handle methods in a generilized way