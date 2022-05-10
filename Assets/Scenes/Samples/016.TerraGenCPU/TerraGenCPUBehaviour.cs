using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _016_TerraGenCPU
{
	[ExecuteInEditMode]
	public class TerraGenCPUBehaviour : MonoBehaviour
	{
		[Range(0.01f, 1f)] public float normalizedProcPlaneSize = .5f;

		[SerializeField] private Transform container;

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
				return;
			}
			if (transform.childCount != count)
			{
				for (int i = 0; i < count; ++i)
				{
					ProcPlaneCreateParameters createParams = default;
					createParams.materialName = "White";
					createParams.name = $"plane[{i}]";
					createParams.parent = transform;
					ProcPlaneBehaviour procPlane = ProcPlaneBehaviour.Create(createParams);
					IVertexModifier vm = procPlane.gameObject.AddComponent<WorldPerlinVertexModifierBehaviour>();
					procPlane.VertexModifier = vm;

					Vector2Int posXY = Utils.GetXYFromIndex(i, countX);
					procPlane.transform.localPosition = new Vector3(posXY.x, 0, posXY.y);
				}
			}
		}

		void CreateContainer()
		{
			if (container)
				return;

			var obj = new GameObject();
			container = obj.transform;
			container.SetParent(transform);
			container.localPosition = Vector3.zero;
		}

		void DestroyContainer()
		{
			if (!container)
				return;

			container.SafeDestroy();
		}

		void Populate()
		{
			CreateContainer();

			int countX = Mathf.FloorToInt(transform.localScale.x / normalizedProcPlaneSize);
			int countZ = Mathf.FloorToInt(transform.localScale.z / normalizedProcPlaneSize);
			int count = countX * countZ;
			for (int i = 0; i < count; ++i)
			{
				ProcPlaneCreateParameters createParams = default;
				createParams.materialName = "White";
				createParams.name = $"plane[{i}]";
				createParams.parent = transform;
				ProcPlaneBehaviour procPlane = ProcPlaneBehaviour.Create(createParams);
				IVertexModifier vm = procPlane.gameObject.AddComponent<WorldPerlinVertexModifierBehaviour>();
				procPlane.VertexModifier = vm;

				Vector2Int posXY = Utils.GetXYFromIndex(i, countX);
				procPlane.transform.localPosition = new Vector3(posXY.x, 0, posXY.y);
			}
		}
	}
}