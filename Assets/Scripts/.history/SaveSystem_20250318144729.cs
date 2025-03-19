using UnityEngine;
using System.IO;

public class SaveSystem : MonoBehaviour
{
    public static void DeleteSaveFile()
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("Save file deleted");
        }
        else
        {
            Debug.Log("No save file found to delete");
        }
    }
} 