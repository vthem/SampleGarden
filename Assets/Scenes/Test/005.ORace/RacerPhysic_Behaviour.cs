using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class RacerPhysic_Behaviour : MonoBehaviour
{
	[Range(-1, 1f)]
	public float yawInput;

	[Range(0, 500f)]
	public float maxSpeed = 1f;

	public float gravityAcceleration = 1f;
	public float hoverAltitude = 1f;

	public World_Behaviour world;

	public bool debugUseVerticalInputAxis = true;
	public bool enableDrawDebugGravity;

	private float verticalSpeed = 0f;

    // Update is called once per frame
    void Update()
    {
		world.EnableDrawDebugGravity = enableDrawDebugGravity;
		if (!world.TryFindBarycentricGravityAt(transform.position, out World_Behaviour.BaricentryGravityData gravityData))
		{
			Debug.LogError("gravity not found");
			return;
		}

		var groundPoint = gravityData.hit.point;
		var gravity = gravityData.direction;
		Debug.DrawLine(transform.position, groundPoint, Color.white);

		// find forward only based on gravity and angle
		var forward = Quaternion.AngleAxis(transform.rotation.eulerAngles.y, -gravity) * Vector3.forward;
		Debug.DrawLine(transform.position, transform.position + forward, Color.blue);
		var nextGroundPoint = groundPoint + forward * maxSpeed * Time.deltaTime;

		if (!world.TryFindBarycentricGravityAt(nextGroundPoint, out World_Behaviour.BaricentryGravityData nextGravityData))
		{
			Debug.LogError("gravity not found (nextGroundPoint)");
			return;
		}
		
		var nextGravity = nextGravityData.direction;
		nextGroundPoint = nextGravityData.hit.point;

		var altitude = (transform.position - groundPoint).magnitude;
		Debug.DrawLine(nextGroundPoint, nextGroundPoint - nextGravity * altitude, Color.yellow);
		var nextPosition = nextGroundPoint -nextGravity * altitude;
		//var nextHoverPoint = nextGroundPoint - nextGravity * hoverAltitude;
		//Color debugColor = Color.white;
		//if (Vector3.Dot(-nextGravity, (nextPosition - nextHoverPoint)) > 0)
		//{ // above hoverPoint
		//	verticalSpeed += gravityAcceleration * Time.deltaTime;
		//	nextPosition += nextGravity * verticalSpeed * Time.deltaTime;
		//	debugColor = Color.red;
		//}

		//if (Vector3.Dot(-nextGravity, (nextPosition - nextHoverPoint)) < 0)
		//{ // below hoverPoint
		//	verticalSpeed = 0f;
		//	nextPosition = nextHoverPoint;
		//	debugColor = Color.green;
		//}

		transform.position = nextPosition;
	}
}
