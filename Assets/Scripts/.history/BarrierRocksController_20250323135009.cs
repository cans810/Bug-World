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
        GetComponent<Animator>().SetBool("Dissappear", true);
        
        // No auto-destruction - we'll let PlayerInventory handle this after camera returns
    }

    public void PlayRockBreakSound(){
        SoundEffectManager.Instance.PlaySound("RockBreak", gameObject.transform.position);
    }

    // This method will be called by PlayerInventory after the camera returns
    public void DestroyBarrier()
    {
        Debug.Log($"DestroyBarrier called on {gameObject.name} at {transform.position}");
        Destroy(gameObject);
    }
}
