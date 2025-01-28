# MIDI Piano Keyboard to Project Sekai Interfacer
![Program Screenshot](https://github.com/KarstSkarn/MIDI-Piano-Keyboard-Project-Sekai-Interfacer/blob/main/programscreenshot.png?raw=true "Program Screenshot")
*By KarstSkarn - https://karstskarn.carrd.co*
*If you liked it you can support me on https://ko-fi.com/karstskarn*
*It motivates me for this and many other projects!*


Contact Discord: https://discord.com/invite/d9rNwkerZw

## Description
This tiny program allows to interface any MIDI device in order to play Project Sekai. 

**Important: ** For this to work** you must** play PSekai on PC using Android emulation with Bluestacks. [Bluestacks Emulator Free Download](https://www.bluestacks.com "Bluestacks Emulator Free Download")

This software is designed to **work exclusively** with a specific Bluestacks keyboard layout, which you can download here: [Bluestacks Keyboard Layout for Project Sekai ](https://github.com/KarstSkarn/Karst-PSekai-PC-Control-Scheme "Bluestacks Keyboard Layout for Project Sekai "). Depending on your keyboard’s layout or culture, you will need to adjust the keys both in the Bluestacks layout and in this software's configuration file (Configuration.xml). Make sure to map the keys to match your preferred setup. For reference, the default configuration provided is tailored to a Spanish-style keyboard.

## Keys Distribution

The program has two possible modes. You can switch modes by editing the file "*Configuration.xml*" and changing the value of *layoutMode*.

**Note:** By default is set to mode 2.

#### Mode 1

![](https://github.com/KarstSkarn/MIDI-Piano-Keyboard-Project-Sekai-Interfacer/blob/main/mode1.png?raw=true)

Project Sekai features 12 possible note positions, and this mode maps each keyboard key in an octave to correspond to one of these note positions in the game.

Each keyboard octave is treated the same, allowing you to use any octave or even different sections of the keyboard with both hands seamlessly—it will work perfectly either way.

#### Mode 2

![](https://github.com/KarstSkarn/MIDI-Piano-Keyboard-Project-Sekai-Interfacer/blob/main/mode2.png?raw=true)

This mode has been the most intuitive for me. It divides the keyboard at the piano key specified by keyboardCutPoint (which may vary depending on the piano and its size). All octaves below this point correspond to the first six channels of the game, while all octaves above it correspond to the upper six channels.

As with Mode 1, all octaves function the same way—they are effectively looped.

## Installation
This program is fully standalone and requires no installation. Simply execute it, and it will automatically detect all MIDI devices connected to your computer, allowing you to select one. From there, it will display all note events and their corresponding keys on the screen. While running, the program simulates computer keyboard key presses based on input from the MIDI device.

**Tip: **To test if it's working, try using it with a program like Notepad. This will help you confirm that the keypress simulation is functioning correctly.