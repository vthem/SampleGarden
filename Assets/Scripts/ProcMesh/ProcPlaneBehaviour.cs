using System;

using Unity.Collections;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

#endif
using UnityEngine;
using UnityEngine.SceneManagement;

using SysStopwatch = System.Diagnostics.Stopwatch;

public struct ProcPlaneCreateParameters
{
	public string name;
	public string materialName;
	public Transform parent;
	public MeshLodInfo lodInfo;
	public IVertexModifier vertexModifier;
	public bool recalculateNormals;

	public ProcPlaneCreateParameters(string name,
									 string materialName,
									 IVertexModifier vertexModifier)
	{
		this.name = name;
		this.materialName = materialName;
		this.vertexModifier = vertexModifier;
		parent = null;
		lodInfo.leftLod = lodInfo.rightLod = lodInfo.frontLod = lodInfo.backLod = -1;
		recalculateNormals = true;
	}
}

public interface IVertexModifierGetter
{
	IVertexModifier VertexModifier { get; }
}

[ExecuteInEditMode]
public class ProcPlaneBehaviour : MonoBehaviour
{
	public bool recalculateNormals = true;

	public static ProcPlaneBehaviour Create(ProcPlaneCreateParameters createParams)
	{
		GameObject obj = new GameObject(createParams.name);
		if (createParams.parent)
		{
			obj.transform.SetParent(createParams.parent);
		}

		MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
		MeshRenderer meshRenderer = obj.AddComponent<MeshRenderer>();
		obj.name = createParams.name;

		Mesh mesh = meshFilter.sharedMesh;
		if (!mesh)
		{
			mesh = new Mesh();
			meshFilter.sharedMesh = mesh;
		}
		mesh.MarkDynamic();

		ProcPlaneBehaviour procPlane = obj.AddComponent<ProcPlaneBehaviour>();
		procPlane.lodInfo = createParams.lodInfo;
		procPlane.vertexModifier = createParams.vertexModifier;
		procPlane.recalculateNormals = createParams.recalculateNormals;
		procPlane.vertexModifier = createParams.vertexModifier;

		meshRenderer.sharedMaterial = Resources.Load(createParams.materialName) as Material;
		return procPlane;
	}

	public int LeftLod { get => lodInfo.leftLod; set => lodInfo.leftLod = value; }
	public int FrontLod { get => lodInfo.frontLod; set => lodInfo.frontLod = value; }
	public int RightLod { get => lodInfo.rightLod; set => lodInfo.rightLod = value; }
	public int BackLod { get => lodInfo.backLod; set => lodInfo.backLod = value; }
	public IVertexModifier VertexModifier { get => vertexModifier; set => vertexModifier = value; }

	public T GetVertexModifierAs<T>() where T : class
	{
		return vertexModifier as T;
	}

	#region private
	[SerializeField]
	private MeshLodInfo lodInfo;

	[SerializeField]
	private readonly bool forceRebuild = false;

	private IVertexModifier vertexModifier;
	private MeshGenerateParameter meshGenerateParameter;
	private static TimeSpan frameElapsed = TimeSpan.Zero;

	private Matrix4x4 objToParent;
	private Material material;
	private bool forceRebuildOnce = false;

	public static void ClearElapsed()
	{
		frameElapsed = TimeSpan.Zero;
	}

	public static double GetBenchmarkResult()
	{
		return frameElapsed.TotalMilliseconds;
	}

	private void Update()
	{
		if (!material)
		{
			material = GetComponent<MeshRenderer>().sharedMaterial;
		}

		if (null == vertexModifier)
		{
			IVertexModifierGetter getter = GetComponent<IVertexModifierGetter>();
			if (getter != null)
			{
				vertexModifier = getter.VertexModifier;
			}
		}
		if (null == vertexModifier)
		{
			return;
		}

		if (!IsMeshInfoValid() || forceRebuild || (vertexModifier.RequireRebuild) || forceRebuildOnce)
		{
			bool enableFrameThrottling = true;
#if UNITY_EDITOR
			enableFrameThrottling = !EditorApplication.isPlaying;
#endif

			//if (enableFrameThrottling && frameElapsed.TotalMilliseconds > 5)
			//{
			//	return;
			//}

			forceRebuildOnce = false;
			vertexModifier.RequireRebuild = false;
			vertexModifier.RequireUpdate = true;


			ReleaseMeshInfo();
			AllocateMeshInfo();
		}

		if (vertexModifier.RequireUpdate)
		{
			objToParent = Matrix4x4.TRS(transform.localPosition, Quaternion.identity, Vector3.one);
			material.SetMatrix("_ObjToParent", objToParent);
			meshGenerateParameter.recalculateNormals = recalculateNormals;

			SysStopwatch sw = SysStopwatch.StartNew();
			ProceduralPlaneMesh.Generate(meshGenerateParameter);
			frameElapsed += sw.Elapsed;

			vertexModifier.RequireUpdate = false;
		}
	}

	private void LateUpdate()
	{
		frameElapsed = TimeSpan.Zero;
	}

	private void AllocateMeshInfo()
	{
		Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
		if (!mesh)
		{
			mesh = new Mesh();
			GetComponent<MeshFilter>().sharedMesh = mesh;
		}
		mesh.MarkDynamic();
		meshGenerateParameter.mesh = mesh;
		meshGenerateParameter.vertexModifier = vertexModifier;
		meshGenerateParameter.lodInfo = lodInfo;

		meshGenerateParameter.vertices = new NativeArray<Vertex>(vertexModifier.VertexCount2D, Allocator.Persistent);
		meshGenerateParameter.indices = new NativeArray<ushort>(vertexModifier.IndiceCount, Allocator.Persistent);
	}

	private void ReleaseMeshInfo()
	{
		if (meshGenerateParameter.vertices.IsCreated)
		{
			meshGenerateParameter.vertices.Dispose();
		}
		if (meshGenerateParameter.indices.IsCreated)
		{
			meshGenerateParameter.indices.Dispose();
		}
	}

	private bool IsMeshInfoValid()
	{
		if (!meshGenerateParameter.mesh || !meshGenerateParameter.indices.IsCreated || !meshGenerateParameter.vertices.IsCreated)
		{
			return false;
		}

		return meshGenerateParameter.lodInfo == lodInfo;
	}

	private void OnDestroy()
	{
		ReleaseMeshInfo();
	}

	private void OnValidate()
	{
		forceRebuildOnce = true;
	}

	private void OnEnable()
	{
#if UNITY_EDITOR
		EditorSceneManager.sceneSaving += SceneSaving;
#endif
	}

	private void OnDisable()
	{
#if UNITY_EDITOR
		EditorSceneManager.sceneSaving -= SceneSaving;
#endif
		ReleaseMeshInfo();
	}

#if UNITY_EDITOR
	private void SceneSaving(Scene scene, string path)
	{
		Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
		if (mesh)
		{
			DestroyImmediate(mesh);
			GetComponent<MeshFilter>().sharedMesh = null;
		}
	}

	[MenuItem("GameObject/3D Object/TSW/ProcMesh/Create ProceduralPlane", false, 0)]
	private static void CreateCustomGameObject(MenuCommand menuCommand)
	{
		ProcPlaneCreateParameters createInfo = new ProcPlaneCreateParameters(
			name: "ProceduralPlane",
			materialName: "Grid",
			vertexModifier: null
		);
		ProcPlaneBehaviour procPlane = ProcPlaneBehaviour.Create(createInfo);
		// Ensure it gets reparented if this was a context click (otherwise does nothing)
		GameObjectUtility.SetParentAndAlign(procPlane.gameObject, menuCommand.context as GameObject);
		// Register the creation in the undo system
		Undo.RegisterCreatedObjectUndo(procPlane.gameObject, "Create " + procPlane.gameObject.name);
		Selection.activeObject = procPlane.gameObject;
	}
#endif
	#endregion // private
}
