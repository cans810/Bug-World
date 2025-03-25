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
    private float animationDuration = 3.0f; // How long the disappear animation should play before destroying

    public void RemoveBarrier()
    {
        Debug.Log("BarrierRocksController.RemoveBarrier called - triggering disappear animation");
        isDisappearing = true;
        GetComponent<Animator>().SetBool("Dissappear", true);
        
        // Use a coroutine to delay the destruction
        StartCoroutine(DelayedDestroy());
    }

    private IEnumerator DelayedDestroy()
    {
        // Wait for the animation to play
        yield return new WaitForSeconds(animationDuration);
        
        // Now destroy the barrier
        DestroyBarrier();
    }

    public void DestroyBarrier()
    {
        Debug.Log($"DestroyBarrier called on {gameObject.name} at {transform.position}");
        Destroy(gameObject);
    }

    public void PlayRockBreakSound(){
        SoundEffectManager.Instance.PlaySound("RockBreak", gameObject.transform.position);
    }
}
