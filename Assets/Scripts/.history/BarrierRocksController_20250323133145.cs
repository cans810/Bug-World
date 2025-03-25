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
        Debug.Log("BarrierRocksController.RemoveBarrier called - triggering disappear animation");
        GetComponent<Animator>().SetBool("Dissappear", true);
    }

    public void DestroyBarrier(){
        Destroy(gameObject);
    }

    public void PlayRockBreakSound(){
        SoundEffectManager.Instance.PlaySound("RockBreak", gameObject.transform.position);
    }
}
