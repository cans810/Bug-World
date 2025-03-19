using UnityEngine;
using System.Collections;

public class LootOutlineBlocker : MonoBehaviour
{
    private Renderer[] renderers;
    private Material[][] originalMaterials;
    
    public void Initialize()
    {
        // Cache all renderers
        renderers = GetComponentsInChildren<Renderer>(true);
        originalMaterials = new Material[renderers.Length][];
        
        // Store original materials
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterials;
        }
        
        // Start monitoring for outline materials
        StartCoroutine(MonitorForOutlineMaterials());
    }
    
    private IEnumerator MonitorForOutlineMaterials()
    {
        WaitForSeconds wait = new WaitForSeconds(0.05f); // Check frequently but not every frame
        
        while (true)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                
                Material[] currentMaterials = renderers[i].sharedMaterials;
                bool hasOutlineMaterial = false;
                
                // Check if any materials are outline materials
                foreach (Material mat in currentMaterials)
                {
                    if (mat != null && mat.name.Contains("Outline"))
                    {
                        hasOutlineMaterial = true;
                        break;
                    }
                }
                
                // If we found outline materials, restore original materials
                if (hasOutlineMaterial && originalMaterials[i] != null)
                {
                    renderers[i].sharedMaterials = originalMaterials[i];
                }
            }
            
            // Also check for and disable any Outline component
            Outline outline = GetComponent<Outline>();
            if (outline != null && outline.enabled)
            {
                outline.enabled = false;
            }
            
            yield return wait;
        }
    }
    
    private void OnDestroy()
    {
        // Clean up
        StopAllCoroutines();
    }
} 