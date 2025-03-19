using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem
{
    public static void SaveGame(GameData data)
    {
        string path = Application.persistentDataPath + "/data.qnd";
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream fs = new FileStream(path, FileMode.Create);
        
        // Log save data for debugging
        Debug.Log($"Saving game data: Level={data.currentLevel}, XP={data.currentXP}, " +
                  $"Strength={data.currentStrength}, Vitality={data.currentVitality}, " +
                  $"Agility={data.currentAgility}, Incubation={data.currentIncubation}, " +
                  $"AvailablePoints={data.availableAttributePoints}");
        
        formatter.Serialize(fs, data);
        fs.Close(); 
    }

    public static GameData LoadGame()
    {
        if (!File.Exists(GetPath()))
        {
            GameData newData = new GameData();
            SaveGame(newData);
            return newData;
        }

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream fs = new FileStream(GetPath(), FileMode.Open);
        GameData loadedData = formatter.Deserialize(fs) as GameData;
        fs.Close();
        
        // Log loaded data for debugging
        Debug.Log($"Loaded game data: Level={loadedData.currentLevel}, XP={loadedData.currentXP}, " +
                  $"Strength={loadedData.currentStrength}, Vitality={loadedData.currentVitality}, " +
                  $"Agility={loadedData.currentAgility}, Incubation={loadedData.currentIncubation}, " +
                  $"AvailablePoints={loadedData.availableAttributePoints}");
        
        return loadedData;
    }

    private static string GetPath()
    {
        return Application.persistentDataPath + "/data.qnd";
    }
    
    // Add a method to delete save data (for testing)
    public static void DeleteSaveData()
    {
        string path = GetPath();
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Save data deleted successfully");
        }
        else
        {
            Debug.Log("No save data found to delete");
        }
    }
}
