using UnityEngine;

public class DoubleSidedTexturedPlane : MonoBehaviour
{
    public Texture2D myTexture;
    [Range(0.01f, 10f)]
    public float width = 1f;
    [Range(0.01f, 10f)]
    public float height = 1f;
    [Range(0.001f, 0.1f)]
    public float spacing = 0.01f;
    
    void Start()
    {
        CreateDoubleSidedPlane();
    }
    
    public void CreateDoubleSidedPlane()
    {
        // Create parent object to hold both planes
        GameObject planeHolder = new GameObject("TexturedPlaneHolder");
        planeHolder.transform.SetParent(this.transform);
        planeHolder.transform.localPosition = Vector3.zero;
        
        // Create top plane
        GameObject topPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        topPlane.transform.SetParent(planeHolder.transform);
        topPlane.transform.localPosition = new Vector3(0, spacing/2, 0);
        topPlane.transform.localScale = new Vector3(width/10, 1, height/10); // Plane is 10x10 units by default
        topPlane.transform.localRotation = Quaternion.identity;
        topPlane.name = "TopPlane";
        
        // Create bottom plane (flipped)
        GameObject bottomPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        bottomPlane.transform.SetParent(planeHolder.transform);
        bottomPlane.transform.localPosition = new Vector3(0, -spacing/2, 0);
        bottomPlane.transform.localScale = new Vector3(width/10, 1, height/10);
        bottomPlane.transform.localRotation = Quaternion.Euler(180, 0, 0); // Flip it
        bottomPlane.name = "BottomPlane";
        
        // Create and apply material with texture
        Material textureMaterial = new Material(Shader.Find("Standard"));
        textureMaterial.mainTexture = myTexture;
        
        // Apply material to both planes
        topPlane.GetComponent<Renderer>().material = textureMaterial;
        bottomPlane.GetComponent<Renderer>().material = textureMaterial;
    }
} 