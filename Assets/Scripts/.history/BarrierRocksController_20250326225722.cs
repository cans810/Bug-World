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
        Debug.Log("Starting barrier disappear animation");
        
        // Just play the animation, don't destroy anything
        Animator anim = GetComponent<Animator>();
        if (anim != null)
            anim.SetBool("Dissappear", true);
    }

    public void PlayRockBreakSound()
    {
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound("RockBreak", transform.position);
        }
    }

    public void PlayLevel31Sound()
    {
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlaySound("Level31", transform.position);
        }
    }

    // This is called explicitly after camera returns
    public void DestroyBarrier()
    {
        Debug.Log("Explicitly destroying barrier object");
        Destroy(gameObject);
    }
}
