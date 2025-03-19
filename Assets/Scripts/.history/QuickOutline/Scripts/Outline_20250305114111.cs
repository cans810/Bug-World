using UnityEngine;

public class Outline : MonoBehaviour {
    // Around line 214 in LoadSmoothNormals method
    try {
        // Clear UV3
        skinnedMeshRenderer.sharedMesh.uv4 = new Vector2[skinnedMeshRenderer.sharedMesh.vertexCount];
        
        // Combine submeshes
        CombineSubmeshes(skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer.sharedMaterials);
    }
    catch (System.Exception e) {
        Debug.LogWarning("Could not set UV4 on mesh: " + e.Message + 
                        "\nThe outline may not appear correctly. Try enabling Read/Write on the mesh import settings.");
    }
} 