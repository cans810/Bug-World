using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrierRocksController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Awake()
    {
        // Log that this barrier exists and where
        Debug.Log($"BarrierRocks initialized at position {transform.position}");
    }

    private bool isDisappearing = false;
    private float animationDuration = 2.0f; // How long the disappear animation should play before destroying

    public void RemoveBarrier()
    {
        Debug.Log("BarrierRocksController.RemoveBarrier called - triggering disappear animation");
        
        // Add null check for the animator
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("Dissappear", true);
        }
        else
        {
            Debug.LogWarning("Animator component not found on barrier!");
        }
    }

    public void PlayRockBreakSound()
    {
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound("RockBreak", transform.position);
        }
    }

    // This method will be called by PlayerInventory after the camera returns
    public void DestroyBarrier()
    {
        Debug.Log($"DestroyBarrier called on {gameObject.name} at {transform.position}");
        Destroy(gameObject);
    }
}
