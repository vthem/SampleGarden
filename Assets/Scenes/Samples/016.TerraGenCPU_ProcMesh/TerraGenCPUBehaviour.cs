using UnityEngine;


using Stopwatch = System.Diagnostics.Stopwatch;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _016_TerraGenCPU_ProcMesh
{
	[ExecuteInEditMode]
	public class TerraGenCPUBehaviour : MonoBehaviour
	{
		[Range(1, 100)] public int PlaneCount = 1;
		public Vector2 size = Vector2.one;

		[Range(0, 7)] public int maxLOD = 0;

		public Vector3 perlinOffset;
		public float perlinScale = 1f;

		public AnimationCurve lodVsDistance;

		public Vector3 maxLODPosition = Vector3.zero;

		private float NormalizedProcPlaneSize => 1 / (float)PlaneCount;

		[HideInInspector] [SerializeField] private Transform container;

		private bool destroyOnNextUpdate = false;
		private Vector3 lastPosition = Vector3.zero;

		private ProcPlaneBehaviour[] procPlaneArray = new ProcPlaneBehaviour[0];

		private void Update()
		{			
			if (destroyOnNextUpdate)
			{
				DestroyContainer();
				destroyOnNextUpdate = false;
			}

			int countX = Mathf.FloorToInt(size.x / NormalizedProcPlaneSize);
			int countZ = Mathf.FloorToInt(size.y / NormalizedProcPlaneSize);
			int count = countX * countZ;
			if (!container || (container && container.childCount != count))
			{
				Debug.Log("rebuild!");
				DestroyContainer();
				Populate();
			}

			bool positionChanged = Vector3.Distance(lastPosition, transform.position) > Mathf.Epsilon;

			UpdateMeshes(positionChanged);

			lastPosition = transform.position;
		}

		private void CreateContainer()
		{
			if (container)
			{
				return;
			}

			GameObject obj = new GameObject("Container");
			container = obj.transform;
			container.SetParent(transform);
			container.localPosition = Vector3.zero;
		}

		private void DestroyContainer()
		{
			if (!container)
			{
				return;
			}

			container.gameObject.SafeDestroy();
			container = null;
		}

		private struct BaseInfo
		{
			public Vector3 localPosition;
			public int lod;
		};

		private void Populate()
		{
			Stopwatch sw = Stopwatch.StartNew();
			CreateContainer();

			int countX = Mathf.FloorToInt(size.x / NormalizedProcPlaneSize);
			int countZ = Mathf.FloorToInt(size.y / NormalizedProcPlaneSize);
			int count = countX * countZ;

			BaseInfo[] baseInfoArray = new BaseInfo[count];
			for (int i = 0; i < count; ++i)
			{
				Vector2Int posXY = Utils.GetXYFromIndex(i, countX);
				float xSize = size.x / countX;
				float zSize = size.y / countZ;

				BaseInfo info = default;
				Vector3 start = new Vector3(-size.x * 0.5f, 0, -size.y * 0.5f);
				info.localPosition = start + (new Vector3(posXY.x * xSize, 0, posXY.y * zSize)) + (new Vector3(xSize, 0, zSize) * 0.5f);
				info.lod = GetLOD(info.localPosition);
				baseInfoArray[i] = info;
			}

			procPlaneArray = new ProcPlaneBehaviour[count]; // cache the proc plane
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
				(vm as VertexModifierBehaviourBase).Lod = baseInfoArray[i].lod;
				(vm as WorldPerlinVertexModifierBehaviour).PerlinOffset = perlinOffset;
				(vm as WorldPerlinVertexModifierBehaviour).PerlinScale = perlinScale;
				procPlane.VertexModifier = vm;

				procPlane.transform.localPosition = baseInfoArray[i].localPosition;

				procPlane.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

				procPlaneArray[i] = procPlane;
			}

			Debug.Log($"Populate time:{sw.ElapsedMilliseconds}ms");
		}

		private int GetLOD(Vector3 localPosition)
		{
			float distance = (transform.TransformPoint(localPosition) - maxLODPosition).magnitude;
			distance /= size.x; // normalize
			distance = Mathf.Clamp01(distance);

			float lodSample = lodVsDistance.Evaluate(distance);
			lodSample = Mathf.Clamp01(lodSample);
			lodSample = Mathf.Clamp01(lodSample);
			return Mathf.RoundToInt(lodSample * maxLOD);
		}

		private void UpdateMeshes(bool forceUpdate)
		{
			if (!container)
			{
				return;
			}

			int countX = Mathf.FloorToInt(size.x / NormalizedProcPlaneSize);
			int countZ = Mathf.FloorToInt(size.y / NormalizedProcPlaneSize);
			int count = countX * countZ;

			ProcPlaneBehaviour.ClearElapsed();

			// first pass, update the lod, perlin offset, size
			for (int i = 0; i < procPlaneArray.Length; ++i)
			{
				ProcPlaneBehaviour procPlane = procPlaneArray[i];
				IVertexModifier vm = procPlane.gameObject.GetComponent<WorldPerlinVertexModifierBehaviour>();

				float xSize = size.x / countX;
				float zSize = size.y / countZ;
				(vm as VertexModifierBehaviourBase).XSize = xSize;
				(vm as VertexModifierBehaviourBase).ZSize = zSize;
				(vm as VertexModifierBehaviourBase).Lod = GetLOD(procPlane.transform.localPosition);
				(vm as WorldPerlinVertexModifierBehaviour).PerlinOffset = perlinOffset;
				(vm as WorldPerlinVertexModifierBehaviour).PerlinScale = perlinScale;
				
				vm.RequireUpdate |= forceUpdate;
			}

			// second pass: update the lod of the neighbor
			for (int i = 0; i < procPlaneArray.Length; ++i)
			{

			}
		}

		[ContextMenu("Rebuild")]
		private void Rebuild()
		{
			DestroyContainer();
		}

		[ContextMenu("Force ProcMesh Update")]
		private void ForceUpdateMeshes()
		{
			for (int i = 0; i < procPlaneArray.Length; ++i)
			{
				ProcPlaneBehaviour procPlane = procPlaneArray[i];
				IVertexModifier vm = procPlane.gameObject.GetComponent<WorldPerlinVertexModifierBehaviour>();
				vm.RequireUpdate = true;
			}
		}
	}

#if UNITY_EDITOR
	// A tiny custom editor for ExampleScript component
	[CustomEditor(typeof(TerraGenCPUBehaviour))]
	public class TerraGenCPUEditor : Editor
	{
		//public void OnSceneGUI()
		//{
		//	TerraGenCPUBehaviour terraGen = target as TerraGenCPUBehaviour;

		//	float size = HandleUtility.GetHandleSize(terraGen.maxLODPosition) * 0.5f;
		//	Vector3 snap = Vector3.one * 0.5f;

		//	EditorGUI.BeginChangeCheck();
		//	Vector3 newTargetPosition = Handles.FreeMoveHandle(terraGen.maxLODPosition, terraGen.transform.rotation, size, snap, Handles.DotHandleCap);
		//	newTargetPosition.y = 0f;
		//	if (EditorGUI.EndChangeCheck())
		//	{
		//		Undo.RecordObject(terraGen, "Change Look At Target Position");
		//		terraGen.maxLODPosition = newTargetPosition;
		//	}
		//}
	}
#endif
}