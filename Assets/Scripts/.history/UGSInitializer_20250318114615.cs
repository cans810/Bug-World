using UnityEngine;
using Unity.Services.Core;
using System.Threading.Tasks;

public class UGSInitializer : MonoBehaviour
{
    private static bool isInitialized = false;

    private async void Start()
    {
        if (!isInitialized)
        {
            try
            {
                await UnityServices.InitializeAsync();
                Debug.Log("Unity Gaming Services initialized successfully");
                isInitialized = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to initialize Unity Gaming Services: {e.Message}");
            }
        }
    }
} 