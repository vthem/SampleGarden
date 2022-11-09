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

	public bool debugUseVerticalInputAxis = true;
	public bool debugDrawGravity;

	private float verticalSpeed = 0f;

    // Update is called once per frame
    void Update()
    {
		// apply roll from input
		//var hInputAxis = yawInput;
		//if (hInputAxis == 0)
		//{
		//	hInputAxis = Input.GetAxis("Horizontal");
		//}

		// move forward, find the new position
		var updatedMaxSpeed = maxSpeed;
		if (debugUseVerticalInputAxis)
		{
			updatedMaxSpeed = maxSpeed * Input.GetAxis("Vertical");
		}
		var translation = transform.forward * .5f; /** updatedMaxSpeed * Time.deltaTime*/;
		var newPosition = transform.position + translation;

		// find gravity, ground point at new position
		World_Behaviour.GravityData gravity = default;
		if (!world.TryFindGravityAt(newPosition, ref gravity))
		{
			Debug.LogError("gravity not found");
			return;
		}
		Ray r = new Ray(newPosition, gravity.direction);
		if (!Physics.Raycast(r, out RaycastHit groundHit))
		{
			Debug.LogError("ground not found");
			return;
		}

		DrawDebugGravity(gravity, groundHit);

		if (!ComputeBarycentricGravity(gravity.mesh, groundHit, out Vector3 barycentricGravity))
		{
			Debug.LogError("barycentric gravity not found");
			return;
		}

		var hoverPoint = groundHit.point - barycentricGravity * hoverAltitude;

		Color debugColor = Color.white;
		if (Vector3.Dot(-barycentricGravity, (newPosition - hoverPoint)) > 0)
		{ // above hoverPoint
			verticalSpeed += gravityAcceleration * Time.deltaTime;
			newPosition += barycentricGravity * verticalSpeed * Time.deltaTime;
			debugColor = Color.red;
		}

		if (Vector3.Dot(-barycentricGravity, (newPosition - hoverPoint)) < 0)
		{ // below hoverPoint
			verticalSpeed = 0f;
			newPosition = hoverPoint;
			debugColor = Color.green;
		}
		Debug.DrawLine(groundHit.point, hoverPoint, debugColor);
		Debug.DrawLine(hoverPoint, newPosition, Color.magenta);

		// compute the new forward
		Vector3 newForward = (newPosition - transform.position).normalized;
		Vector3 down = Vector3.Cross(transform.right, newForward);
		transform.rotation = Quaternion.LookRotation(newForward, -down);
		transform.position += newForward * Time.deltaTime * updatedMaxSpeed;
		//if (TryGetGroundPoinAt(transform.position + transform.forward * .5f, out Vector3 forwardGoundPoint))
		//{
		//	Vector3 newForward = (forwardGoundPoint -groundHit.point).normalized;
		//	Vector3 down = Vector3.Cross(transform.right, newForward);
		//	transform.rotation = Quaternion.LookRotation(newForward, -down);
		//}

	}

	bool TryGetGroundPoinAt(Vector3 position, out Vector3 forwardGrountPoint)
	{
		// find gravity, ground point at new position
		World_Behaviour.GravityData gravity = default;
		if (!world.TryFindGravityAt(position, ref gravity))
		{
			forwardGrountPoint = Vector3.zero;
			Debug.LogError("gravity not found");
			return false;
		}
		Ray r = new Ray(position, gravity.direction);
		if (!Physics.Raycast(r, out RaycastHit groundHit))
		{
			forwardGrountPoint = Vector3.zero;
			Debug.LogError("ground not found");
			return false;
		}

		forwardGrountPoint = groundHit.point;

		return true;
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
