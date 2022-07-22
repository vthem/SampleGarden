using UnityEngine;

namespace _019_EarthMap
{
	public class MouseOrbit_MonoBehaviour : MonoBehaviour
	{
		public new Camera camera;
		public Vector3 currentRotation;
		public float distance = 1;
		public float smooth = 0.01f;
		public Vector2 sensitvity = new Vector2(1f, 1f);

		private Vector2 lastMousePosition;
		private int lastMousePositionFrame;
		private Vector3 targetPosition;

		private void Update()
		{
			if (Time.frameCount - lastMousePositionFrame == 1)
			{
				if (Input.GetMouseButton(1))
				{
					var current = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
					var delta = current - lastMousePosition;

					currentRotation.z -= delta.y * sensitvity.x;
					currentRotation.z = Mathf.Clamp(currentRotation.z, 0f, 80f);
					currentRotation.y += delta.x * sensitvity.y;
				}

				targetPosition = (Quaternion.Euler(0, currentRotation.y, currentRotation.z) * Vector3.right) * distance;
			}

			smooth = Mathf.Clamp(smooth, 0.001f, 1f);
			lastMousePosition = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
			lastMousePositionFrame = Time.frameCount;
		}

		void LateUpdate()
		{
			camera.transform.position = Vector3.Lerp(camera.transform.position, targetPosition, smooth);
			camera.transform.LookAt(Vector3.zero);
		}
	}
}
