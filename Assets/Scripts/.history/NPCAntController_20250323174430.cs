using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCAntController : MonoBehaviour
{
    public Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        animator.SetBool("isWalking", true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
