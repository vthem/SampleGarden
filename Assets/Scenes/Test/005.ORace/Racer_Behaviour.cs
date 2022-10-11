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
	[Range(-1, 1)] public float input = 0f;

	public Quaternion OutRotation { get; private set; }

	private float currentVelocity;

	public void Update(Transform transform)
	{
		var hInputAxis = input;
		if (hInputAxis == 0)
		{
			input = hInputAxis = Input.GetAxis("Horizontal");
		}
		
		var targetZAngle = -hInputAxis * maxAngle;
		var currentZAngle = transform.localEulerAngles.z;
		var newZAngle = Mathf.SmoothDampAngle(currentZAngle, targetZAngle, ref currentVelocity, rollSmooth);
		OutRotation = Quaternion.AngleAxis(Mathf.DeltaAngle(currentZAngle, newZAngle), transform.forward);
	}
}

[System.Serializable]
public class YawModule
{
	[Range(0, 180)] public float angularSpeed = 100f;
	
	public Quaternion OutRotation { get; private set; }

	public void Update(Vector3 groundUp, float steering)
	{
		OutRotation = Quaternion.AngleAxis(angularSpeed * Time.deltaTime * steering, groundUp);
	}
}

[System.Serializable]
public class GroundModule
{
	public bool isValid = false;
	public Vector3 forward;
	public Vector3 right;
	public Vector3 up;
	public Vector3 gravity;

	public static int FrontIndex = 0;
	public static int CenterIndex = 1;
	public static int BackIndex = 2;
	public static int RightIndex = 3;
	public static int LeftIndex = 4;

	public struct GroundPosition
	{
		public bool Found { get; set; }
		public Vector3 Position { get; set; }
		public Vector3 Offset { get; set; }
	}

	private GroundPosition[] groundPositionArray =
	{
		new GroundPosition{ Found = false, Position = Vector3.zero, Offset = Vector3.forward },
		new GroundPosition{ Found = false, Position = Vector3.zero, Offset = Vector3.back },
		new GroundPosition{ Found = false, Position = Vector3.zero, Offset = Vector3.zero },
		new GroundPosition{ Found = false, Position = Vector3.zero, Offset = Vector3.right },
		new GroundPosition{ Found = false, Position = Vector3.zero, Offset = Vector3.left }
	};

	static bool FindGravityAtWorldPoint(Mesh mesh, Vector3 wPoint, Matrix4x4 localToWorld, ref Vector3 normal)
	{
		var vertices = mesh.vertices;
		float minDistance = float.MaxValue;
		int minIdx = -1;
		for (int i = 0; i < vertices.Length; ++i)
		{
			var wVertex = localToWorld.MultiplyPoint(vertices[i]);
			var sqDistance = (wVertex - wPoint).sqrMagnitude;
			if (sqDistance < minDistance)
			{
				minDistance = sqDistance;
				minIdx = i;
			}
		}
		if (minIdx < 0)
		{
			return false;
		}

		normal = -mesh.normals[minIdx];
		return true;
	}

	void UpdateGroundPositionArray(Transform transform)
	{
		isValid = false;

		for (int i = 0; i < groundPositionArray.Length; ++i)
		{
			var gp = groundPositionArray[i];
			gp.Found = false;
			gp.Position = Vector3.zero;

			Ray r = new Ray(transform.position + transform.TransformVector(gp.Offset), -transform.up);
			if (Physics.Raycast(r, out RaycastHit hit, 1000f))
			{
				gp.Found = true;
				gp.Position = hit.point;
				Debug.DrawLine(transform.position, hit.point);
			}

			groundPositionArray[i] = gp;
		}
	}

	private bool TryComputeForward()
	{
		var gpFront = groundPositionArray[FrontIndex];
		var gpBack = groundPositionArray[BackIndex];

		if (!gpFront.Found || !gpBack.Found)
		{
			forward = Vector3.zero;
			return false;
		}

		forward = (gpFront.Position - gpBack.Position).normalized;
		return true;
	}
	private bool TryComputeRight()
	{
		var gpRight = groundPositionArray[RightIndex];
		var gpLeft = groundPositionArray[LeftIndex];

		if (!gpRight.Found || !gpLeft.Found)
		{
			right = Vector3.zero;
			return false;
		}

		right = (gpRight.Position - gpLeft.Position).normalized;
		return true;
	}

	public void Update(Transform transform, Mesh worldMesh, Matrix4x4 localToWorld)
	{

		if (!FindGravityAtWorldPoint(worldMesh, transform.position, localToWorld, ref gravity))
		{
			isValid = false;
			return;
		}
		Debug.DrawLine(transform.position, transform.position + gravity * 3, Color.blue);


		UpdateGroundPositionArray(transform);

		isValid = TryComputeForward() && TryComputeRight();

		if (isValid)
		{
			up = Vector3.Cross(forward, right);
		}
	}

	public GroundPosition this[int index] {
		get => groundPositionArray[index];
	}
}

public class Racer_Behaviour : MonoBehaviour
{
	public AltitudeModule altitudeModule;
	public RollModule rollModule;
	public YawModule yawModule;
	public GroundModule groundModule;

	public GameObject worldMeshObj;
	private Mesh worldMesh;

	private void Start()
	{
		worldMesh = worldMeshObj.GetComponent<MeshFilter>().sharedMesh;
	}

	void Update()
	{
		groundModule.Update(transform, worldMesh, worldMeshObj.transform.localToWorldMatrix);

		var center = groundModule[GroundModule.CenterIndex];
		if (center.Found)
		{
			altitudeModule.Update(transform, center.Position);
		}

		rollModule.Update(transform);
		//yawModule.Update(groundModule.up, rollModule.input);
		yawModule.Update(-groundModule.gravity, rollModule.input);

		transform.rotation = /*rollModule.OutRotation **/ yawModule.OutRotation * transform.rotation;
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
