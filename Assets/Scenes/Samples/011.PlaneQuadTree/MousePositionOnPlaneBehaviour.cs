using UnityEngine;

namespace _011_PlaneQuadTree
{
	public class MousePositionOnPlaneBehaviour : MonoBehaviour
	{
		private bool enableMouseTracking = true;

		// Update is called once per frame
		void Update()
		{
			if (Input.GetKeyDown(KeyCode.L))
			{
				enableMouseTracking = !enableMouseTracking;
			}

			if (!enableMouseTracking)
				return;

			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			var plane = new Plane(Vector3.up, 0f);

			if (plane.Raycast(ray, out float d))
			{
				//Get the point that is clicked
				Vector3 hitPoint = ray.GetPoint(d);

				//Move your cube GameObject to the point where you clicked
				transform.position = hitPoint;
			}
		}
	}

}