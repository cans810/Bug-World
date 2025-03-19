using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    public int currentLevel;
    public int currentXP;
    public int currentChitin;
    public int currentCrumb;
    public int currentEgg;
    public int currentHP;
    public int currentMaxHP;
    public int currentStrength;
    public int currentVitality;
    public int currentAgility;
    public int currentIncubation;
    public int availableAttributePoints;
    public bool hasYetToCollectChitin = true;
    public bool hasYetToCollectCrumb = true;
    public bool hasDiscoveredAnt = false;
    public bool hasDiscoveredFly = false;
    public bool hasDiscoveredLadybug = false;
    public int maxEggCapacity;
    public int currentCoin;
    
    // Replace Dictionary with serializable lists
    [System.Serializable]
    public class EntityPurchaseData
    {
        public string entityType;
        public bool isPurchased;
        
        public EntityPurchaseData(string type, bool purchased)
        {
            entityType = type;
            isPurchased = purchased;
        }
    }
    
    public List<EntityPurchaseData> entityPurchaseList = new List<EntityPurchaseData>();
    public string equippedEntity = "";

    public GameData()
    {
        this.currentLevel = 1;
        this.currentXP = 0;
        this.currentChitin = 0;
        this.currentCrumb = 0;
        this.currentEgg = 0;
        this.currentHP = 100;
        this.currentMaxHP = 100;
        this.currentStrength = 0;
        this.currentVitality = 0;
        this.currentAgility = 0;
        this.currentIncubation = 0;
        this.availableAttributePoints = 0;
        this.hasYetToCollectChitin = true;
        this.hasYetToCollectCrumb = true;
        this.hasDiscoveredAnt = false;
        this.hasDiscoveredFly = false;
        this.hasDiscoveredLadybug = false;
        this.maxEggCapacity = 1;
        this.currentCoin = 0;
        this.entityPurchaseList = new List<EntityPurchaseData>();
        this.equippedEntity = "";
    }
    
    // Helper methods to work with entity purchase data
    public void SetEntityPurchased(string entityType, bool isPurchased)
    {
        // Remove existing entry if any
        entityPurchaseList.RemoveAll(x => x.entityType == entityType);
        
        // Add new entry
        entityPurchaseList.Add(new EntityPurchaseData(entityType, isPurchased));
    }
    
    public bool IsEntityPurchased(string entityType)
    {
        foreach (var data in entityPurchaseList)
        {
            if (data.entityType == entityType)
                return data.isPurchased;
        }
        return false;
    }
}
