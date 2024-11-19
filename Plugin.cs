using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GACC
{
[BepInPlugin("org.daltonyx.plugins.GACC", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    private readonly Dictionary<string, Action<string[]>> commands = new();

    private GameObject myPanel;
    private Image background;
    private bool consoleActive = false;
    private string input = "";
    private int ticker = 0;
    private GameObject textInputArea;
    private TextMeshProUGUI textInputDisplay;

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo("Ready to load custom commands!");

        LoadAndRegisterCommands();
        RegisterConsoleCommands();
    }

    public void Log(string message)
    {
        Logger.LogInfo(message);
    }

    private void LoadAndRegisterCommands()
    {
        string commandsPath = Path.Combine(Paths.ConfigPath, "CommandScripts");
        Directory.CreateDirectory(commandsPath);

        var commandDlls = Directory.GetFiles(commandsPath, "*.dll");

        if (commandDlls.Length == 0)
        {
            Logger.LogWarning("No command DLLs found! Place .dll files in the BepInEx/config/CommandScripts folder.");
            return;
        }

        foreach (var dll in commandDlls)
        {
            Logger.LogInfo($"Loading command DLL: {Path.GetFileName(dll)}");

            try
            {
                if (File.Exists(dll))
                {
                    LoadAndRegisterCommandDll(dll);
                }
                else
                {
                    Logger.LogError($"DLL not found: {dll}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to load command DLL {dll}: {ex.Message}");
            }
        }
    }

    private void LoadAndRegisterCommandDll(string dllPath)
    {
        Assembly assembly = Assembly.LoadFrom(dllPath);
        Logger.LogInfo($"Assembly {Path.GetFileName(dllPath)} loaded successfully.");

        var commandTypes = assembly.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(ICommand)) && t.IsClass);

        foreach (var type in commandTypes)
        {
            try
            {
                // Try to find a constructor that accepts a ManualLogSource
                var constructor = type.GetConstructor(new[] { typeof(ManualLogSource) });
                ICommand command = null;

                if (constructor != null)
                {
                    command = (ICommand)constructor.Invoke(new object[] { Plugin.Logger });
                }
                else
                {
                    command = Activator.CreateInstance(type) as ICommand;
                }

                if (command != null)
                {
                    // Use a delegate compatible with Execute(params string[] args)
                    commands[command.Name] = args => command.Execute(args);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error registering command from type {type.Name}: {ex.Message}");
            }
        }
    }

    private void RegisterConsoleCommands()
    {
        foreach (var command in commands)
        {
            Logger.LogInfo($"Registered command: {command.Key}");
        }
    }

    private void Update()
    {
        if (ticker > 0)
            ticker--;

        if (Input.GetKeyDown(KeyCode.Slash))
        {
            ToggleConsole();
        }

        if (consoleActive)
        {
            if (textInputDisplay)
                textInputDisplay.gameObject.SetActive(true);
            HandleTextInput();

            if (Input.GetKeyDown(KeyCode.Return) && ticker == 0)
            {
                ExecuteCommand(input.Trim());
                input = "";
                ticker = 10;
                consoleActive = false;
            }
        }

        else
        {
            if (textInputDisplay)
                textInputDisplay.gameObject.SetActive(false);
        }
    }

    private void ToggleConsole()
    {
        if (myPanel == null)
        {
            Logger.LogInfo("Recreating UI elements for the console.");
            SetupVisualElements();
        }

        consoleActive = !consoleActive;
        myPanel.SetActive(consoleActive);
        if (consoleActive)
        {
            textInputDisplay.text = input;
        }
    }

    private void ExecuteCommand(string input)
    {
        if (string.IsNullOrEmpty(input))
            return;

        // Split input into command name and arguments
        var parts = input.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

        string commandName = parts[0];
        string[] args = parts.Length > 1 ? parts[1].Split(' ') : new string[0];

        if (commands.ContainsKey(commandName))
        {
            try
            {
                var commandToExecute = commands[commandName];
                commandToExecute?.Invoke(args);
                Log($"Command '{commandName}' has been executed with arguments: {string.Join(", ", args)}");
            }
            catch (Exception ex)
            {
                Log($"Error executing command '{commandName}': {ex.Message}");
            }
        }
        else
        {
            Logger.LogWarning($"Unknown command: {commandName}");
            Log($"Unknown command: {commandName}");
        }
    }

    private void HandleTextInput()
    {
        if (!textInputDisplay)
            return;

        foreach (char c in Input.inputString)
        {
            if (c == '\b')
            {
                if (input.Length > 0)
                {
                    input = input.Substring(0, input.Length - 1);
                }
            }
            else if (c == '\n' || c == '\r')
            {
                // Do nothing on enter here, handled in ExecuteCommand
            }
            else
            {
                input += c;
            }
        }

        textInputDisplay.text = input;
    }

    private void SetupVisualElements()
    {
        // what an ABSOLUTE NIGHTMARE setting up TextMeshProUGUI objects is in raw code
        // like, why do we _require_ things to be set? shouldn't they have a default, UNITY!?

        var canvas = GameObject.Find("Canvas~").GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        myPanel = new GameObject("ConsolePanel");
        myPanel.transform.SetParent(canvas.transform, false);

        background = myPanel.AddComponent<UnityEngine.UI.Image>();
        background.color = new Color(0, 0, 0, 1); //  black background

        RectTransform rectTransform = myPanel.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0f, 0f); // Anchored to the bottom-left corner
        rectTransform.anchorMax = new Vector2(1f, 0.03f);
        rectTransform.sizeDelta = new Vector2(0, 0);
        rectTransform.anchoredPosition = new Vector2(0, 0);

        // Create and set up the text input display (input area)
        textInputArea = new GameObject("TextInputArea");
        textInputArea.transform.SetParent(canvas.transform, false);
        textInputDisplay = textInputArea.AddComponent<TextMeshProUGUI>();
        textInputDisplay.color = Color.white;
        textInputDisplay.fontSize = 24;
        textInputDisplay.alignment = TextAlignmentOptions.MidlineLeft;

        RectTransform inputRectTransform = textInputDisplay.GetComponent<RectTransform>();
        inputRectTransform.anchorMin = new Vector2(0f, 0f);    // Anchored to the bottom-left corner
        inputRectTransform.anchorMax = new Vector2(1f, .025f); // Full width
        inputRectTransform.sizeDelta = new Vector2(0f, 0f);
        inputRectTransform.anchoredPosition = new Vector2(20, 0);

        myPanel.SetActive(false);
    }
}
}
