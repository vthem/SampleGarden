using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using _013_TesselatedWorm;

#if UNITY_EDITOR
using UnityEditor;
#endif


[System.Serializable]
public class BaseModule
{
	public bool enable = true;
}


[System.Serializable]
public class GravityModule : BaseModule
{
	public float downAltitudeSmooth = 1f;
	public float verticalMaxSpeed = 1f;
	public float hoverHeight = 0.5f;
	public Vector3 outGravity;
	public bool drawDebugGravity = false;

	public bool outIsValid;
	public string outErrorReason;
	public Vector3 outGroundPoint;

	private Vector3 verticalVelocity;


	bool FindGravityAtWorldPoint(Mesh mesh, Vector3 wPoint, Matrix4x4 localToWorld, ref Vector3 normal)
	{
		var vertices = mesh.vertices;
		float minDistance = float.MaxValue;
		int minIdx = -1;
		Vector3 wGravityPoint = Vector3.zero;
		for (int i = 0; i < vertices.Length; ++i)
		{
			var wVertex = localToWorld.MultiplyPoint(vertices[i]);
			var sqDistance = (wVertex - wPoint).sqrMagnitude;
			if (sqDistance < minDistance)
			{
				wGravityPoint = wVertex;
				minDistance = sqDistance;
				minIdx = i;
			}
		}
		if (minIdx < 0)
		{
			return false;
		}

		normal = -mesh.normals[minIdx];

		if (drawDebugGravity)
		{
			Debug.DrawLine(wGravityPoint, wGravityPoint - normal * 3f, Color.cyan);
		}

		return true;
	}

	public void Update(Transform transform, Mesh worldMesh, Matrix4x4 localToWorld)
	{
		if (!enable)
		{
			outIsValid = true;
			return;
		}

		outErrorReason = "not initialized";
		outIsValid = false;

		if (!FindGravityAtWorldPoint(worldMesh, transform.position, localToWorld, ref outGravity))
		{
			outErrorReason = "Gravity not found";
			return;
		}
		if (drawDebugGravity)
		{
			Debug.DrawLine(transform.position, transform.position + outGravity * 3, Color.blue);
		}


		//var groundPoint = ground[GroundModule.CenterIndex].Position;
		//var groundDir = (groundPoint - transform.position).normalized;

		Ray r = new Ray(transform.position, outGravity);
		if (!Physics.Raycast(r, out RaycastHit hit, 1000f))
		{
			outErrorReason = "Ground not found";
			return;
		}
		outGroundPoint = hit.point;

		var hoverTarget = outGroundPoint - outGravity * hoverHeight;
		transform.localPosition = Vector3.SmoothDamp(transform.localPosition, hoverTarget, ref verticalVelocity, downAltitudeSmooth, verticalMaxSpeed);

		outIsValid = true;
	}
}

[System.Serializable]
public class RollModule : BaseModule
{
	[Range(0, 1f)] public float rollSmooth = 1f;
	[Range(10, 90f)] public float maxAngle = 45f;
	[Range(-1, 1)] public float input = 0f;

	public float OutNormalizedRoll { get; private set; }

	public void Update(Transform transform)
	{
		if (!enable)
			return;

		var hInputAxis = input;
		if (hInputAxis == 0)
		{
			hInputAxis = Input.GetAxis("Horizontal");
		}

		var targetUp = Quaternion.AngleAxis(-hInputAxis * maxAngle, transform.forward) * transform.up;
		Debug.DrawLine(transform.position, transform.position + targetUp * 2, Color.magenta);

		var rot = Quaternion.FromToRotation(transform.up, targetUp);
		OutNormalizedRoll = hInputAxis;

		transform.rotation = rot * transform.rotation;
	}
}

[System.Serializable]
public class YawModule : BaseModule
{
	[Range(0, 180)] public float angularSpeed = 100f;

	public void Update(Transform transform, RollModule roll)
	{
		if (!enable)
			return;

		var rot = Quaternion.AngleAxis(angularSpeed * Time.deltaTime * roll.OutNormalizedRoll, transform.up);
		transform.rotation = rot * transform.rotation;
	}
}

[System.Serializable]
public class PitchModule : BaseModule
{
	[Range(0, 1f)] public float smooth = 1f;
	[Range(0, 50f)] public float maxSpeed = 1f;

	private Vector3 velocity;

	public Quaternion OutRotation { get; private set; }

	public void Update(Transform transform, Vector3 forward, Vector3 groundForward)
	{
		var newForward = Vector3.SmoothDamp(forward, groundForward, ref velocity, smooth, maxSpeed);
		OutRotation = Quaternion.FromToRotation(forward, newForward);
		transform.rotation = OutRotation * transform.rotation;
	}
}

[System.Serializable]
public class MoveModule : BaseModule
{
	[Range(0, 50f)] public float maxSpeed = 1f;

	public void Update(Transform transform)
	{
		if (!enable)
			return;
		var translation = transform.forward * maxSpeed * Time.deltaTime;
		transform.position += translation;
	}
}

[System.Serializable]
public class GroundModule : BaseModule
{
	public bool outIsValid = false;
	public Vector3 forward;
	public Vector3 right;
	public Vector3 up;

	public static int FrontIndex = 0;
	public static int CenterIndex = 1;
	public static int BackIndex = 2;
	public static int RightIndex = 3;
	public static int LeftIndex = 4;

	public bool drawDebugGroundPosition = false;

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
	private Quaternion gravityRotationDerivative;
	public float gravityRotationSpeed = 100f;

	void UpdateGroundPositionArray(Transform transform, Vector3 gravity)
	{
		outIsValid = false;

		for (int i = 0; i < groundPositionArray.Length; ++i)
		{
			var gp = groundPositionArray[i];
			gp.Found = false;
			gp.Position = Vector3.zero;

			Ray r = new Ray(transform.position + transform.TransformVector(gp.Offset), gravity);
			if (Physics.Raycast(r, out RaycastHit hit, 1000f))
			{
				gp.Found = true;
				gp.Position = hit.point;
				if (drawDebugGroundPosition)
				{
					Debug.DrawLine(transform.position, hit.point);
				}
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
	public bool newRotation = false;
	public void Update(Transform transform, GravityModule gravityModule)
	{
		UpdateGroundPositionArray(transform, gravityModule.outGravity);

		outIsValid = TryComputeForward() && TryComputeRight();

		if (outIsValid)
		{
			up = Vector3.Cross(forward, right);
		}

		//outRotation = QuaternionUtil.SmoothDamp(transform.rotation, Quaternion.FromToRotation(-transform.up, gravity), ref gravityRotationDerivative, gravitySmoothTime);
		var outRotation = Quaternion.FromToRotation(-transform.up, gravityModule.outGravity);
		if (newRotation)
		{
			transform.rotation = Quaternion.RotateTowards(transform.rotation,  outRotation * transform.rotation, Time.deltaTime * gravityRotationSpeed);

		}
		else
		{
			transform.rotation = outRotation * transform.rotation;
		}
	}

	public GroundPosition this[int index]
	{
		get => groundPositionArray[index];
	}
}

public class Racer_Behaviour : MonoBehaviour
{
	public GravityModule gravityModule;
	public RollModule rollModule;
	public YawModule yawModule;
	public GroundModule groundModule;
	public PitchModule pichModule;
	public MoveModule moveModule;

	public GameObject worldMeshObj;
	private Mesh worldMesh;

	private BaseModule[] modules;

	private void Start()
	{
		worldMesh = worldMeshObj.GetComponent<MeshFilter>().sharedMesh;
		modules = new BaseModule[] { gravityModule, rollModule, yawModule, groundModule, pichModule, moveModule };
	}

	void Update()
	{
		gravityModule.Update(transform, worldMesh, worldMeshObj.transform.localToWorldMatrix);
		if (!gravityModule.outIsValid)
		{
			Debug.LogError($"gravityModule not valid! reason:{gravityModule.outErrorReason}");
			return;
		}

		groundModule.Update(transform, gravityModule);
		if (!groundModule.outIsValid)
		{
			Debug.LogError("! not valid!");
			return;
		}

		rollModule.Update(transform);
		yawModule.Update(transform, rollModule);
		//pichModule.Update(transform, transform.forward, groundModule.forward); ;
		moveModule.Update(transform);
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(Racer_Behaviour))]
	public class Racer_Editor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var racer = target as Racer_Behaviour;
			racer.gravityModule.enable = EditorGUILayout.Toggle("Enable Fake Gravity", racer.gravityModule.enable);
		}
	}
#endif
}
