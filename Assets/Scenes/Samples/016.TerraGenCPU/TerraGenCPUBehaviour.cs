using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _016_TerraGenCPU
{
	[ExecuteInEditMode]
	public class TerraGenCPUBehaviour : MonoBehaviour
	{
		[Range(1, 100)] public int PlaneCount = 1;
		public Vector2 size = Vector2.one;

		[Range(0, 7)] public int lod = 0;

		private float NormalizedProcPlaneSize => 1 / (float)PlaneCount;

		[HideInInspector] [SerializeField] private Transform container;

		private bool destroyOnNextUpdate = false;
		private Vector3 lastPosition = Vector3.zero;

		void Update()
		{
			bool destroyBecausePositionChanged = Vector3.Distance(lastPosition, transform.position) > Mathf.Epsilon;

			if (destroyOnNextUpdate || destroyBecausePositionChanged)
			{
				DestroyContainer();
				destroyOnNextUpdate = false;
			}			

			int countX = Mathf.FloorToInt(size.x / NormalizedProcPlaneSize);
			int countZ = Mathf.FloorToInt(size.y / NormalizedProcPlaneSize);
			int count = countX * countZ;
			if (!container || (container && container.childCount != count))
			{
				DestroyContainer();
				Populate();
			}

			lastPosition = transform.position;
		}

		void CreateContainer()
		{
			if (container)
				return;

			var obj = new GameObject("Container");
			container = obj.transform;
			container.SetParent(transform);
			container.localPosition = Vector3.zero;
		}

		void DestroyContainer()
		{
			if (!container)
				return;

			container.gameObject.SafeDestroy();
			container = null;
		}

		void Populate()
		{
			CreateContainer();

			int countX = Mathf.FloorToInt(size.x / NormalizedProcPlaneSize);
			int countZ = Mathf.FloorToInt(size.y / NormalizedProcPlaneSize);
			int count = countX * countZ;
			for (int i = 0; i < count; ++i)
			{
				Vector2Int posXY = Utils.GetXYFromIndex(i, countX);

				ProcPlaneCreateParameters createParams = default;
				createParams.materialName = "White";
				createParams.name = $"plane[{posXY.x},{posXY.y}]";
				createParams.parent = container;
				createParams.lodInfo = new MeshLodInfo();
				createParams.lodInfo.leftLod = createParams.lodInfo.rightLod = createParams.lodInfo.frontLod = createParams.lodInfo.backLod = -1;
				ProcPlaneBehaviour procPlane = ProcPlaneBehaviour.Create(createParams);
				IVertexModifier vm = procPlane.gameObject.AddComponent<WorldPerlinVertexModifierBehaviour>();
				float xSize = size.x / countX;
				float zSize = size.y / countZ;
				(vm as VertexModifierBehaviourBase).XSize = xSize;
				(vm as VertexModifierBehaviourBase).ZSize = zSize;
				(vm as VertexModifierBehaviourBase).Lod = lod;
				procPlane.VertexModifier = vm;

				Vector3 start = new Vector3(-size.x * 0.5f, 0, -size.y * 0.5f);
				procPlane.transform.localPosition = start + (new Vector3(posXY.x * xSize, 0, posXY.y * zSize)) + (new Vector3(xSize, 0, zSize) * 0.5f);
			}
		}

		[ContextMenu("Rebuild")]
		void Rebuild()
		{
			DestroyContainer();
		}

		private void OnValidate()
		{
			destroyOnNextUpdate = true;
		}
	}
}