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
	public float gravityRotationSpeed = 100f;
	public bool drawDebugGravity = false;
	public float gravitySmoothTime = .5f;

	public Vector3 outGravity;
	public Vector3 outGravitySmooth;
	public Vector3 outBarycentricGravity;
	public bool outIsValid;	
	public string outErrorReason;
	public Vector3 outGroundPoint;

	public World_Behaviour world;

	private Vector3 verticalVelocity;
	private Vector3 gravityVelocity;

	public void ForcePositionGround(Transform transform)
	{
		World_Behaviour.GravityData gravity = default;
		if (!world.TryFindGravityAt(transform.position, ref gravity))
		{
			return;
		}
		Ray r = new Ray(transform.position, outGravity);
		if (!Physics.Raycast(r, out RaycastHit hit))
		{
			return;
		}
		transform.position = hit.point;
	}

	public void Update(Transform transform)
	{
		if (!enable)
		{
			outIsValid = true;
			return;
		}

		outErrorReason = "not initialized";
		outIsValid = false;

		World_Behaviour.GravityData gravity = default;
		if (!world.TryFindGravityAt(transform.position, ref gravity))
		{
			outErrorReason = "gravity not found";
			return;
		}


		outGravity = gravity.direction;
		if (drawDebugGravity)
		{
			Debug.DrawLine(transform.position, transform.position + outGravity * 3, Color.blue);
		}

		outGravitySmooth = Vector3.SmoothDamp(outGravitySmooth, outGravity, ref gravityVelocity, gravitySmoothTime);

		//var groundPoint = ground[GroundModule.CenterIndex].Position;
		//var groundDir = (groundPoint - transform.position).normalized;

		Ray r = new Ray(transform.position, outGravity);
		if (!Physics.Raycast(r, out RaycastHit hit))
		{
			outErrorReason = "ground not found";
			return;
		}
		outGroundPoint = hit.point;

		if (drawDebugGravity)
		{
			var mesh = gravity.mesh;
			var triangles = mesh.triangles;

			var vertices = mesh.vertices;

			var v0 = gravity.gameObject.transform.TransformPoint(vertices[triangles[hit.triangleIndex * 3 + 0]]);
			var v1 = gravity.gameObject.transform.TransformPoint(vertices[triangles[hit.triangleIndex * 3 + 1]]);
			var v2 = gravity.gameObject.transform.TransformPoint(vertices[triangles[hit.triangleIndex * 3 + 2]]);

			Debug.DrawLine(v0, v1, Color.magenta);
			Debug.DrawLine(v1, v2, Color.magenta);
			Debug.DrawLine(v2, v0, Color.magenta);

			Vector3[] normals = mesh.normals;
			Vector3 n0 = normals[triangles[hit.triangleIndex * 3 + 0]];
			Vector3 n1 = normals[triangles[hit.triangleIndex * 3 + 1]];
			Vector3 n2 = normals[triangles[hit.triangleIndex * 3 + 2]];


			Debug.DrawLine(v0, v0 + n0, Color.yellow);
			Debug.DrawLine(v1, v1 + n1, Color.yellow);
			Debug.DrawLine(v2, v2 + n2, Color.yellow);

		}

		ComputeBarycentricGravity(gravity.mesh, hit);


		var hoverTarget = outGroundPoint - outGravity * hoverHeight;
		transform.localPosition = Vector3.SmoothDamp(transform.localPosition, hoverTarget, ref verticalVelocity, downAltitudeSmooth, verticalMaxSpeed);

		var outRotation = Quaternion.FromToRotation(-transform.up, outBarycentricGravity);
		transform.rotation = outRotation * transform.rotation;

		outIsValid = true;
	}

	private void ComputeBarycentricGravity(Mesh mesh, RaycastHit hit)
	{
		Vector3[] normals = mesh.normals;
		int[] triangles = mesh.triangles;

		if (hit.triangleIndex >= triangles.Length)
		{
			Debug.LogError("hit.triangleIndex >= triangles.Length");
			return;
		}
		// Extract local space normals of the triangle we hit
		Vector3 n0 = normals[triangles[hit.triangleIndex * 3 + 0]];
		Vector3 n1 = normals[triangles[hit.triangleIndex * 3 + 1]];
		Vector3 n2 = normals[triangles[hit.triangleIndex * 3 + 2]];

		// interpolate using the barycentric coordinate of the hitpoint
		Vector3 baryCenter = hit.barycentricCoordinate;

		// Use barycentric coordinate to interpolate normal
		Vector3 interpolatedNormal = n0 * baryCenter.x + n1 * baryCenter.y + n2 * baryCenter.z;
		// normalize the interpolated normal
		interpolatedNormal = interpolatedNormal.normalized;

		outBarycentricGravity = -interpolatedNormal;
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

	public void Update(Transform transform, CollisionModule collisionModule)
	{
		if (!enable)
			return;

		var translation = transform.forward * maxSpeed * Time.deltaTime;
		if (collisionModule.hasHit && (collisionModule.raycastHit.point - transform.position).sqrMagnitude <= translation.sqrMagnitude)
		{
			transform.position = collisionModule.raycastHit.point;
			enable = false;
		}
		else
		{
			transform.position += translation;
		}
	}
}

[System.Serializable]
public class CollisionModule : BaseModule
{
	public bool hasHit = false;
	public RaycastHit raycastHit;

	public void Update(Transform transform)
	{
		hasHit = false;
		if (Physics.Raycast(transform.position, transform.forward, out raycastHit, 50f, 1 << LayerMask.NameToLayer("Obstacle")))
		{
			hasHit = true;
		}
	}
}

public class Racer_Behaviour : MonoBehaviour
{
	public GravityModule gravityModule;
	public RollModule rollModule;
	public YawModule yawModule;
	public PitchModule pichModule;
	public MoveModule moveModule;
	public CollisionModule collisionModule;

	void Update()
	{
		gravityModule.Update(transform);
		if (!gravityModule.outIsValid)
		{
			Debug.LogError($"gravityModule not valid! reason:{gravityModule.outErrorReason}");
			return;
		}

		rollModule.Update(transform);
		yawModule.Update(transform, rollModule);
		//pichModule.Update(transform, transform.forward, groundModule.forward);
		collisionModule.Update(transform);
		moveModule.Update(transform, collisionModule);
	}

	[ContextMenu("AutoPlace")]
	public void AutoPlace()
	{
		gravityModule.ForcePositionGround(transform);
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
