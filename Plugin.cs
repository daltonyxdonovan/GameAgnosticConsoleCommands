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
        private Dictionary<string, Action> commands = new();
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

            var commandTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces().Contains(typeof(ICommand)) && t.IsClass);

            foreach (var type in commandTypes)
            {
                try
                {
                    var constructor = type.GetConstructor(new[] { typeof(ManualLogSource) });
                    if (constructor != null)
                    {
                        var command = (ICommand)constructor.Invoke(new object[] { Plugin.Logger });

                        commands[command.Name] = command.Execute;
                    }
                    else
                    {
                        if (Activator.CreateInstance(type) is ICommand command)
                        {
                            commands[command.Name] = command.Execute;
                        }
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
            if (ticker > 0) ticker--;

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

        private void ExecuteCommand(string command)
        {
            if (string.IsNullOrEmpty(command)) return;

            if (commands.ContainsKey(command))
            {
                try
                {
                    var commandToExecute = commands[command];
                    commandToExecute?.Invoke();
                    Log($"Command '{command}' has been executed!");
                }
                catch (Exception ex)
                {
                    Log($"Error executing command '{command}': {ex.Message}");
                }
            }
            else
            {
                Logger.LogWarning($"Unknown command: {command}");
                Log($"Unknown command: {command}");
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
            var canvas = FindObjectOfType<Canvas>();
            
            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("GACCCanvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            myPanel = new GameObject("GACCPanel");
            myPanel.transform.SetParent(canvas.transform, false);
            background = myPanel.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(0, 0, 0, 1); // Black background

            RectTransform rectTransform = myPanel.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0f); // Anchored to the bottom-left corner
            rectTransform.anchorMax = new Vector2(1f, 0.03f);
            rectTransform.sizeDelta = new Vector2(0, 0);
            rectTransform.anchoredPosition = new Vector2(0, 0);

            // Create and set up the text input display (input area)
            textInputArea = new GameObject("GACCTextInputArea");
            textInputArea.transform.SetParent(canvas.transform, false);
            textInputDisplay = textInputArea.AddComponent<TextMeshProUGUI>();
            textInputDisplay.color = Color.white;
            textInputDisplay.fontSize = 24;
            textInputDisplay.alignment = TextAlignmentOptions.MidlineLeft;

            RectTransform inputRectTransform = textInputDisplay.GetComponent<RectTransform>();
            inputRectTransform.anchorMin = new Vector2(0f, 0f); // Anchored to the bottom-left corner
            inputRectTransform.anchorMax = new Vector2(1f, .025f); // Full width
            inputRectTransform.sizeDelta = new Vector2(0f, 0f);
            inputRectTransform.anchoredPosition = new Vector2(20, 0);

            myPanel.SetActive(false);
        }
    }
}
