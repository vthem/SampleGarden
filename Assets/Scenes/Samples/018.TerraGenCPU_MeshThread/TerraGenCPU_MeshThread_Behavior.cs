using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;
using Unity.Collections;
using UnityEngine.Rendering;
using System;
using Unity.Jobs;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _018_TerraGenCPU_MeshThread
{
	public class TerraGenCPU_MeshThread_Behavior : MonoBehaviour
	{

		public Vector2Int count = Vector2Int.one;
		public Vector2 size = Vector2.one;
		public Texture2D heightMap;
		public float heightScale = 1f;

		[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
		struct VertexData
		{
			public Vector3 pos;
			public Vector2 uv;
		}

		private NativeArray<VertexData> vertices;
		private NativeArray<uint> indices;

		private VertexAttributeDescriptor[] vertexLayout = new[]
		{
			new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
			new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
		};


		//private struct CreateMeshJob : IJobParallelFor
		//{
		//	public NativeArray<VertexData> vertices;
		//	public NativeArray<uint> indices;
		//	[ReadOnly] public NativeArray<Color32> heightMapData;
		//	public Vector2Int pixelCount;
		//	public Vector2Int vertexCount;

		//	public void Execute(int index)
		//	{
		//		Vector2Int pixelXY = Utils.GetXYFromIndex(index, vertexCount.x);



		//	}
		//}

		private static VertexData ComputeVertexData(int i, Vector2Int verticeCount, Vector2 size, float heightScale, NativeArray<Color32> heightMapData, Vector2Int pixelCount)
		{
			Vector2Int idx = Utils.GetXYFromIndex(i, verticeCount.x);
			Vector2 fCount = verticeCount;
			Vector2 uv = idx / fCount;


			Color32 color = Utils.SampleColorFromNativeArray(uv, heightMapData, pixelCount);
			Vector2 pos = uv * size;
			Vector3 pos3 = new Vector3(pos.x, color.r * heightScale, pos.y);
			VertexData vd = new VertexData();
			vd.pos = pos3;
			vd.uv = uv;
			return vd;
		}

		public void CreateMesh()
		{
			Stopwatch sw = Stopwatch.StartNew();

			var verticesCount = count.x * count.y;
			var triCount = (count.x - 1) * (count.y - 1) * 2 * 3;

			vertices = new NativeArray<VertexData>(verticesCount, Allocator.Persistent);
			indices = new NativeArray<uint>(triCount, Allocator.Persistent);

			NativeArray<Color32> heightData = heightMap.GetRawTextureData<Color32>();
			Vector2Int textureSize = new Vector2Int(heightMap.width, heightMap.height);
			var max = Utils.GetArrayIdxClamp(textureSize - Vector2Int.one, textureSize);
			if (max >= heightData.Length)
			{
				Debug.Log($"Invalid texture size {max} {heightData.Length}");
				return;
			}

			int tIdx = 0;
			for (int i = 0; i < vertices.Length; ++i)
			{
				Vector2Int idx = Utils.GetXYFromIndex(i, count.x);
				Vector2 fCount = count;
				Vector2 uv = idx / fCount;
				Vector2 pos = uv * size;

				Color32 color = heightMap.GetPixelBilinear(uv.x, uv.y);
				Vector3 pos3 = new Vector3(pos.x, color.r * heightScale, pos.y);
				VertexData vd = new VertexData();
				vd.pos = pos3;
				vd.uv = uv;
				vertices[i] = vd;

				color = Utils.SampleColorFromNativeArray(uv, heightData, textureSize);
				pos3 = new Vector3(pos.x, color.r * heightScale, pos.y);
				vd = new VertexData();
				vd.pos = pos3;
				vd.uv = uv;
				vertices[i] = vd;

				if (tIdx < indices.Length)
				{
					uint p1 = (uint)i;
					uint p2 = ((uint)i + 1);
					uint p3 = ((uint)i + (uint)count.x);
					uint p4 = ((uint)i + (uint)count.x + 1);
					indices[tIdx++] = p1;
					indices[tIdx++] = p3;
					indices[tIdx++] = p2;
					indices[tIdx++] = p2;
					indices[tIdx++] = p3;
					indices[tIdx++] = p4;
				}
			}

			//for (uint y = 0; y < count.y - 1; ++y)
			//{
			//	for (uint x = 0; x < count.x - 1; ++x)
			//	{
			//		uint vi = y * (uint)(count.x) + x;
			//		uint p1 = vi;
			//		uint p2 = (vi + 1);
			//		uint p3 = (vi + (uint)count.x);
			//		uint p4 = (vi + (uint)count.x + 1);
			//		indices[tIdx++] = p1;
			//		indices[tIdx++] = p3;
			//		indices[tIdx++] = p2;
			//		indices[tIdx++] = p2;
			//		indices[tIdx++] = p3;
			//		indices[tIdx++] = p4;
			//	}
			//}

			MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();

			Mesh mesh = new Mesh();
			mesh.indexFormat = IndexFormat.UInt32;

			// specify vertex count and layout
			mesh.SetVertexBufferParams(verticesCount, vertexLayout);
			mesh.SetVertexBufferData(vertices, 0, 0, verticesCount);
			mesh.SetIndexBufferParams(triCount, IndexFormat.UInt32);
			mesh.SetIndexBufferData(indices, 0, 0, triCount);
			mesh.subMeshCount = 1;
			mesh.SetSubMesh(0, new SubMeshDescriptor(0, triCount, MeshTopology.Triangles));
			mesh.RecalculateBounds();

			meshFilter.mesh = mesh;

			Debug.Log($"creation time:{sw.ElapsedMilliseconds}ms");
		}

		private void OnDestroy()
		{
			if (vertices.IsCreated)
			{
				vertices.Dispose();
			}
			if (indices.IsCreated)
			{
				indices.Dispose();
			}
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(TerraGenCPU_MeshThread_Behavior))]
	public class TerraGenCPU_MeshGen_Editor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			if (GUILayout.Button("Create"))
			{
				var mono = target as TerraGenCPU_MeshThread_Behavior;
				mono.CreateMesh();
			}
		}
	}
#endif

}

