using UnityEngine;

namespace _011_PlaneQuadTree
{
	public class MousePositionOnPlaneBehaviour : MonoBehaviour
	{
		public GameObject posObj;

		// Start is called before the first frame update
		void Start()
		{

		}

		// Update is called once per frame
		void Update()
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			var plane = new Plane(Vector3.up, 0f);

			if (plane.Raycast(ray, out float d))
			{
				//Get the point that is clicked
				Vector3 hitPoint = ray.GetPoint(d);

				//Move your cube GameObject to the point where you clicked
				posObj.transform.position = hitPoint;
			}
		}
	}

}