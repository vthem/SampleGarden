using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class RacerPhysic_Behaviour : MonoBehaviour
{
	[Range(-1, 1f)]
	public float yawInput;

	[Range(0, 50f)]
	public float maxSpeed = 1f;

	public float gravityAcceleration = 1f;
	public float hoverAltitude = 1f;

	public World_Behaviour world;

	public Vector3 outGravity;

	public bool debugUseVerticalInputAxis = true;
	public bool debugDrawGravity;
	public string debugFailReason;

    // Update is called once per frame
    void Update()
    {
		debugFailReason = string.Empty;
		// apply roll from input
		//var hInputAxis = yawInput;
		//if (hInputAxis == 0)
		//{
		//	hInputAxis = Input.GetAxis("Horizontal");
		//}

		// move forward, find the new position
		if (debugUseVerticalInputAxis)
		{
			maxSpeed *= Input.GetAxis("Vertical");
		}
		var translation = transform.forward * maxSpeed * Time.deltaTime;
		var newPosition = transform.position + translation;

		// find gravity, ground point at new position
		World_Behaviour.GravityData gravity = default;
		if (!world.TryFindGravityAt(newPosition, ref gravity))
		{
			debugFailReason = "gravity not found";
			return;
		}

		Ray r = new Ray(transform.position, outGravity);
		if (!Physics.Raycast(r, out RaycastHit groundHit))
		{
			debugFailReason = "ground not found";
			return;
		}

		DrawDebugGravity(gravity, groundHit);

		if (!ComputeBarycentricGravity(gravity.mesh, groundHit, out Vector3 barycentricGravity))
		{
			debugFailReason = "barycentric gravity not found";
			return;
		}

		// are we above or below the hover point ?
		//var hoverPoint = groundHit.point - barycentricGravity * hoverAltitude;
		if ((newPosition - groundHit.point).sqrMagnitude > (-barycentricGravity * hoverAltitude).sqrMagnitude)
		{ // above
			// TODO, we need to be sure that we are on the right side of the world !
		}
		else
		{ // below

		}

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

		barycentricGravity = - interpolatedNormal;
		return true;
	}

	void DrawDebugGravity(World_Behaviour.GravityData gravity, RaycastHit hit)
	{
		if (!debugDrawGravity)
		{
			return;
		}
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
}
