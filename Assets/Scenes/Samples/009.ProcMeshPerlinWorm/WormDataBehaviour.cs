using UnityEngine;

namespace _009_ProcMeshPerlinWorm
{
	public class WormDataBehaviour : MonoBehaviour
	{
		public PerlinWorm perlinWorm;
		public bool enableDrawGizmos = false;

		public static PerlinWorm GetPerlinWorm()
		{
			PerlinWorm w = FindObjectOfType<WormDataBehaviour>().perlinWorm;
			if (w == null)
			{
				Debug.Log("No GameObject with WormDataBehaviour found on scene");
			}
			return w;
		}

		private void Awake()
		{
			perlinWorm.Update();
		}

		private void Update()
		{
			perlinWorm.Update();
		}

		private void OnDrawGizmosSelected()
		{
			if (!enableDrawGizmos)
			{
				return;
			}

			perlinWorm.Update();
			perlinWorm.DrawGizmos();
		}
	}
}