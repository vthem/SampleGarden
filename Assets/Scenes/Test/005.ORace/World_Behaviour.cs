using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class World_Behaviour : MonoBehaviour
{
	public bool EnableDrawDebugGravity { get; set; } = false;

	public struct GravityData
	{
		public Vector3 direction;
		public Mesh mesh;
		public GameObject gameObject;
	}

	public struct BaricentryGravityData
	{
		public Vector3 direction;
		public RaycastHit hit;
	}

	public GameObject[] worldObjectArray;

	private void Awake()
	{
		var l = new List<GameObject>();
		for (int i = 0; i < transform.childCount; ++i)
		{
			if (!transform.GetChild(i).gameObject.activeInHierarchy)
			{
				continue;
			}
			l.Add(transform.GetChild(i).gameObject);
		}
		worldObjectArray = l.ToArray();
	}

	public bool TryFindGravityAt(Vector3 wPoint, ref GravityData gravity)
	{
		if (!TryFindGameObjectAt(wPoint, out gravity.gameObject))
		{
			return false;
		}

		Mesh mesh = gravity.gameObject.GetComponent<MeshCollider>().sharedMesh;
		Matrix4x4 localToWorld = gravity.gameObject.transform.localToWorldMatrix;

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

		gravity.direction = -mesh.normals[minIdx];
		gravity.mesh = mesh;

		return true;
	}

	public bool TryFindBarycentricGravityAt(Vector3 wPoint, out BaricentryGravityData gravity)
	{
		// find gravity, ground point at new position
		GravityData gravityData = default;
		if (!TryFindGravityAt(wPoint, ref gravityData))
		{
			Debug.LogError("TryFindBarycentricGravityAt > gravity not found");
			gravity = default;
			return false;
		}
		Ray r = new Ray(wPoint - gravityData.direction, gravityData.direction);
		if (!Physics.Raycast(r, out RaycastHit groundHit, 100f))
		{
			Debug.LogError("TryFindBarycentricGravityAt > ground not found");
			gravity = default;
			return false;
		}

		if (!ComputeBarycentricGravity(gravityData.mesh, groundHit, out Vector3 direction))
		{
			Debug.LogError("TryFindBarycentricGravityAt > barycentric gravity not found");
			gravity = default;
			return false;
		}

		gravity = new BaricentryGravityData();
		gravity.direction = direction;
		gravity.hit = groundHit;

		if (EnableDrawDebugGravity)
		{
			DrawDebugGravity(gravityData, groundHit);
		}
		
		return true;
	}


	void DrawDebugGravity(World_Behaviour.GravityData gravity, RaycastHit hit)
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


	private bool ComputeBarycentricGravity(Mesh mesh, RaycastHit hit, out Vector3 barycentricGravity)
	{
		Vector3[] normals = mesh.normals;
		int[] triangles = mesh.triangles;

		if (hit.triangleIndex >= triangles.Length)
		{
			barycentricGravity = Vector3.up;
			return false;
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

		barycentricGravity = -interpolatedNormal;
		return true;
	}

	private bool TryFindGameObjectAt(Vector3 wPoint, out GameObject worldGameObject)
	{
		for (int i = 0; i < worldObjectArray.Length; ++i)
		{
			if (!worldObjectArray[i].activeInHierarchy)
			{
				continue;
			}
			if (worldObjectArray[i].TryGetComponent<MeshCollider>(out MeshCollider meshCollider))
			{
				if (meshCollider.bounds.Contains(wPoint))
				{
					worldGameObject = worldObjectArray[i];
					return true;
				}
			}
		}
		worldGameObject = null;
		return false;
	}
}
