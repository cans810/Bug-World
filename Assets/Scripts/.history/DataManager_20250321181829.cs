using UnityEngine;

public class DataManager : MonoBehaviour
{
    public GameData gameData;
    public PlayerAttributes playerAttributes;

    public void SaveGame()
    {
        // Existing code...
        
        // Get the speed points
        if (playerAttributes != null)
        {
            gameData.currentSpeed = playerAttributes.SpeedPoints;
        }
        
        // Rest of existing code...
    }

    public void LoadGame()
    {
        // Existing code...
        
        // Set the speed points
        if (playerAttributes != null)
        {
            // Access the SetSpeedPoints method (you might need to add this to PlayerAttributes)
            playerAttributes.SetSpeedPoints(gameData.currentSpeed);
        }
        
        // Rest of existing code...
    }
} 