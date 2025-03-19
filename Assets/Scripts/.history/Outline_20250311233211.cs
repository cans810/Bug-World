using UnityEngine;
using System.Collections.Generic;

public class Outline : MonoBehaviour
{
    private List<Renderer> ignoredRenderers = new List<Renderer>();

    public void AddIgnoredRenderer(Renderer renderer)
    {
        if (!ignoredRenderers.Contains(renderer))
        {
            ignoredRenderers.Add(renderer);
        }
    }

    public void ClearIgnoredRenderers()
    {
        ignoredRenderers.Clear();
    }

    private void CreateMaterialsForRenderers()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            if (ignoredRenderers.Contains(renderer))
                continue;
            
            // Rest of your existing code to create materials...
        }
    }
} 