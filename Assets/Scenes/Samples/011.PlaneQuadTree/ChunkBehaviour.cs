using System.Collections.Generic;

using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _011_PlaneQuadTree
{
	public class PlaneQuadTree
	{
		private const int _count = 4;

		public Rect rect;
		//public int depth;
		public PlaneQuadTree[] childs = new PlaneQuadTree[_count];
		public PlaneQuadTree[] neibhbors = new PlaneQuadTree[_count];
		public int depthInfo = 0;
		public int index;
		public int instanceId;

		public bool HasChilds { get { return childs[0] != null; } }

		public PlaneQuadTree()
		{
			Reset();
		}

		public void Reset()
		{
			index = -1;
			depthInfo = 0;
			rect = new Rect();
			for (int i = 0; i < _count; ++i)
			{
				childs[i] = null;
				neibhbors[i] = null;
			}
			//neighborDelta = 0;
		}

		public bool TryFind(Vector2 pos, out PlaneQuadTree outQt)
		{
			outQt = null;
			if (!rect.Contains(pos))
				return false;

			outQt = this;
			for (int i = 0; i < outQt.childs.Length; ++i)
			{
				if (outQt.childs[i] != null && outQt.childs[i].rect.Contains(pos))
				{
					outQt = outQt.childs[i];
					i = -1;
					continue;
				}
			}
			return true;
		}

		const int depthBits = 6;
		const int depthMask = 0x0000003F;
		const int selftDepthInfoIndex = 4;

		public void SetNeightborDelta(int i, int depth)
		{
			var mask = depthMask << (i * depthBits);
			depthInfo &= ~mask;
			depthInfo |= depth << (i * depthBits);
		}

		public int GetNeightborDelta(int i)
		{
			return (depthInfo >> (i * depthBits)) & depthMask;
		}

		public int Depth
		{
			get
			{
				return (int)(depthInfo >> (selftDepthInfoIndex * depthBits)) & depthMask;
			}
			set
			{
				depthInfo &= ~0x3F000000;
				depthInfo |= value << (selftDepthInfoIndex * depthBits);
			}
		}

		public static readonly Vector2[] Neighbors = { Vector2.left, Vector2.up, Vector2.right, Vector2.down };
	}

	public class ChunkBehaviour : MonoBehaviour
	{
		private const float planeSize = 10f;
		public float[] depthMinDistance = new float[] { 20f, 5f, 2.5f, 1f };
		public Rect region;

		public GameObject positionObj;
		public Mesh mesh;
		public Material material;
		public bool drawOutsideRegion = false;
		[Range(0, 15)] public int maxDepth = 4;

		public enum LodStrategy
		{
			TargetPosition,
			TargetPositionForceDepth,
			CameraViewportArea
		};
		public LodStrategy lodStrategy = LodStrategy.TargetPosition;

		private Stack<PlaneQuadTree> freeQuads = new Stack<PlaneQuadTree>();
		private float[] depthMinSqDistance;
		private PlaneQuadTree root;
		private bool paused = false;
		private List<PlaneQuadTree> quads = new List<PlaneQuadTree>();
		private Vector2 targetPosition;
		private List<InstanceData> instances = new List<InstanceData>();
		private ComputeBuffer instancesBuffer;
		private ComputeBuffer argsBuffer;

		private const int MaxInstanceDataCount = 200;
		private Bounds bounds;

		private struct InstanceData
		{
			public Matrix4x4 objectToWorld;
			public Matrix4x4 worldToObject;
			public int depth;

			public static int Size()
			{
				return sizeof(float) * 4 * 4 * 2 + sizeof(int);
			}
		}

		private void Start()
		{
			if (!mesh)
			{
				Debug.LogError("mesh property not set");
				return;
			}
			CopyUpdateMesh();
		}

		private void CopyUpdateMesh()
		{
			// the goal is to have vertex position between 0..10 in the vertex shader, to use them as indices.

			var meshCopy = new Mesh();
			meshCopy.name = "chunk";
			var vertices = mesh.vertices;
			for (int i = 0; i < vertices.Length; ++i)
			{
				vertices[i] += new Vector3(5, 0, 5);
			}
			meshCopy.vertices = vertices;
			meshCopy.triangles = mesh.triangles;
			meshCopy.normals = mesh.normals;
			meshCopy.uv = mesh.uv;

			meshCopy.RecalculateBounds();
			mesh = meshCopy;
		}

		private void Update()
		{
			RebuildPlaneList();

			if (argsBuffer != null)
			{
				Graphics.DrawMeshInstancedIndirect(
					mesh: mesh,
					submeshIndex: 0,
					material: material,
					bounds: bounds,
					bufferWithArgs: argsBuffer,
					argsOffset: 0,
					properties: null,
					castShadows: UnityEngine.Rendering.ShadowCastingMode.Off,
					receiveShadows: true);
			}
		}

		private void RebuildPlaneList()
		{

			if (Input.GetKeyDown(KeyCode.K))
				paused = !paused;

			if (paused)
				return;

			depthMinSqDistance = new float[depthMinDistance.Length];
			for (int i = 0; i < depthMinDistance.Length; ++i)
			{
				var v = depthMinDistance[i];
				depthMinSqDistance[i] = v * v;
			}

			ComputeTargetPosition();

			// clear the existing
			DestroyTree(root);
			root = null;
			quads.Clear();
			bounds = new Bounds();

			// rebuild the quadtree
			BuildRoot();
			UpdateQuad(root);

			instances.Clear();
			// update quads data
			for (int i = 0; i < quads.Count; ++i)
			{
				var qt = quads[i];
				if (qt.HasChilds)
					continue;
				bool isInsideRegion = region.xMin <= qt.rect.xMin && region.xMax >= qt.rect.xMax && region.yMin <= qt.rect.yMin && region.yMax >= qt.rect.yMax;
				if (!drawOutsideRegion && !isInsideRegion)
					continue;
				var scale = new Vector3(qt.rect.size.x / planeSize, 1, qt.rect.size.y / planeSize);
				var pos = GetQuatPositionWS(qt);
				bounds.Encapsulate(pos);
			
				var instanceId = instances.Count;
				InstanceData data = new InstanceData();
				data.objectToWorld = Matrix4x4.TRS(pos, Quaternion.identity, scale);
				data.worldToObject = data.objectToWorld.inverse;
				FindNeighborDepth(qt);
				data.depth = qt.depthInfo;
				instances.Add(data);
				qt.instanceId = instanceId;
			}

			if (instances.Count == 0)
				return;

			if (instancesBuffer != null)
			{
				instancesBuffer.Release();
			}
			instancesBuffer = new ComputeBuffer(instances.Count, InstanceData.Size());
			instancesBuffer.SetData(instances.ToArray());
			material.SetBuffer("_PerInstanceData", instancesBuffer);

			material.SetFloat("_quadWidth", region.width);
			material.SetFloat("_quadHeight", region.height);

			uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
			args[0] = (uint)mesh.GetIndexCount(0);
			args[1] = (uint)instances.Count;
			args[2] = (uint)mesh.GetIndexStart(0);
			args[3] = (uint)mesh.GetBaseVertex(0);

			if (argsBuffer != null)
			{
				argsBuffer.Release();
			}
			argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
			argsBuffer.SetData(args);
		}

		private void OnDisable()
		{
			if (instancesBuffer != null)
			{
				instancesBuffer.Release();
				instancesBuffer = null;
			}
			if (argsBuffer != null)
			{
				argsBuffer.Release();
				argsBuffer = null;
			}
		}

		private Vector3 GetQuatPositionWS(PlaneQuadTree qt)
		{
			return GetPositionWS(qt.rect.min);
		}

		private Vector3 GetPositionWS(Vector2 v)
		{
			return new Vector3(v.x, 0f, v.y) + transform.localPosition;
		}

		private float GetViewportArea(PlaneQuadTree qt)
		{
			var min = Camera.main.WorldToViewportPoint(GetPositionWS(qt.rect.min));
			var max = Camera.main.WorldToViewportPoint(GetPositionWS(qt.rect.max));
			var delta = max - min;
			return delta.x * delta.y;
		}

		void BuildRoot()
		{
			if (root != null)
				return;

			var sqSize = Mathf.Max(region.size.x, region.size.y);
			var rootRect = new Rect(region.xMin, region.yMin, sqSize, sqSize);
			root = CreateQuad(rootRect, 0);
			
		}

		private PlaneQuadTree NewPlaneQuadTree()
		{
			return new PlaneQuadTree();
		}

		private void DestroyTree(PlaneQuadTree qt)
		{
			if (null == qt)
				return;

			for (int i = 0; i < qt.childs.Length; ++i)
			{
				DestroyTree(qt.childs[i]);
				qt.childs[i] = null;
			}

			qt.Reset();
			freeQuads.Push(qt);
		}

		private void ComputeTargetPosition()
		{
			Vector3 worldPosition3 = positionObj.transform.position;
			var localPosition3 = transform.InverseTransformPoint(worldPosition3);
			targetPosition = new Vector2(localPosition3.x, localPosition3.z);
		}

		private void UpdateQuad(PlaneQuadTree qt)
		{
			if (null == qt)
				return;

			bool createChilds = false;
			switch (lodStrategy)
			{
				case LodStrategy.TargetPosition:
					createChilds = ShouldCreateChilds_Distance(qt);
					break;
				case LodStrategy.TargetPositionForceDepth:
					createChilds = ShouldCreateChilds_ForceDepth(qt);
					break;
				case LodStrategy.CameraViewportArea:
					break;
				default:
					break;
			};
			

			if (createChilds)
			{
				CreateQuadChilds(qt);
			}
			for (int i = 0; i < qt.childs.Length; ++i)
			{
				UpdateQuad(qt.childs[i]);
			}
		}

		private bool ShouldCreateChilds_Distance(PlaneQuadTree qt)
		{
			if (qt.Depth >= depthMinSqDistance.Length)
				return false;

			bool createChilds = qt.rect.Contains(targetPosition);
			if (createChilds)
				return true;

			var qtSqSqSize = qt.rect.size.x * qt.rect.size.x;
			var qtSqDist = (qt.rect.center - targetPosition).sqrMagnitude - qtSqSqSize;
			qtSqDist = Mathf.Max(0, qtSqDist);
			var minSqDist = depthMinSqDistance[qt.Depth];

			return qtSqDist < minSqDist;
		}

		private bool ShouldCreateChilds_ForceDepth(PlaneQuadTree qt)
		{
			bool inside = qt.rect.Contains(targetPosition);
			if (!inside)
				return false;

			if (qt.Depth >= maxDepth)
				return false;

			return true;
		}

		private PlaneQuadTree CreateQuad(Rect rect, int depth)
		{
			PlaneQuadTree qt;
			if (freeQuads.Count == 0)
			{
				qt = NewPlaneQuadTree();
			}
			else
			{
				qt = freeQuads.Pop();
			}

			qt.rect = rect;
			qt.Depth = depth;

			for (int i = 0; i < qt.childs.Length; ++i)
			{
				qt.childs[i] = null;
			}

			qt.index = quads.Count;
			quads.Add(qt);

			return qt;
		}

		private void CreateQuadChilds(PlaneQuadTree parent)
		{
			if (null == parent)
				return;

			Vector2 childSize = parent.rect.size / 2f;

			Rect rect = new Rect(new Vector2(parent.rect.center.x - parent.rect.size.x * 0.5f, parent.rect.center.y - parent.rect.size.y * 0.5f), childSize);
			parent.childs[0] = CreateQuad(rect, parent.Depth + 1);

			rect = new Rect(new Vector2(parent.rect.center.x - parent.rect.size.x * 0.5f, parent.rect.center.y), childSize);
			parent.childs[1] = CreateQuad(rect, parent.Depth + 1);

			rect = new Rect(new Vector2(parent.rect.center.x, parent.rect.center.y), childSize);
			parent.childs[2] = CreateQuad(rect, parent.Depth + 1);

			rect = new Rect(new Vector2(parent.rect.center.x, parent.rect.center.y - parent.rect.size.y * 0.5f), childSize);
			parent.childs[3] = CreateQuad(rect, parent.Depth + 1);
		}

		private void OnDrawGizmos()
		{
			for (int i = 0; i < quads.Count; ++i)
			{
				var qt = quads[i];
				GizmosDrawRect(qt.rect, Color.green);
#if UNITY_EDITOR
				if (!qt.HasChilds)
				{
					Handles.Label(GetPositionWS(qt.rect.center), $"{qt.instanceId}:{qt.Depth}");

					Vector2[] dirs = PlaneQuadTree.Neighbors;

					for (int k = 0; k < 4; ++k)
					{
						var pos = qt.rect.center + dirs[k] * qt.rect.size.x * 0.45f;

						//if (qt.neibhbors[k] == null)
						//{
						//	Handles.Label(GetPositionWS(pos), $"=");
						//}
						//else
						//{
						//	Handles.Label(GetPositionWS(pos), $"{qt.neibhbors[k].instanceId}:{qt.neibhbors[k].depth}");
						//}
						Handles.Label(GetPositionWS(pos), $"{qt.GetNeightborDelta(k)}");
					}
				}
#endif
			}
			GizmosDrawRect(region, Color.magenta);
		}

		private void GizmosDrawRect(Rect rect, Color color)
		{
			var v1 = transform.TransformPoint(new Vector3(rect.xMin, 0, rect.yMin));
			var v2 = transform.TransformPoint(new Vector3(rect.xMin, 0, rect.yMax));
			var v3 = transform.TransformPoint(new Vector3(rect.xMax, 0, rect.yMax));
			var v4 = transform.TransformPoint(new Vector3(rect.xMax, 0, rect.yMin));
			Gizmos.color = color;
			Gizmos.DrawLine(v1, v2);
			Gizmos.DrawLine(v2, v3);
			Gizmos.DrawLine(v3, v4);
			Gizmos.DrawLine(v4, v1);
		}

		private void FindNeighborDepth(PlaneQuadTree qt)
		{
			Vector2[] dirs = PlaneQuadTree.Neighbors;

			for (int i = 0; i < dirs.Length; ++i)
			{
				if (root.TryFind(qt.rect.center + dirs[i] * qt.rect.size.x, out var neighbor))
				{
					if (neighbor.Depth < qt.Depth)
					{
						var delta = qt.Depth - neighbor.Depth;
						qt.SetNeightborDelta(i, delta);
						qt.neibhbors[i] = neighbor;
					}
				}
				
			}
		}
	}

}