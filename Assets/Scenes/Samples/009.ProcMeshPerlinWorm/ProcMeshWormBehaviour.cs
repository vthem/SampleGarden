using UnityEngine;

#if UNITY_EDITOR
#endif


public class WormVertexModifier : VertexModifierBase
{
	public Matrix4x4 localToWorld;
	public PerlinWorm worm;
	public int index = 0;
	public int normalRounding = 100;

	public override Vector3 Vertex(int x, int z)
	{
		int vCount = VertexCount1D - 1;
		float angle = Mathf.PI * 2 * x / vCount;

		Vector3 pOnPath = worm.GetPosition(index + z);
		Vector3 tan = worm.GetForward(index + z);
		Quaternion q = Quaternion.FromToRotation(Vector3.forward, tan);
		float radius = worm.config.wormRadius;
		Vector3 pOnCircle = q * new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
		return pOnPath + pOnCircle;
	}

	public override Vector3 Normal(int x, int z)
	{
		int vCount = VertexCount1D - 1;
		float angle = Mathf.PI * 2 * x / vCount;

		//Vector3 pOnPath = worm.GetPosition(index + z);
		Vector3 tan = worm.GetForward(index + z);
		Quaternion q = Quaternion.FromToRotation(Vector3.forward, tan);
		float radius = worm.config.wormRadius;
		Vector3 pOnCircle = q * new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
		//return -pOnCircle;
		return Round(-pOnCircle);
		//Vector3 norm = (pOnCircle - pOnPath).normalized;
		//return norm;
		//return new Vector3(Mathf.Round(norm.x * normalRounding) / normalRounding, Mathf.Round(norm.y * normalRounding) / normalRounding, Mathf.Round(norm.z * normalRounding) / normalRounding);
		//return normal;
	}

	Vector3 Round(Vector3 v)
	{
		return new Vector3(Mathf.Round(v.x * normalRounding) / normalRounding, Mathf.Round(v.y * normalRounding) / normalRounding, Mathf.Round(v.z * normalRounding) / normalRounding);
	}
}

public class ProcMeshWormBehaviour : MonoBehaviour
{
	public PerlinWormConfig wormConfig;
	public string meshMaterialName = "Grid";
	[Range(0, 7)] public int lod = 1;

	public int normalRounding = 100;
	public bool enableDrawGizmos = false;


	private ProcPlaneBehaviour[] procPlanes;
	private PerlinWorm perlinWorm = null;
	private bool initialized = false;

	public Transform camera;
	public Transform light;


	private void Initialize()
	{
		if (initialized)
		{
			return;
		}

		procPlanes = new ProcPlaneBehaviour[1];
		perlinWorm = new PerlinWorm();
		perlinWorm.config = wormConfig;
		perlinWorm.Update();
		VertexModifierBase vm = new WormVertexModifier
		{
			Lod = lod,
			worm = perlinWorm,
			normalRounding = normalRounding
		};
		ProcPlaneCreateParameters createInfo = new ProcPlaneCreateParameters(
			name: "Worm",
			materialName: meshMaterialName,
			vertexModifier: vm
		);
		createInfo.recalculateNormals = false;
		ProcPlaneBehaviour procPlane = ProcPlaneBehaviour.Create(createInfo);
		procPlanes[0] = procPlane;

		initialized = true;
	}

	private void Update()
	{
		Initialize();

		perlinWorm.Update();

		for (int i = 0; i < procPlanes.Length; ++i)
		{
			WormVertexModifier vm = procPlanes[i].GetVertexModifierAs<WormVertexModifier>();
			vm.Lod = lod;
			vm.HasChanged = true;
			vm.normalRounding = normalRounding;
		}

		camera.position = perlinWorm.GetPosition(0);
		var lookatPoint = camera.position + perlinWorm.GetForward(0) * 10f;
		camera.LookAt(lookatPoint, Vector3.up);
		light.position = perlinWorm.GetPosition(0);
	}

	private void OnDrawGizmosSelected()
	{
		if (!enableDrawGizmos)
			return;

		if (null == perlinWorm)
		{
			perlinWorm = new PerlinWorm();
			perlinWorm.config = wormConfig;
		}
		perlinWorm.Update();
		perlinWorm.DrawGizmos();
	}
}