using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;
using Unity.Collections;
using UnityEngine.Rendering;
using System;
using Unity.Jobs;
using UnityEngine.UIElements;
using static UnityEngine.InputManagerEntry;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;

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

		struct RGB
		{
			public byte r;
			public byte g;
			public byte b;
		}

		private struct VerticesJob : IJobParallelFor
		{
			public NativeArray<VertexData> vertices;
			[ReadOnly] public NativeArray<RGB> heightData;
			[ReadOnly] public Vector2Int count;
			[ReadOnly] public Vector2 size;
			[ReadOnly] public Vector2Int textureSize;
			[ReadOnly] public float heightScale;

			public void Execute(int vertexIndex)
			{
				Vector2Int gPos = Utils.GetXYFromIndex(vertexIndex, count.x);
				Vector2 fCount = count;
				Vector2 uv = gPos / fCount;
				Vector2 pos = uv * size;

				var height = Utils.SampleColorFromNativeArray(uv, heightData, textureSize);
				Vector3 pos3 = new Vector3(pos.x, height.r / (255f) * heightScale, pos.y);
				VertexData vd = new VertexData();
				vd.pos = pos3;
				vd.uv = uv;
				vertices[vertexIndex] = vd;
			}
		}

		private struct IndicesJob : IJobParallelFor
		{
			[NativeDisableContainerSafetyRestriction]
			public NativeArray<uint> indices;
			[ReadOnly] public Vector2Int count;

			public void Execute(int triangleIndex)
			{
				if (triangleIndex % 2 == 0)
				{
					int vertexIndex = triangleIndex / 2;
					uint p1 = (uint)vertexIndex;
					uint p2 = ((uint)vertexIndex + 1);
					uint p3 = ((uint)vertexIndex + (uint)count.x);
					indices[(vertexIndex * 6) + 0] = p1;
					indices[(vertexIndex * 6) + 1] = p3;
					indices[(vertexIndex * 6) + 2] = p2;
				}
				else
				{
					int vertexIndex = (triangleIndex - 1) / 2;
					uint p2 = ((uint)vertexIndex + 1);
					uint p3 = ((uint)vertexIndex + (uint)count.x);
					uint p4 = ((uint)vertexIndex + (uint)count.x + 1);
					indices[(vertexIndex * 6) + 3] = p2;
					indices[(vertexIndex * 6) + 4] = p3;
					indices[(vertexIndex * 6) + 5] = p4;
				}
			}
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

			var verticesCount = count.x * count.y;
			var trianglesCount = (count.x - 1) * (count.y - 1) * 2;
			var indicesCount = trianglesCount * 3;

			VerticesJob verticesJob;
			verticesJob.vertices = new NativeArray<VertexData>(verticesCount, Allocator.TempJob);;
			verticesJob.heightData = heightData;
			verticesJob.count = count;
			verticesJob.size = size;
			verticesJob.textureSize = textureSize;
			verticesJob.heightScale = heightScale;

			IndicesJob indicesJob;
			indicesJob.indices = new NativeArray<uint>(indicesCount, Allocator.TempJob);
			indicesJob.count = count;

			JobHandle verticesJobHandle = verticesJob.Schedule(verticesCount, 64);
			JobHandle indicesJobHandle = indicesJob.Schedule(trianglesCount, 64);

			indicesJobHandle.Complete();
			verticesJobHandle.Complete();
			Debug.Log($"job complete time:{sw.ElapsedMilliseconds}ms");

			MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();

			Mesh mesh = new Mesh();
			mesh.indexFormat = IndexFormat.UInt32;

			// specify vertex count and layout
			var updateFlags = MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontValidateIndices;
			mesh.SetVertexBufferParams(verticesCount, vertexLayout);
			mesh.SetIndexBufferParams(indicesCount, IndexFormat.UInt32);
			mesh.SetVertexBufferData(verticesJob.vertices, 0, 0, verticesJob.vertices.Length, 0, updateFlags);
			Debug.Log($"SetVertexBufferData time:{sw.ElapsedMilliseconds}ms");
			mesh.SetIndexBufferData(indicesJob.indices, 0, 0, indicesJob.indices.Length, updateFlags);
			Debug.Log($"SetIndexBufferData time:{sw.ElapsedMilliseconds}ms");
			mesh.subMeshCount = 1;
			mesh.SetSubMesh(0, new SubMeshDescriptor(0, indicesCount, MeshTopology.Triangles));
			mesh.RecalculateBounds();
			Debug.Log($"RecalculateBounds time:{sw.ElapsedMilliseconds}ms");

			meshFilter.mesh = mesh;

			verticesJob.vertices.Dispose();
			indicesJob.indices.Dispose();
			
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

