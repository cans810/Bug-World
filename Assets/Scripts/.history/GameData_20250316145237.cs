using System;
using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    // Player level and experience
    public int currentLevel = 1;
    public int currentXP = 0;
    
    // Resources
    public int currentChitin = 0;
    public int currentCrumb = 0;
    public int currentEgg = 0;
    
    // Health
    public int currentHP = 100;
    public int currentMaxHP = 100;
    
    // Attributes
    public int currentStrength = 0;
    public int currentVitality = 0;
    public int currentAgility = 0;
    public int currentIncubation = 0;
    public int availableAttributePoints = 0;
    
    // Flags
    public bool hasYetToCollectChitin = true;
    public bool hasYetToCollectCrumb = true;
    
    // Discovered entities
    public bool hasDiscoveredAnt = false;
    public bool hasDiscoveredFly = false;
    public bool hasDiscoveredLadybug = false;
    
    // Egg capacity
    public int maxEggCapacity = 1; // Default value
    
    // Constructor
    public GameData()
    {
        // Initialize with default values
    }
} 