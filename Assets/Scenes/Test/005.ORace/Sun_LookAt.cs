using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sun_LookAt : MonoBehaviour
{
	public Racer_Behaviour racer;
	public Vector3 eulerOffset;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		transform.forward = Quaternion.Euler(eulerOffset) * racer.gravityModule.outBarycentricGravity;
	}
}
