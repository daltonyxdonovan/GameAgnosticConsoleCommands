using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PullTrash : ICommand
{
    private readonly ManualLogSource _logger;

    public PullTrash(ManualLogSource logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public string Name => "/grab";

    public void Execute()
    {
        try
        {
            GameObject trashManager = GameObject.Find("TrashSpawner");
            if (trashManager == null)
            {
                _logger.LogError("TrashSpawner not found in the scene.");
                return;
            }

            string[] allowedTrash = { "Planks", "FishRed", "Crate", "PalmLeaf", "Fish" };
            List<Trash> trashList = new List<Trash>();

            foreach (Transform child in trashManager.transform)
            {
                if (allowedTrash.Any(name => child.name.StartsWith(name)))
                {
                    Trash trash = child.GetComponent<Trash>();
                    if (trash != null)
                    {
                        trashList.Add(trash);
                    }
                    else
                    {
                        _logger.LogWarning($"Trash component missing on child {child.name}.");
                    }
                }
            }

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                _logger.LogError("Player not found in the scene.");
                return;
            }

            UnityEngine.Vector3 playerPos = player.transform.position;

            if (trashList.Count == 0)
            {
                _logger.LogInfo("No valid trash found to pull.");
            }

            foreach (Trash t in trashList)
            {
                t.transform.position = playerPos;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"An error occurred while executing the /grab command: {ex.Message}");
        }
    }
}
