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
        return loadedData;
    }

    
    private static string GetPath()
    {
        return Application.persistentDataPath + "/data.qnd";
    }
}
