using UnityEngine;

namespace _002.WormPathPerlinBasedTest
{
	public class GizmoDrawerBehaviour : MonoBehaviour
	{
		public PerlinWormConfig config;
		public bool interpolate = false;
		[Range(1, 1000)]
		public int inverseStep = 1;
		[Range(0, 1)]
		public float t;

		private void OnDrawGizmos()
		{
			var worm = new PerlinWorm();
			worm.config = config;
			worm.Update();

			if (interpolate)
			{
				DrawInterpolated(worm);
			}
			else
			{
				DrawIndexed(worm);
			}
		}

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

		void DrawInterpolated(PerlinWorm worm)
		{
			if (inverseStep <= 0)
				return;

			float step = 1f / inverseStep;
			for (float t = step; t <= 1f; t += step)
			{
				Gizmos.DrawLine(worm.GetPositionNormalized(t - step), worm.GetPositionNormalized(t));
				Vector3 forward = worm.GetForwardNormalized(t);
				Vector3 up = worm.GetUpNormalized(t);
				GizmoDrawCircle(forward, up, worm.GetPositionNormalized(t), config.wormRadius, 20);
			}
		}

		void DrawIndexed(PerlinWorm worm)
		{
			for (int i = 1; i < worm.Count; ++i)
			{
				//float radius = Mathf.PerlinNoise(0f, i * config.step + config.worldPosZ + config.radiusOffset);
				Gizmos.DrawLine(worm.GetPosition(i - 1), worm.GetPosition(i));
				Vector3 forward = worm.GetForward(i);
				Vector3 up = worm.GetUp(i);
				GizmoDrawCircle(forward, up, worm.GetPosition(i), config.wormRadius, 20);
			}
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(worm.GetPositionNormalized(t), 0.1f);
		}
	}
}
