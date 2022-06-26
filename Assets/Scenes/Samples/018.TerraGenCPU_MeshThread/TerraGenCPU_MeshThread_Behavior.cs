using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;
using Unity.Collections;
using UnityEngine.Rendering;
using System;
using Unity.Jobs;
using UnityEngine.UIElements;
using static UnityEngine.InputManagerEntry;
using Unity.Jobs.LowLevel.Unsafe;

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

		private VertexAttributeDescriptor[] vertexLayout = new[]
		{
			new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
			new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
		};


		private struct MeshJob : IJob
		{
			public NativeArray<VertexData> vertices;
			public NativeArray<uint> indices;
			[ReadOnly] public NativeArray<RGB> heightData;
			[ReadOnly] public Vector2Int count;
			[ReadOnly] public Vector2 size;
			[ReadOnly] public Vector2Int textureSize;
			[ReadOnly] public float heightScale;
			[ReadOnly] public int offset;

			public void Execute()
			{
				for (int vertexIndex = 0; vertexIndex < vertices.Length; ++vertexIndex)
				{
					Vector2Int globalIdx = Utils.GetXYFromIndex(vertexIndex + offset, count.x);
					Vector2 fCount = count;
					Vector2 uv = globalIdx / fCount;
					Vector2 pos = uv * size;

					var height = Utils.SampleColorFromNativeArray(uv, heightData, textureSize);
					Vector3 pos3 = new Vector3(pos.x, height.r / (255f) * heightScale, pos.y);
					VertexData vd = new VertexData();
					vd.pos = pos3;
					vd.uv = uv;
					vertices[vertexIndex] = vd;

					if ((vertexIndex * 6) < indices.Length)
					{
						uint p1 = (uint)vertexIndex;
						uint p2 = ((uint)vertexIndex + 1);
						uint p3 = ((uint)vertexIndex + (uint)count.x);
						uint p4 = ((uint)vertexIndex + (uint)count.x + 1);
						indices[(vertexIndex * 6) + 0] = p1;
						indices[(vertexIndex * 6) + 1] = p3;
						indices[(vertexIndex * 6) + 2] = p2;
						indices[(vertexIndex * 6) + 3] = p2;
						indices[(vertexIndex * 6) + 4] = p3;
						indices[(vertexIndex * 6) + 5] = p4;
					}
				}
			}
		}

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

		struct RGB {
			public byte r;
			public byte g;
			public byte b;
		}

		public void CreateMesh()
		{
			Stopwatch sw = Stopwatch.StartNew();

			var heightData = heightMap.GetRawTextureData<RGB>();
			Vector2Int textureSize = new Vector2Int(heightMap.width, heightMap.height);
			var max = Utils.GetArrayIdxClamp(textureSize - Vector2Int.one, textureSize);
			if (max != heightData.Length - 1)
			{
				Debug.Log($"Invalid texture size {max} {heightData.Length} {textureSize} {textureSize.x * textureSize.y} format:{heightMap.format}");
				return;
			}

			int jobsCount = JobsUtility.JobWorkerMaximumCount;

			var verticesCount = count.x * count.y;
			var indicesCount = (count.x - 1) * (count.y - 1) * 2 * 3;
			int verticesCountPartial = verticesCount / jobsCount;			
			int indicesCountPartial = indicesCount / jobsCount;

			Debug.Log($"jobsCount:{jobsCount} indicesCountPartial:{indicesCountPartial} verticesCountPartial:{verticesCountPartial}");

			MeshJob[] jobArray = new MeshJob[jobsCount];
			JobHandle[] jobHandleArray = new JobHandle[jobsCount];
			int offset = 0;
			for (int j = 0; j < jobsCount; ++j)
			{
				if (j == jobsCount-1)
				{
					verticesCountPartial += verticesCount % jobsCount;
					indicesCountPartial += indicesCount % jobsCount;
				}
				MeshJob job;
				job.vertices = new NativeArray<VertexData>(verticesCountPartial, Allocator.TempJob);;
				job.indices = new NativeArray<uint>(indicesCountPartial, Allocator.TempJob);
				job.heightData = heightData;
				job.count = count;
				job.size = size;
				job.textureSize = textureSize;
				job.heightScale = heightScale;
				job.offset = offset;
				jobHandleArray[j] = job.Schedule();

				jobArray[j] = job;

				offset += verticesCountPartial;
			}

			for (int j = 0; j < jobsCount; ++j)
			{
				jobHandleArray[j].Complete();
			}

			MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();

			Mesh mesh = new Mesh();
			mesh.indexFormat = IndexFormat.UInt32;

			// specify vertex count and layout
			var updateFlags = MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontValidateIndices;
			mesh.SetVertexBufferParams(verticesCount, vertexLayout);
			mesh.SetIndexBufferParams(indicesCount, IndexFormat.UInt32);
			int meshVerticesStart = 0;
			int meshIndicesStart = 0;
			for (int j = 0; j < jobsCount; ++j)
			{
				var job = jobArray[j];

				mesh.SetVertexBufferData(job.vertices, 0, meshVerticesStart, job.vertices.Length, 0, updateFlags);
				mesh.SetIndexBufferData(job.indices, 0, meshIndicesStart, job.indices.Length, updateFlags);

				meshVerticesStart += job.vertices.Length;
				meshIndicesStart += job.indices.Length;
			}
			mesh.subMeshCount = 1;
			mesh.SetSubMesh(0, new SubMeshDescriptor(0, indicesCount, MeshTopology.Triangles));
			mesh.RecalculateBounds(); 

			meshFilter.mesh = mesh;

			for (int j = 0; j < jobsCount; ++j)
			{
				var job = jobArray[j];
				if (job.indices.IsCreated)
					job.indices.Dispose();
				if (job.vertices.IsCreated)
					job.vertices.Dispose();

			}
			Debug.Log($"creation time:{sw.ElapsedMilliseconds}ms");
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

