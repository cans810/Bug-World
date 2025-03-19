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


    
    private static string GetPath()
    {
        return Application.persistentDataPath + "/data.qnd";
    }
}
