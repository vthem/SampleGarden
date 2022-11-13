using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateWheel_Behaviour : MonoBehaviour
{
	public float angularVelocity = 1f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(new Vector3(0, 0, angularVelocity * Time.deltaTime), Space.Self);
    }
}
