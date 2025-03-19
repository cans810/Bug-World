using UnityEngine;
using TMPro;

public class AttributeDisplay : MonoBehaviour
{
    [SerializeField] private PlayerAttributes playerAttributes;
    [SerializeField] private TextMeshProUGUI strengthText;
    [SerializeField] private TextMeshProUGUI vitalityText;
    
    private void Start()
    {
        if (playerAttributes == null)
            playerAttributes = FindObjectOfType<PlayerAttributes>();
            
        // Subscribe to attribute changes
        if (playerAttributes != null)
            playerAttributes.OnAttributesChanged += UpdateDisplay;
            
        // Initial update
        UpdateDisplay();
    }
    
    private void UpdateDisplay()
    {
        if (playerAttributes != null)
        {
            if (strengthText != null)
                strengthText.text = $"Strength: {playerAttributes.GetStrengthValue():F0}";
                
            if (vitalityText != null)
                vitalityText.text = $"Vitality: {playerAttributes.GetVitalityValue():F0}";
        }
    }
    
    private void OnDestroy()
    {
        if (playerAttributes != null)
            playerAttributes.OnAttributesChanged -= UpdateDisplay;
    }
} 