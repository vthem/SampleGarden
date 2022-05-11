using UnityEngine;


namespace _009_ProcMeshPerlinWorm
{

	public class WormVertexModifier : VertexModifierBase
	{
		public Matrix4x4 localToWorld;
		public PerlinWorm worm;
		public int index = 0;
		public int normalRounding = 100;
		public int vertexCount1D = 0;

		public override Vector3 Vertex(int x, int z)
		{
			int vCount = vertexCount1D - 1;
			float angle = Mathf.PI * 2 * x / vCount;

			Vector3 pOnPath = worm.GetPosition(index * vertexCount1D + z);
			Vector3 tan = worm.GetForward(index * vertexCount1D + z);
			Quaternion q = Quaternion.FromToRotation(Vector3.forward, tan);
			float radius = worm.config.wormRadius;
			Vector3 pOnCircle = q * new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
			return pOnPath + pOnCircle;
		}

		public override Vector3 Normal(int x, int z)
		{
			int vCount = vertexCount1D - 1;
			float angle = Mathf.PI * 2 * x / vCount;

			//Vector3 pOnPath = worm.GetPosition(index + z);
			Vector3 tan = worm.GetForward(index * vertexCount1D + z);
			Quaternion q = Quaternion.FromToRotation(Vector3.forward, tan);
			float radius = worm.config.wormRadius;
			Vector3 pOnCircle = q * new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
			return -pOnCircle;
		}

		private Vector3 Round(Vector3 v)
		{
			return new Vector3(Mathf.Round(v.x * normalRounding) / normalRounding, Mathf.Round(v.y * normalRounding) / normalRounding, Mathf.Round(v.z * normalRounding) / normalRounding);
		}
	}

	public class WormMeshGeneratorBehaviour : MonoBehaviour
	{

		public string meshMaterialName = "Grid";
		[Range(0, 7)] public int lod = 1;

		public int normalRounding = 100;

		[Range(0, 15)] public int zPlaneCount = 2;

		private ProcPlaneBehaviour[] procPlanes;

		private bool initialized = false;


		private PerlinWorm perlinWorm;

		private void Initialize()
		{
			if (initialized)
			{
				return;
			}


			perlinWorm = FindObjectOfType<WormDataBehaviour>().perlinWorm;
			perlinWorm.config.length = VertexModifier.ComputeVertexCount1D(lod) * zPlaneCount;
			perlinWorm.Update();

			Debug.Log($"perlin count:{perlinWorm.Count} vm:{VertexModifier.ComputeVertexCount1D(lod) * zPlaneCount}");

			procPlanes = new ProcPlaneBehaviour[zPlaneCount];
			for (int planeIndex = 0; planeIndex < zPlaneCount; ++planeIndex)
			{

				VertexModifierBase vm = new WormVertexModifier
				{
					Lod = lod,
					worm = perlinWorm,
					normalRounding = normalRounding,
					index = planeIndex,
					vertexCount1D = VertexModifier.ComputeVertexCount1D(lod)
				};
				ProcPlaneCreateParameters createInfo = new ProcPlaneCreateParameters(
					name: $"Worm{planeIndex}",
					materialName: meshMaterialName,
					vertexModifier: vm
				)
				{
					recalculateNormals = false
				};
				ProcPlaneBehaviour procPlane = ProcPlaneBehaviour.Create(createInfo);
				procPlanes[planeIndex] = procPlane;
			}

			initialized = true;
		}

		private void Update()
		{
			Initialize();

			for (int i = 0; i < procPlanes.Length; ++i)
			{
				WormVertexModifier vm = procPlanes[i].GetVertexModifierAs<WormVertexModifier>();
				vm.Lod = lod;
				vm.RequireRebuild = true;
				vm.normalRounding = normalRounding;
			}
		}
	}
}