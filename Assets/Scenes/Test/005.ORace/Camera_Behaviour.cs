using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Behaviour : MonoBehaviour
{
	public Racer_Behaviour racer;

	public Vector3 lookAtTargetVector;
	public Vector3 positionTargetVector;

	public float postionMaxSpeed = 1f;
	[Range(0, 1f)] public float positionSmoothTime = 1f;

	public float lookAtMaxSpeed = 1f;
	[Range(0, 1f)] public float lookAtSmoothTime = 1f;

	private Vector3 positionVelocity;
	private Vector3 lookAtVelocity;

	private Vector3 currentLookAtTargetPosition;

	public Vector3 WorldPositionTargetPosition
	{
		get
		{
			return racer.transform.position + racer.transform.TransformVector(positionTargetVector);
		}
	}

	public Vector3 WorldLookAtTargetPosition
	{
		get
		{
			return racer.transform.position + racer.transform.TransformVector(lookAtTargetVector);
		}
	}

	private void Start()
	{
		ForcePositionAndRotation();
	}

	void LateUpdate()
    {
		transform.position = Vector3.SmoothDamp(transform.position, WorldPositionTargetPosition, ref positionVelocity, positionSmoothTime, postionMaxSpeed);
		currentLookAtTargetPosition = Vector3.SmoothDamp(currentLookAtTargetPosition, WorldLookAtTargetPosition, ref lookAtVelocity, lookAtSmoothTime, lookAtMaxSpeed);
		transform.LookAt(currentLookAtTargetPosition, -racer.gravityModule.outGravitySmooth);
	}

	[ContextMenu("Force Position & Rotation")]
	void ForcePositionAndRotation()
	{
		currentLookAtTargetPosition = WorldLookAtTargetPosition;
		transform.position = WorldPositionTargetPosition;
		transform.LookAt(currentLookAtTargetPosition);
	}
}
