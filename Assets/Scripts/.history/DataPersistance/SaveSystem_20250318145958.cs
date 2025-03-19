using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem
{
    public static void SaveGame(GameData data)
    {
        try
        {
            string path = Application.persistentDataPath + "/data.qnd";
            BinaryFormatter formatter = new BinaryFormatter();
            
            // Create directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            
            using (FileStream fs = new FileStream(path, FileMode.Create))
            {
                formatter.Serialize(fs, data);
            }
            
            Debug.Log($"Game saved successfully to: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving game: {e.Message}\n{e.StackTrace}");
        }
    }

    public static GameData LoadGame()
    {
        string path = GetPath();
        
        try
        {
            if (!File.Exists(path))
            {
                Debug.Log("No save file found, creating new game data");
                GameData newData = new GameData();
                SaveGame(newData);
                return newData;
            }

            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                GameData loadedData = formatter.Deserialize(fs) as GameData;
                
                if (loadedData == null)
                {
                    Debug.LogError("Loaded data is null, creating new game data");
                    return new GameData();
                }
                
                // Log all contents of the loaded save file for debugging
                Debug.Log("=== LOADED SAVE FILE CONTENTS ===");
                Debug.Log($"Player Level: {loadedData.currentLevel}");
                Debug.Log($"Experience: {loadedData.currentXP}");
                Debug.Log($"Health: {loadedData.currentHP}/{loadedData.currentMaxHP}");
                Debug.Log($"Resources - Chitin: {loadedData.currentChitin}, Crumb: {loadedData.currentCrumb}, Coins: {loadedData.currentCoin}");
                Debug.Log($"Attributes - Strength: {loadedData.currentStrength}, Vitality: {loadedData.currentVitality}, Agility: {loadedData.currentAgility}");
                Debug.Log($"Incubation: {loadedData.currentIncubation}, Available Points: {loadedData.availableAttributePoints}");
                Debug.Log($"Eggs: {loadedData.currentEgg}/{loadedData.maxEggCapacity}");
                Debug.Log($"Collection Flags - HasYetToCollectChitin: {loadedData.hasYetToCollectChitin}, HasYetToCollectCrumb: {loadedData.hasYetToCollectCrumb}");
                Debug.Log($"Discovered Entities - Ant: {loadedData.hasDiscoveredAnt}, Fly: {loadedData.hasDiscoveredFly}, Ladybug: {loadedData.hasDiscoveredLadybug}");
                Debug.Log("===============================");
                
                Debug.Log($"Game loaded successfully from: {path}");
                return loadedData;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading game: {e.Message}\n{e.StackTrace}");
            return new GameData();
        }
    }

    
    private static string GetPath()
    {
        return Application.persistentDataPath + "/data.qnd";
    }
}
