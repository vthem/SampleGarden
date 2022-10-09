using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif


[System.Serializable]
public class AltitudeModule
{
	public bool enable = false;
	public float downAltitudeSmooth = 1f;
	public float verticalMaxSpeed = 1f;
	public float hoverHeight = 0.5f;

	private Vector3 verticalVelocity;

	public void Update(Transform transform, Vector3 groundPoint)
	{
		if (!enable)
			return;

		var groundDir = (groundPoint - transform.position).normalized;
		var hoverTarget = groundPoint - groundDir * hoverHeight;
		transform.localPosition = Vector3.SmoothDamp(transform.localPosition, hoverTarget, ref verticalVelocity, downAltitudeSmooth, verticalMaxSpeed);
	}
}

[System.Serializable]
public class RollModule
{
	[Range(0, 1f)] public float rollSmooth = 1f;
	[Range(10, 90f)] public float maxAngle = 45f;

	public Quaternion rotation;
	private float currentVelocity;
	public float targetZAngle;
	public float currentZAngle;
	[Range(-1, 1)] public float input = 0f;

	public void Update(Transform transform)
	{
		var hInputAxis = input;
		if (hInputAxis == 0)
		{
			input = hInputAxis = Input.GetAxis("Horizontal");
		}
		
		targetZAngle = -hInputAxis * maxAngle;
		currentZAngle = transform.localEulerAngles.z;
		var newZAngle = Mathf.SmoothDampAngle(currentZAngle, targetZAngle, ref currentVelocity, rollSmooth);
		//rotation = Quaternion.Euler(0, 0, Mathf.DeltaAngle(currentZAngle, newZAngle));
		rotation = Quaternion.AngleAxis(Mathf.DeltaAngle(currentZAngle, newZAngle), transform.forward);
	}
}

[System.Serializable]
public class YawModule
{
	[Range(0, 90)] public float angularSpeed = 45f;
	public float input;
	public Quaternion rotation;

	public void Update(Transform transform)
	{
		//rotation = Quaternion.Euler(0, angularSpeed * Time.deltaTime * input, 0);
		rotation = Quaternion.AngleAxis(-angularSpeed * Time.deltaTime * input, transform.up);
	}
}

public class Racer_Behaviour : MonoBehaviour
{
	public AltitudeModule altitudeModule;
	public RollModule rollModule;
	public YawModule yawModule;

	public float zMaxSpeed = 1f;
	
	public float forwardSmoothTime = 1f;
	public float forwardMaxSpeed = 1f;

	public float yAngularSpeed = 1f;

	
	private Vector3 velocity;
	private Vector3 forwardVelocity;
	private float heading = 0f; // [0, 360[ degree

	private struct GroundPositionInfo
	{
		public bool Found { get; set; }
		public Vector3 Position { get; set; }
		public Vector3 Offset { get; set; }

		public static int FrontIndex = 0;
		public static int CenterIndex = 1;
		public static int BackIndex = 2;
		public static int RightIndex = 3;
		public static int LeftIndex = 4;
	}
	private GroundPositionInfo[] groundPositionInfoArray =
	{
		new GroundPositionInfo{ Found = false, Position = Vector3.zero, Offset = Vector3.forward },
		new GroundPositionInfo{ Found = false, Position = Vector3.zero, Offset = Vector3.back },
		new GroundPositionInfo{ Found = false, Position = Vector3.zero, Offset = Vector3.zero },
		new GroundPositionInfo{ Found = false, Position = Vector3.zero, Offset = Vector3.right },
		new GroundPositionInfo{ Found = false, Position = Vector3.zero, Offset = Vector3.left }
	};

	void UpdateGroundPositionInfoArray()
	{
		for (int i = 0; i < groundPositionInfoArray.Length; ++i)
		{
			var gp = groundPositionInfoArray[i];
			gp.Found = false;
			gp.Position = Vector3.zero;

			Ray r = new Ray(transform.position + transform.TransformVector(gp.Offset), -transform.up);
			if (Physics.Raycast(r, out RaycastHit hit, 1000f))
			{
				gp.Found = true;
				gp.Position = hit.point;
				Debug.DrawLine(transform.position, hit.point);
			}
			
			groundPositionInfoArray[i] = gp;
		}
	}

	bool TryComputeForward(out Vector3 forward)
	{
		var gpFront = groundPositionInfoArray[GroundPositionInfo.FrontIndex];
		var gpBack = groundPositionInfoArray[GroundPositionInfo.BackIndex];

		if (!gpFront.Found || !gpBack.Found)
		{
			forward = Vector3.zero;
			return false;
		}

		forward = (gpFront.Position - gpBack.Position).normalized;
		return true;
	}
	bool TryComputeRight(out Vector3 right)
	{
		var gpRight = groundPositionInfoArray[GroundPositionInfo.RightIndex];
		var gpLeft = groundPositionInfoArray[GroundPositionInfo.LeftIndex];

		if (!gpRight.Found || !gpLeft.Found)
		{
			right = Vector3.zero;
			return false;
		}

		right = (gpRight.Position - gpLeft.Position).normalized;
		return true;
	}

	// Update is called once per frame
	void Update()
	{
		UpdateGroundPositionInfoArray();

		var gpCenter = groundPositionInfoArray[GroundPositionInfo.CenterIndex];
		if (gpCenter.Found)
		{
			altitudeModule.Update(transform, gpCenter.Position);
		}

		//var hInputAxis = Input.GetAxis("Horizontal");
		//heading += yAngularSpeed * Time.deltaTime * hInputAxis;
		//if (heading > 360)
		//{
		//	heading = heading - 360f;
		//}
		//else if (heading < 0)
		//{
		//	heading += 360f;
		//}

		//if (TryComputeForward(out Vector3 forward) && TryComputeRight(out Vector3 right))
		//{
		//	Vector3 up = Vector3.Cross(forward, right);

		//	// rotate forward according
		//	var rot = Quaternion.AngleAxis(heading, up);
		//	forward = Vector3.SmoothDamp(forward, rot * forward, ref forwardVelocity, forwardSmoothTime, forwardMaxSpeed);
		//	Debug.DrawLine(transform.position, transform.position + transform.forward * 2, Color.blue);
		//	Debug.DrawLine(transform.position, transform.position + forward * 2, Color.green);
		//	//transform.rotation = Quaternion.LookRotation(forward, up);
		//}

		rollModule.Update(transform);
		yawModule.input = rollModule.input;
		yawModule.Update(transform);

		transform.rotation *= rollModule.rotation/* * yawModule.rotation*/;
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(Racer_Behaviour))]
	public class Racer_Editor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var racer = target as Racer_Behaviour;
			racer.altitudeModule.enable = EditorGUILayout.Toggle("Enable Fake Gravity", racer.altitudeModule.enable);
		}
	}
#endif
}
