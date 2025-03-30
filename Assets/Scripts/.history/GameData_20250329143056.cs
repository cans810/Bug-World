using System.Collections.Generic;

public class GameData
{
    // Add to GameData class to track unlocked borders/areas
    public List<string> unlockedBorderAreas = new List<string>();

    // In constructor, initialize the list
    public GameData()
    {
        // ... existing code ...
        unlockedBorderAreas = new List<string>();
    }
} 