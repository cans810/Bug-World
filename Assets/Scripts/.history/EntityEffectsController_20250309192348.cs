using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EntityEffectsController : MonoBehaviour
{
    public GameObject healOrDamageCanvasPrefab;
    public Color damageTextColor = Color.red;
    public Color healTextColor = Color.green;
    public float textDuration = 1.0f;
    public float textRiseSpeed = 1.0f;
    public Vector3 textOffset = new Vector3(0, 1.5f, 0);

    private LivingEntity livingEntity;

    void Start()
    {
        livingEntity = GetComponent<LivingEntity>();
        
        if (livingEntity != null)
        {
            livingEntity.OnDamaged.AddListener(ShowDamageEffect);
            livingEntity.OnHealed.AddListener(ShowHealEffect);
        }
        else
        {
            Debug.LogWarning("EntityEffectsController requires a LivingEntity component on the same GameObject.");
        }
    }

    void ShowDamageEffect()
    {
        float damageAmount = livingEntity.LastDamageAmount;
        if (damageAmount <= 0) return;

        ShowFloatingText(damageAmount.ToString("0"), damageTextColor);
    }

    void ShowHealEffect()
    {
        float healAmount = livingEntity.LastHealAmount;
        if (healAmount <= 0) return;

        ShowFloatingText("+" + healAmount.ToString("0"), healTextColor);
    }

    void ShowFloatingText(string text, Color color)
    {
        if (healOrDamageCanvasPrefab == null) return;

        GameObject textCanvas = Instantiate(healOrDamageCanvasPrefab, transform.position + textOffset, Quaternion.identity);
        
        TextMeshProUGUI textComponent = textCanvas.GetComponentInChildren<TextMeshProUGUI>();
        
        if (textComponent != null)
        {
            textComponent.text = text;
            textComponent.color = color;
            
            textCanvas.transform.forward = Camera.main.transform.forward;
            
            StartCoroutine(AnimateText(textCanvas));
        }
        else
        {
            Debug.LogWarning("TextMeshProUGUI component not found in healOrDamageCanvasPrefab.");
            Destroy(textCanvas);
        }
    }

    IEnumerator AnimateText(GameObject textObject)
    {
        float startTime = Time.time;
        Vector3 startPosition = textObject.transform.position;
        
        while (Time.time < startTime + textDuration)
        {
            float progress = (Time.time - startTime) / textDuration;
            
            textObject.transform.position = startPosition + new Vector3(0, textRiseSpeed * progress, 0);
            
            TextMeshProUGUI textComponent = textObject.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null && progress > 0.7f)
            {
                Color color = textComponent.color;
                color.a = 1 - ((progress - 0.7f) / 0.3f);
                textComponent.color = color;
            }
            
            if (Camera.main != null)
            {
                textObject.transform.forward = Camera.main.transform.forward;
            }
            
            yield return null;
        }
        
        Destroy(textObject);
    }

    void Update()
    {
        
    }
}
