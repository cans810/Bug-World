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

    public void RemoveBarrier(){
        GetComponent<Animator>().SetBool("Dissappear", true);
    }

    public void DestroyBarrier(){
        Destroy(gameObject);
    }

    public void PlayRockBreakSound()
    {
        // Get the AudioSource component attached to the GameObject
        AudioSource audioSource = GetComponent<AudioSource>();
        
        // Check if the AudioSource is attached and has a clip assigned
    
}
