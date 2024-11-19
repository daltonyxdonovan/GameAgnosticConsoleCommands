# Game Agnostic Console Commands (GACC)

## Overview

Game Agnostic Console Commands (GACC) is a barebones BepInEx 5.4 plugin for dynamically loading and executing custom commands in games, without having to hardcode them. This system allows for extensibility, enabling savvy folks to add their own custom functionality without needing to modify the core plugin code. It has only 2 requirements to run properly in a Unity Game that has BepInEx 5.4 installed:

- The game must use TextMeshProUGUI as it's text handler (as we use it to display _our_ text)
- The game must use a Canvas object as it's UI handler (and it must be one that displays, as we just pick the one we can find!)

## To Install

Just merge the enclosed BepInEx folder with your game's BepInEx 5 folder! (YourGameFolder/BepInEx)

Custom Commands go in (YourGameFolder/BepInEx/config/CommandScripts)

## How to Create a Custom Command (Windows)

Open Visual Studio and create a new project.
You want this project to be a C# Class Library, and it should target netstandard 2.1

If you cannot find this project template, you likely need to install some .net dev packages in the visual studio installer GUI

1. Create a New Class for Your Command and write your code

Each custom command needs to implement the ICommand interface. This interface requires two properties:

    Name: A unique string that identifies the command.
    Execute(): A method that runs the command when called.

### Example Command Class:

```
using BepInEx.Logging;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class ExampleCommand : ICommand
{
    private readonly ManualLogSource _logger;

    public ExampleCommand(ManualLogSource logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "/setPollution";

    public void Execute(params string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                _logger.LogError("No arguments provided. Usage: /setPollution <int>");
                return;
            }

            if (!int.TryParse(args[0], out int pollutionLevel))
            {
                _logger.LogError("Invalid argument. Please provide a valid integer.");
                return;
            }

            Game.current.pollution = pollutionLevel;
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while executing the command: {ex.Message}");
        }
    }
}

```

2. Build the Command DLL

After writing the command code, compile it into a DLL.

    Add references to any necessary libraries by right clicking 'Dependencies' in the solution explorer, then 'Add Project Reference', then 'Browse' for your dll to reference. For example:
    
        - BepInEx.dll (for logging, you can find this in your bepinex/core folder of your game).
        - ICommand.dll (to inherit from).
        
    Build the project by running 'dotnet build' in a terminal that is based in your command folder, and you'll get a .dll file. congrats! this is your custom command

3. Place the DLL in the CommandScripts Folder

        Navigate to the folder where your plugin needs installed (YourGameFolder/BepInEx/config/CommandScripts).
   
        Drop the compiled .dll into this directory.

5. Load the Command in Your Plugin

        Once the DLL is placed in the appropriate directory, the plugin will automatically detect it and attempt to load the commands at runtime.

       Just start the game and read the bepinex log to see if it worked! (it's near the top, and you can just as easily try to run the command to test it instead!)

5. Using Your Command

    After loading the command DLL, you can use your custom command via the console interface. For example:

       - Open the console _in game_ (by pressing the key: /).
          -- note that this is why I traditionally start all commands with /, as that will be entered when you open the command input
       - Type ExampleCommand (or whatever you named your command).
       - Press Enter to execute the command.
       - The system will call the Execute() method of the CommandExample class, which will log the message: "Example command executed!".

## Logger Injection

In your commands, you can inject the ManualLogSource (logger) to log messages. This allows custom commands to output log messages to the console for debugging or information purposes. The logger prints to the actual bepinex terminal (who would have known!)

# Troubleshooting

    Command Not Found: Ensure that the DLL is placed correctly in the CommandScripts folder and that the class implementing ICommand has a public constructor. This folder should be 'YourGameFolder/BepInEx/config/CommandScripts'

    No Output in Console: Verify that the command name is typed correctly and that your command is registered by checking the bepinex logs. ( you may want/need to enable the bepinex terminal, in 'YourGameFolder/BepInEx/config/bepinex.cfg' set logging to true and a terminal will pop up when you start the game every time!)
   
    DLL Not Loading: Check the plugin logs for any errors related to the DLL loading process. Ensure that the DLL is built for the correct framework version and is not corrupted. (I compile them as Class Library netstandard 2.1 and it works fine)


Enjoy building your custom commands! 😊
