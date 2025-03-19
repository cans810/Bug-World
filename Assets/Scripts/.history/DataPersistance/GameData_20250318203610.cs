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
    public int currentCoin;
    public int currentEgg;
    public int maxEggCapacity;
    public int currentHP;
    public int currentMaxHP;
    public int currentStrength;
    public int currentVitality;
    public int currentAgility;
    public int currentIncubation;
    public int availableAttributePoints;
    public bool hasYetToCollectChitin;
    public bool hasYetToCollectCrumb;
    public bool hasDiscoveredAnt;
    public bool hasDiscoveredFly;
    public bool hasDiscoveredLadybug;
    public bool hasDiscoveredFlea;

    public GameData()
    {
        this.currentLevel = 1;
        this.currentXP = 0;
        this.currentChitin = 0;
        this.currentCrumb = 0;
        this.currentCoin = 0;
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
        this.hasDiscoveredFlea = false;
        this.maxEggCapacity = 1;
    }
}
