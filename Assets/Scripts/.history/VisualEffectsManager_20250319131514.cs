using UnityEngine;

public class VisualEffectsManager : MonoBehaviour
{
    public GameObject xpPrefab;

    public void SpawnXPSymbol(Vector3 position)
    {
        // Create the XP symbol
        if (xpPrefab != null)
        {
            GameObject xpInstance = Instantiate(xpPrefab, position, Quaternion.identity);
            
            // Set up animation - will move toward the XP counter
            XPSymbolAnimation xpAnim = xpInstance.GetComponent<XPSymbolAnimation>();
            if (xpAnim != null)
            {
                // Find the XP text in the UI
                GameObject xpText = GameObject.FindGameObjectWithTag("XPText");
                if (xpText != null)
                {
                    xpAnim.SetTargetPosition(xpText.transform.position);
                }
                else
                {
                    // Fallback to canvas center if no XP text is found
                    Canvas mainCanvas = FindObjectOfType<Canvas>();
                    if (mainCanvas != null)
                    {
                        xpAnim.SetTargetPosition(mainCanvas.transform.position);
                    }
                }
            }
            else
            {
                // If there's no XP animation component, destroy after a delay
                Destroy(xpInstance, 3f);
            }
        }
    }
} 