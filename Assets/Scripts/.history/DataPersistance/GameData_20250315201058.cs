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

    public GameData()
    {
        this.currentLevel = 1;
        this.currentXP = 0;
        this.currentChitin = 0;
        this.currentCrumb = 0;
        this.currentEgg = 0;
        this.currentHP = 0;
        this.currentMaxHP = 0;
        this.currentStrength = 0;
        this.currentVitality = 0;
        this.currentAgility = 0;
        this.currentIncubation = 0;
    }

}
