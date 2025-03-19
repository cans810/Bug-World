using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class UnderwaterEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float waterSurfaceHeight = 10f;
    
    [Header("Underwater Post Processing")]
    [SerializeField] private PostProcessVolume postProcessVolume;
    [SerializeField] private PostProcessProfile underwaterProfile;
    [SerializeField] private PostProcessProfile normalProfile;
    
    [Header("Underwater Colors")]
    [SerializeField] private Color underwaterTint = new Color(0.2f, 0.5f, 0.8f, 1f);
    [SerializeField] private float underwaterSaturation = 0.7f;
    [SerializeField] private float underwaterContrast = 0.9f;
    [SerializeField] private float underwaterTemperature = 20f; // Blue tint
    
    [Header("Underwater Fog")]
    [SerializeField] private bool enableFog = true;
    [SerializeField] private Color underwaterFogColor = new Color(0.15f, 0.35f, 0.6f, 1f);
    [SerializeField] private float underwaterFogDensity = 0.1f;
    
    // Store original settings to restore when exiting water
    private Color originalFogColor;
    private float originalFogDensity;
    private bool originalFogEnabled;
    private bool isUnderwater = false;
    
    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        if (postProcessVolume == null)
        {
            // Try to find post process volume in scene
            postProcessVolume = FindObjectOfType<PostProcessVolume>();
            
            // If none exists, create one
            if (postProcessVolume == null)
            {
                GameObject volumeObject = new GameObject("Post Process Volume");
                postProcessVolume = volumeObject.AddComponent<PostProcessVolume>();
                postProcessVolume.isGlobal = true;
                postProcessVolume.priority = 1;
            }
        }
        
        // Store original fog settings
        originalFogColor = RenderSettings.fogColor;
        originalFogDensity = RenderSettings.fogDensity;
        originalFogEnabled = RenderSettings.fog;
        
        // Create underwater profile if not assigned
        if (underwaterProfile == null)
            CreateUnderwaterProfile();
            
        // Store normal profile if not assigned
        if (normalProfile == null && postProcessVolume.profile != null)
            normalProfile = postProcessVolume.profile;
    }
    
    private void Update()
    {
        // Check if camera is underwater
        bool underwater = mainCamera.transform.position.y < waterSurfaceHeight;
        
        // Apply or remove underwater effects when state changes
        if (underwater != isUnderwater)
        {
            isUnderwater = underwater;
            ApplyUnderwaterEffects(underwater);
        }
    }
    
    private void ApplyUnderwaterEffects(bool underwater)
    {
        // Apply post processing
        postProcessVolume.profile = underwater ? underwaterProfile : normalProfile;
        
        // Apply fog settings
        if (enableFog)
        {
            if (underwater)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = underwaterFogColor;
                RenderSettings.fogDensity = underwaterFogDensity;
            }
            else
            {
                RenderSettings.fog = originalFogEnabled;
                RenderSettings.fogColor = originalFogColor;
                RenderSettings.fogDensity = originalFogDensity;
            }
        }
    }
    
    private void CreateUnderwaterProfile()
    {
        // Create a new profile
        underwaterProfile = ScriptableObject.CreateInstance<PostProcessProfile>();
        
        // Add color grading effect
        ColorGrading colorGrading = underwaterProfile.AddSettings<ColorGrading>();
        colorGrading.enabled.Override(true);
        
        // Set blue tint
        colorGrading.temperature.Override(underwaterTemperature);
        colorGrading.colorFilter.Override(underwaterTint);
        
        // Reduce saturation
        colorGrading.saturation.Override(underwaterSaturation * 100 - 100); // Convert to -100 to 100 range
        
        // Adjust contrast
        colorGrading.contrast.Override(underwaterContrast * 100 - 100); // Convert to -100 to 100 range
        
        // Add vignette for edge darkening
        Vignette vignette = underwaterProfile.AddSettings<Vignette>();
        vignette.enabled.Override(true);
        vignette.intensity.Override(0.3f);
        vignette.color.Override(new Color(0.1f, 0.3f, 0.5f));
        
        // Add slight blur
        DepthOfField depthOfField = underwaterProfile.AddSettings<DepthOfField>();
        depthOfField.enabled.Override(true);
        depthOfField.focusDistance.Override(10f);
        depthOfField.aperture.Override(5.6f);
        
        // Add chromatic aberration for water distortion
        ChromaticAberration chromaticAberration = underwaterProfile.AddSettings<ChromaticAberration>();
        chromaticAberration.enabled.Override(true);
        chromaticAberration.intensity.Override(0.1f);
    }
}