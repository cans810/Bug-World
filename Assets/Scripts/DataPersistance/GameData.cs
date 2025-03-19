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

    public bool hasDiscoveredFlea;
    public bool hasDiscoveredLarvae;
    public bool hasDiscoveredAnt;
    public bool hasDiscoveredFly;
    public bool hasDiscoveredLadybug;
    public bool hasDiscoveredMosquito;
    public bool hasDiscoveredGrasshopper;
    public bool hasDiscoveredWasp;
    public bool hasDiscoveredWolfSpider;
    public bool hasDiscoveredBeetle;
    public bool hasDiscoveredStickInsect;
    public bool hasDiscoveredCentipede;
    public bool hasDiscoveredMantis;
    public bool hasDiscoveredTarantula;
    public bool hasDiscoveredStagBeetle;
    public bool hasDiscoveredScorpion;


    public int maxChitinCapacity;
    public int maxCrumbCapacity;

    public string equippedEntityType = "";
    public int collectedCrumbs = 0;

    public List<string> purchasedEntities = new List<string>();

    public List<EggData> activeEggs = new List<EggData>();

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
        this.hasDiscoveredLarvae = false;
        this.hasDiscoveredMosquito = false;
        this.hasDiscoveredGrasshopper = false;
        this.hasDiscoveredWasp = false;
        this.hasDiscoveredWolfSpider = false;
        this.hasDiscoveredBeetle = false;
        this.hasDiscoveredStickInsect = false;
        this.hasDiscoveredCentipede = false;
        this.hasDiscoveredMantis = false;
        this.hasDiscoveredTarantula = false;
        this.hasDiscoveredStagBeetle = false;
        this.hasDiscoveredScorpion = false;
        this.maxEggCapacity = 1;
        this.maxChitinCapacity = 50;
        this.maxCrumbCapacity = 5;

        // Initialize purchased entities list
        purchasedEntities = new List<string>();
        
        // Add any default purchased entities (if applicable)

        // Initialize egg list
        activeEggs = new List<EggData>();
    }
}
