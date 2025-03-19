using UnityEngine;

[System.Serializable]
public class EggData
{
    public string entityType;
    public float remainingTime;
    public Vector3 position;
    
    // Constructor for easy creation
    public EggData(string entityType, float remainingTime, Vector3 position)
    {
        this.entityType = entityType;
        this.remainingTime = remainingTime;
        this.position = position;
    }
} 