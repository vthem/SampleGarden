using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sun_LookAt : MonoBehaviour
{
	public World_Behaviour world;
	public Transform target;
	public Vector3 eulerOffset;

    // Update is called once per frame
    void Update()
    {
		World_Behaviour.GravityData gravity = default;
		if (!world.TryFindGravityAt(target.position, ref gravity))
		{
			Debug.LogError("gravity not found");
			return;
		}
		transform.forward = /*Quaternion.Euler(eulerOffset) **/ gravity.direction;
	}
}
