using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Racer_Behaviour : MonoBehaviour
{
	public float ySmooth = 1f;
	public float yMaxSpeed = 1f;
	public float zMaxSpeed = 1f;
	public float hoverHeight = 0.5f;
	public float forwardSmoothTime = 1f;
	public float forwardMaxSpeed = 1f;

	public float yAngularSpeed = 1f;

	private bool enableFakeGravity = false;
	private Vector3 yVelocity;
	private Vector3 velocity;
	private Vector3 forwardVelocity;

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
		if (enableFakeGravity && gpCenter.Found)
		{
			var groundDir = (gpCenter.Position - transform.position).normalized;
			var hoverTarget = gpCenter.Position - groundDir * hoverHeight;
			transform.localPosition = Vector3.SmoothDamp(transform.localPosition, hoverTarget, ref yVelocity, ySmooth, yMaxSpeed);
		}

		if (TryComputeForward(out Vector3 forward) && TryComputeRight(out Vector3 right))
		{
			Vector3 up = Vector3.Cross(forward, right);

			// rotate forward according
			var hInputAxis = Input.GetAxis("Horizontal");
			var rot = Quaternion.AngleAxis(yAngularSpeed * Time.deltaTime * hInputAxis, up);
			forward = Vector3.SmoothDamp(forward, rot * forward, ref forwardVelocity, forwardSmoothTime, forwardMaxSpeed);
			Debug.DrawLine(transform.position, transform.position + transform.forward * 2, Color.blue);
			Debug.DrawLine(transform.position, transform.position + forward * 2, Color.green);
			transform.rotation = Quaternion.LookRotation(forward, up);
		}
	}

	private void OnDrawGizmos()
	{

	}

#if UNITY_EDITOR
	[CustomEditor(typeof(Racer_Behaviour))]
	public class Racer_Editor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var racer = target as Racer_Behaviour;
			racer.enableFakeGravity = EditorGUILayout.Toggle("Enable Fake Gravity", racer.enableFakeGravity);
		}
	}
#endif
}
