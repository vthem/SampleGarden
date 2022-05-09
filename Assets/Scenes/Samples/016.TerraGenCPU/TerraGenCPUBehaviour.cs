using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _016_TerraGenCPU
{
	[ExecuteInEditMode]
	public class TerraGenCPUBehaviour : MonoBehaviour
	{
		[Range(0.01f, 1f)] public float normalizedProcPlaneSize = .5f;

		private ProcPlaneBehaviour[] planes = new ProcPlaneBehaviour[0];
		// Start is called before the first frame update
		void Start()
		{

		}

		// Update is called once per frame
		void Update()
		{
			int countX = Mathf.FloorToInt(transform.localScale.x / normalizedProcPlaneSize);
			int countY = Mathf.FloorToInt(transform.localScale.y / normalizedProcPlaneSize);
			int count = countX * countY;
			if (planes.Length != count)
			{
				for (int i = 0; i < planes.Length; ++i)
				{
					planes[i].SafeDestroy();
				}
				planes = new ProcPlaneBehaviour[count];
				for (int i = 0; i < planes.Length; ++i)
				{
					ProcPlaneCreateParameters createParams = default;
					createParams.materialName = "White";
					createParams.name = $"plane[{i}]";
					createParams.parent = transform.parent;

					ProcPlaneBehaviour.Create(createParams);
				}
			}
		}
	}
}