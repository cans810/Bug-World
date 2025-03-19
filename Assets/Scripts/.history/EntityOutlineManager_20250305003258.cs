using System.Collections.Generic;
using UnityEngine;

public class EntityOutlineManager : MonoBehaviour
{
    [Header("Global Settings")]
    [SerializeField] private Color defaultOutlineColor = Color.red;
    [SerializeField] private float defaultOutlineWidth = 0.005f;
    [SerializeField] private bool enableOutlinesOnStart = true;
    
    [Header("Entity Tags")]
    [SerializeField] private string[] entityTags = { "Player", "Enemy", "NPC" };
    
    private List<EntityOutline> trackedEntities = new List<EntityOutline>();
    
    private void Start()
    {
        // Find all entities with specified tags and add outlines
        FindAndSetupEntities();
        
        // Enable outlines if specified
        if (enableOutlinesOnStart)
        {
            EnableAllOutlines();
        }
    }
    
    private void FindAndSetupEntities()
    {
        foreach (string tag in entityTags)
        {
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
            
            foreach (GameObject obj in taggedObjects)
            {
                // Add outline component if it doesn't exist
                EntityOutline outline = obj.GetComponent<EntityOutline>();
                if (outline == null)
                {
                    outline = obj.AddComponent<EntityOutline>();
                }
                
                // Configure outline
                outline.SetOutlineColor(defaultOutlineColor);
                outline.SetOutlineWidth(defaultOutlineWidth);
                
                // Track the entity
                trackedEntities.Add(outline);
            }
        }
    }
    
    public void EnableAllOutlines()
    {
        foreach (EntityOutline outline in trackedEntities)
        {
            outline.SetOutlineEnabled(true);
        }
    }
    
    public void DisableAllOutlines()
    {
        foreach (EntityOutline outline in trackedEntities)
        {
            outline.SetOutlineEnabled(false);
        }
    }
    
    public void ToggleOutlines()
    {
        // Check first entity's state to determine toggle direction
        if (trackedEntities.Count > 0)
        {
            bool firstIsEnabled = trackedEntities[0].GetComponent<Renderer>().material.shader.name.Contains("Outline");
            
            foreach (EntityOutline outline in trackedEntities)
            {
                outline.SetOutlineEnabled(!firstIsEnabled);
            }
        }
    }
    
    // Public method to set specific entity's outline
    public void SetEntityOutline(GameObject entity, bool enabled)
    {
        EntityOutline outline = entity.GetComponent<EntityOutline>();
        if (outline != null)
        {
            outline.SetOutlineEnabled(enabled);
        }
    }
} 