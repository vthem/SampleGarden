using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Behaviour : MonoBehaviour
{
	public Transform target;

	public Vector3 lookAtTargetOffset;
	public Vector3 positionTargetOffset;

	public float postionMaxSpeed = 1f;
	[Range(0, 1f)] public float positionSmoothTime = 1f;

	public float lookAtMaxSpeed = 1f;
	[Range(0, 1f)] public float lookAtSmoothTime = 1f;

	private Vector3 positionVelocity;
	private Vector3 lookAtVelocity;

	private Vector3 lookAtTargetPosition;

	private void Start()
	{
		ForcePositionAndRotation();
	}

	void LateUpdate()
    {
		transform.position = Vector3.SmoothDamp(transform.position, target.transform.position + positionTargetOffset, ref positionVelocity, positionSmoothTime, postionMaxSpeed);
		lookAtTargetPosition = Vector3.SmoothDamp(lookAtTargetPosition, target.position + lookAtTargetOffset, ref lookAtVelocity, lookAtSmoothTime, lookAtMaxSpeed);
		transform.LookAt(lookAtTargetPosition);
	}

	[ContextMenu("Force Position & Rotation")]
	void ForcePositionAndRotation()
	{
		lookAtTargetPosition = target.position + lookAtTargetOffset;
		transform.position = target.transform.position + positionTargetOffset;
		transform.LookAt(lookAtTargetPosition);
	}
}
