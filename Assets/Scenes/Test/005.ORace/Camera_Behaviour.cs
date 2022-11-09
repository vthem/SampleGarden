using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_Behaviour : MonoBehaviour
{
	public Transform target;

	public Vector3 lookAtTargetVector;
	public Vector3 positionTargetVector;

	public Vector3 WorldPositionTargetPosition
	{
		get
		{
			return target.TransformPoint(positionTargetVector);
		}
	}

	public Vector3 WorldLookAtTargetPosition
	{
		get
		{
			return target.TransformPoint(lookAtTargetVector);
		}
	}

	private void Start()
	{
		ForcePositionAndRotation();
	}

	void LateUpdate()
    {
		InternalUpdate();
	}

	void InternalUpdate()
	{
		transform.position = WorldPositionTargetPosition;
		transform.LookAt(WorldLookAtTargetPosition, target.up);
	}

	[ContextMenu("Force Position & Rotation")]
	void ForcePositionAndRotation()
	{
		InternalUpdate();
	}
}
