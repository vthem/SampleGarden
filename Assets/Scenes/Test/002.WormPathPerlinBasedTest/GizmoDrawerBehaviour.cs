using UnityEngine;

namespace _002.WormPathPerlinBasedTest
{
	public class GizmoDrawerBehaviour : MonoBehaviour
	{
		public PerlinWormConfig config;

		private void OnDrawGizmos()
		{
			var worm = new PerlinWorm();
			worm.config = config;
			worm.BuildPointsArray();

			void GizmoDrawCircle(Vector3 forward, Vector3 up, Vector3 position, float radius, int segmentCount)
			{
				Vector3 prev = Vector3.Cross(forward, up);
				Quaternion rot = Quaternion.AngleAxis(360f / segmentCount, forward);
				for (int i = 0; i < segmentCount; ++i)
				{
					Vector3 cur = rot * prev;
					Gizmos.DrawLine(position + prev * radius, position + cur * radius);
					prev = cur;
				}
			}

			for (int i = 1; i < worm.Count; ++i)
			{
				//float radius = Mathf.PerlinNoise(0f, i * config.step + config.worldPosZ + config.radiusOffset);
				Gizmos.DrawLine(worm.GetPosition(i - 1), worm.GetPosition(i));
				Vector3 forward = worm.GetForward(i);
				Vector3 up = worm.GetUp(i);
				GizmoDrawCircle(forward, up, worm.GetPosition(i), config.wormRadius, 20);
			}
		}
	}
}
