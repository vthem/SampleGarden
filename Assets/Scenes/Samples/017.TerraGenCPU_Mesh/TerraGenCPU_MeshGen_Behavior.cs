using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _017_TerraGenCPU_Mesh
{
	public class TerraGenCPU_MeshGen_Behavior : MonoBehaviour
	{
		public Vector2Int count = Vector2Int.one;
		public Vector2 size = Vector2.one;
		public Texture2D heightMap;

		public void CreateMesh()
		{
			Stopwatch sw = Stopwatch.StartNew();

			MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();

			Mesh mesh = new Mesh();

			mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			var verticesCount = count.x * count.y;

			Vector3[] vertArray = new Vector3[verticesCount];

			
			for (int i = 0; i < vertArray.Length; ++i)
			{
				Vector2Int idx = Utils.GetXYFromIndex(i, count.x);
				Vector2 fCount = count;
				Vector2 uv = idx / fCount;
				Vector2 pos = uv * size;

				var color = heightMap.GetPixelBilinear(uv.x, uv.y);
				Vector3 pos3 = new Vector3(pos.x, color.r, pos.y);
				vertArray[i] = pos3;
			}
			mesh.vertices = vertArray;


			var triCount = (count.x - 1) * (count.y - 1) * 2 * 3;

			int[] triArray = new int[triCount];
			int tIdx = 0;
			for (int y = 0; y < count.y - 1; ++y)
			{
				for (int x = 0; x < count.x - 1; ++x)
				{
					var vi = y * (count.x) + x;
					int p1 = vi;
					int p2 = (vi + 1);
					int p3 = (vi + count.x);
					int p4 = (vi + count.x + 1);
					triArray[tIdx++] = p1;
					triArray[tIdx++] = p3;
					triArray[tIdx++] = p2;
					triArray[tIdx++] = p2;
					triArray[tIdx++] = p3;
					triArray[tIdx++] = p4;
				}
			}

			mesh.triangles = triArray;

			Vector3[] normalArray = new Vector3[verticesCount];
			for (int i = 0; i < normalArray.Length; ++i)
			{
				normalArray[i] = transform.up;
			}

			mesh.normals = normalArray;

			Vector2[] uvArray = new Vector2[verticesCount];
			for (int i = 0; i < uvArray.Length; ++i)
			{
				Vector2Int idx = Utils.GetXYFromIndex(i, count.x);
				Vector2 fCount = count;
				Vector2 uv = idx / fCount;
				uvArray[i] = uv;
			}
			mesh.uv = uvArray;

			meshFilter.mesh = mesh;

			Debug.Log($"creation time:{sw.ElapsedMilliseconds}ms");
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(TerraGenCPU_MeshGen_Behavior))]
	public class TerraGenCPU_MeshGen_Editor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			
			if (GUILayout.Button("Create"))
			{
				var mono = target as TerraGenCPU_MeshGen_Behavior;
				mono.CreateMesh();
			}
		}
	}
#endif

}

