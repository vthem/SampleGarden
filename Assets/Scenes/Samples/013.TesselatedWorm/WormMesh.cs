using Unity.Collections;

using UnityEngine;
using UnityEngine.Rendering;

using static UnityEngine.Rendering.HableCurve;

namespace _013_TesselatedWorm
{
	[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
	struct Vertex
	{
		public Vector3 pos;
		public Vector3 normal;
		public Vector2 uv;
	}

	[ExecuteInEditMode]
	public class WormMesh : MonoBehaviour
	{
		public PerlinWorm perlinWorm;

		[Range(1, 128)] public int radialSegmentCount = 6;

		public float gizmoPointSize = 0.1f;

		public float width = 1f;
		public float height = 1f;

		private Mesh mesh = null;

		private float Length => perlinWorm.config.length;
		private float SegmentLength => perlinWorm.config.stepLength * 100;
		private float Radius => perlinWorm.config.wormRadius;

		// Start is called before the first frame update
		private void Start()
		{
		}

		// Update is called once per frame
		private void Update()
		{
			perlinWorm.Update();
			if (mesh == null)
			{
				AllocateMeshInfo();
			}

			Vector4[] vPos = new Vector4[perlinWorm.Count];
			Vector4[] vForward = new Vector4[perlinWorm.Count];
			for (int i = 0; i < vPos.Length; ++i)
			{
				vPos[i] = perlinWorm.GetPosition(i);
				vForward[i] = perlinWorm.GetForward(i);
			}

			Material mat = GetComponent<MeshRenderer>().sharedMaterial;
			mat.SetVectorArray("_PositionArray", vPos);
			mat.SetVectorArray("_ForwardArray", vForward);
			mat.SetInt("_ArrayCount", perlinWorm.Count);
			mat.SetFloat("_Radius", Radius);
			mat.SetFloat("_Length", Length);

			GenerateMesh();
		}

		private void OnDisable()
		{
			ReleaseMeshInfo();
		}

		private void OnValidate()
		{
			if (mesh)
				mesh.SafeDestroy();
			mesh = null;
			ReleaseMeshInfo();
		}

		private NativeArray<Vertex> vertices;
		private NativeArray<ushort> indices;

		private void AllocateMeshInfo()
		{
			mesh = new Mesh();
			GetComponent<MeshFilter>().sharedMesh = mesh;
			mesh.MarkDynamic();

			vertices = new NativeArray<Vertex>(VertexCount, Allocator.Persistent);
			indices = new NativeArray<ushort>(IndiceCount, Allocator.Persistent);
		}

		private void ReleaseMeshInfo()
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

		private static VertexAttributeDescriptor[] vertexLayout = new[]
		{
			new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
			new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
			new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2)
		};

		public int VertexCount => XCount * ZCount;
		public int IndiceCount => (XCount - 1) * (ZCount - 1) * 2 * 3;
		public int XCount => radialSegmentCount + 1;
		public int ZCount => Mathf.CeilToInt(Length / SegmentLength) + 1;

		private void GenerateMesh()
		{
			int xCount = XCount;
			int zCount = ZCount;
			var vertexCount = VertexCount;
			var indiceCount = IndiceCount;

			// specify vertex count and layout
			mesh.SetVertexBufferParams(vertexCount, vertexLayout);

			int x, z;
			for (z = 0; z < zCount; z++)
			{
				for (x = 0; x < xCount; x++)
				{
					var i = x + z * xCount;
					float angle = Mathf.PI * 2 * x / (radialSegmentCount);
					//Vector3 pOnPath = perlinWorm.GetPositionNormalized(z / (float)(zCount - 1));
					//Vector3 pOnCircle = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
					//Vector3 p = pOnCircle * Radius + pOnPath; // new Vector3(0, 0, z * SegmentLength);

					float t = z / (float)(zCount - 1);
					Vector3 pOnPath = perlinWorm.GetPositionNormalized(t);
					//Vector3 tan = perlinWorm.GetForwardNormalized(t);
					//Quaternion q = Quaternion.FromToRotation(Vector3.forward, tan);
					Vector3 pOnCircle = new Vector3(Mathf.Cos(angle) * Radius, Mathf.Sin(angle) * Radius, 0);
					Vector3 p = pOnPath + pOnCircle;

					vertices[i] = new Vertex
					{
						pos = p,
						normal = -pOnCircle,
						uv = new Vector2(x / (float)(xCount - 1), z / (float)(xCount - 1))
					};
				}
			}

			mesh.SetVertexBufferData(vertices, 0, 0, vertexCount);
			mesh.SetIndexBufferParams(indiceCount, IndexFormat.UInt16);

			int idx = 0;
			for (z = 0; z < zCount - 1; ++z)
			{
				for (x = 0; x < xCount - 1; ++x)
				{
					int vi = z * (xCount) + x;
					ushort p1 = (ushort)vi;
					ushort p2 = (ushort)(vi + 1);
					ushort p3 = (ushort)(vi + xCount);
					ushort p4 = (ushort)(vi + xCount + 1);
					indices[idx++] = p1;
					indices[idx++] = p3;
					indices[idx++] = p2;
					indices[idx++] = p2;
					indices[idx++] = p3;
					indices[idx++] = p4;
				}
			}

			mesh.SetIndexBufferData(indices, 0, 0, indiceCount);
			mesh.subMeshCount = 1;
			mesh.SetSubMesh(0, new SubMeshDescriptor(0, indiceCount, MeshTopology.Triangles));
			mesh.RecalculateBounds();
			mesh.RecalculateNormals();
		}
	}

}