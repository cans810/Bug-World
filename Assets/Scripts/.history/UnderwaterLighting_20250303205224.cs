using UnityEngine;

public class UnderwaterLighting : MonoBehaviour
{
    [Header("Lighting Settings")]
    [SerializeField] private Light mainDirectionalLight;
    [SerializeField] private Color underwaterLightColor = new Color(0.2f, 0.5f, 0.8f, 1f);
    [SerializeField] private float underwaterLightIntensity = 1.2f;
    [SerializeField] private float waterSurfaceHeight = 10f;
    
    [Header("Ambient Light Settings")]
    [SerializeField] private Color underwaterAmbientColor = new Color(0.1f, 0.3f, 0.5f, 1f);
    [SerializeField] private float underwaterAmbientIntensity = 1.5f;
    
    // Store original settings
    private Color originalLightColor;
    private float originalLightIntensity;
    private Color originalAmbientColor;
    private float originalAmbientIntensity;
    private bool isUnderwater = false;
    
    private void Start()
    {
        // Find main directional light if not assigned
        if (mainDirectionalLight == null)
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    mainDirectionalLight = light;
                    break;
                }
            }
            
            // Create directional light if none exists
            if (mainDirectionalLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                mainDirectionalLight = lightObj.AddComponent<Light>();
                mainDirectionalLight.type = LightType.Directional;
                mainDirectionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }
        
        // Store original light settings
        originalLightColor = mainDirectionalLight.color;
        originalLightIntensity = mainDirectionalLight.intensity;
        originalAmbientColor = RenderSettings.ambientLight;
        originalAmbientIntensity = RenderSettings.ambientIntensity;
        
        // Configure light for underwater scene
        ConfigureUnderwaterLighting();
    }
    
    private void Update()
    {
        // Check if camera is underwater (optional - only if you want different lighting above/below water)
        bool underwater = Camera.main.transform.position.y < waterSurfaceHeight;
        
        if (underwater != isUnderwater)
        {
            isUnderwater = underwater;
            ConfigureUnderwaterLighting();
        }
    }
    
    private void ConfigureUnderwaterLighting()
    {
        // For underwater scene, we'll set this up regardless of camera position
        
        // Configure main directional light
        mainDirectionalLight.shadows = ShadowQuality.Soft; // Or ShadowQuality.None to disable shadows completely
        mainDirectionalLight.color = underwaterLightColor;
        mainDirectionalLight.intensity = underwaterLightIntensity;
        
        // Make sure light is coming from above
        Vector3 rotation = mainDirectionalLight.transform.eulerAngles;
        rotation.x = 90f; // Point straight down for minimal shadows
        mainDirectionalLight.transform.eulerAngles = rotation;
        
        // Set ambient lighting (global illumination)
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = underwaterAmbientColor;
        RenderSettings.ambientIntensity = underwaterAmbientIntensity;
        
        // Disable shadows on water surface
        Renderer waterRenderer = GameObject.Find("WaterSurface")?.GetComponent<Renderer>();
        if (waterRenderer != null)
        {
            waterRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            waterRenderer.receiveShadows = false;
        }
    }
}