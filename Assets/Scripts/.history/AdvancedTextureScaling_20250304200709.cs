using UnityEngine;

public class AdvancedTextureScaling : MonoBehaviour
{
    [SerializeField] private Material dirtMaterial;
    [SerializeField] private float baseTiling = 2.0f;
    [SerializeField] private float detailTiling = 8.0f; // For detail texture
    [SerializeField] private Texture2D detailTexture; // Smaller detail texture
    
    private void Start()
    {
        if (dirtMaterial == null || detailTexture == null)
            return;
            
        // Create a new shader property for the detail texture
        dirtMaterial.SetTexture("_DetailAlbedoMap", detailTexture);
        dirtMaterial.EnableKeyword("_DETAIL_MULX2");
        
        // Set different tiling values for main and detail texture
        dirtMaterial.mainTextureScale = new Vector2(baseTiling, baseTiling);
        
        // Set the detail texture tiling (requires modifying shader properties)
        Vector4 detailST = new Vector4(detailTiling, detailTiling, 0, 0);
        dirtMaterial.SetVector("_DetailAlbedoMap_ST", detailST);
    }
} 