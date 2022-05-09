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

		void Update()
		{
			int countX = Mathf.FloorToInt(transform.localScale.x / normalizedProcPlaneSize);
			int countZ = Mathf.FloorToInt(transform.localScale.z / normalizedProcPlaneSize);
			int count = countX * countZ;
			if (transform.childCount > 0 && transform.childCount != count)
			{
				Transform[] childs = new Transform[transform.childCount];
				for (int i= 0; i < transform.childCount; ++i)
				{
					childs[i] = transform.GetChild(i);
				}
				for (int i = 0; i < transform.childCount; ++i)
				{
					childs[i].gameObject.SafeDestroy();
				}
				planes = new ProcPlaneBehaviour[0];
				return;
			}
			if (planes.Length != count)
			{
				
				for (int i = 0; i < planes.Length; ++i)
				{
					planes[i].SafeDestroy();
				}
				planes = new ProcPlaneBehaviour[count];
				for (int i = 0; i < count; ++i)
				{
					ProcPlaneCreateParameters createParams = default;
					createParams.materialName = "White";
					createParams.name = $"plane[{i}]";
					createParams.parent = transform;
					ProcPlaneBehaviour procPlane = ProcPlaneBehaviour.Create(createParams);
					IVertexModifier vm = procPlane.gameObject.AddComponent<WorldPerlinVertexModifierBehaviour>();
					procPlane.VertexModifier = vm;					
				}
			}
		}
	}
}