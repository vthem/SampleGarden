using System.Collections.Generic;

using UnityEngine;
using System.Reflection;
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
		public int depth;
		public PlaneQuadTree[] childs = new PlaneQuadTree[_count];
		public int[] neighborDepthDelta = new int[_count];
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
			depth = -1;
			rect = new Rect();
			for (int i = 0; i < _count; ++i)
			{
				childs[i] = null;
				neighborDepthDelta[i] = -1;
			}
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
	}

	public class ChunkBehaviour : MonoBehaviour
	{
		private const int QuadTreeMaxSize = 200;
		private const float planeSize = 10f;
		public float[] depthMinDistance = new float[] { 20f, 5f, 2.5f, 1f };
		public Rect region;

		public GameObject positionObj;
		public Mesh mesh;
		public Material material;
		public bool drawOutsideRegion = false;
		[Range(0, 7)] public int maxDepth = 4;

		public enum LodStrategy
		{
			TargetPosition,
			CameraViewportArea
		};
		public LodStrategy lodStrategy = LodStrategy.TargetPosition;

		//private PlaneQuadTree[] tree = new PlaneQuadTree[QuadTreeMaxSize];
		private Stack<PlaneQuadTree> freeQuads = new Stack<PlaneQuadTree>();
		private float[] depthMinSqDistance;
		private PlaneQuadTree root;
		private bool paused = false;
		private List<PlaneQuadTree> quads = new List<PlaneQuadTree>();
		private Vector2 targetPosition;
		private Matrix4x4[] planes = new Matrix4x4[MaxInstanceDataCount];
		private Matrix4x4[] planesInv = new Matrix4x4[MaxInstanceDataCount];
		private Color[] depthDeltaData = new Color[MaxInstanceDataCount];

		private const int MaxInstanceDataCount = 200;
		private int instanceCount = 0;

		private void Update()
		{	
			RebuildPlaneList();

			Graphics.DrawMeshInstanced(
				mesh: mesh,
				submeshIndex: 0,
				material: material,
				matrices: planes,
				count: instanceCount,
				properties: null,
				castShadows: UnityEngine.Rendering.ShadowCastingMode.Off,
				receiveShadows: true);
		}

		private void RebuildPlaneList()
		{
			instanceCount = 0;

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
			
			// rebuild the quadtree
			BuildRoot();
			UpdateQuad(root);

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
				Matrix4x4 planeMatrix = Matrix4x4.TRS(GetQuatPositionWS(qt), Quaternion.identity, scale);
				var instanceId = instanceCount;
				planes[instanceId] = planeMatrix;
				planesInv[instanceId] = planeMatrix.inverse;
				depthDeltaData[instanceId] = FindNeighborDepth(qt);
				qt.instanceId = instanceId;
				instanceCount++;
			}

			material.SetColorArray("depthDeltaData", depthDeltaData);
			material.SetMatrixArray("_objectToWorld", planesInv);
		}

		private Vector3 GetQuatPositionWS(PlaneQuadTree qt)
		{
			return GetPositionWS(qt.rect.center);
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

			bool createChilds = ShouldCreateChilds_Distance(qt);

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
			if (qt.depth >= depthMinSqDistance.Length)
				return false;

			bool createChilds = qt.rect.Contains(targetPosition);
			if (createChilds)
				return true;

			var qtSqSqSize = qt.rect.size.x * qt.rect.size.x;
			var qtSqDist = (qt.rect.center - targetPosition).sqrMagnitude - qtSqSqSize;
			qtSqDist = Mathf.Max(0, qtSqDist);
			var minSqDist = depthMinSqDistance[qt.depth];

			return qtSqDist < minSqDist;

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
			qt.depth = depth;

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
			parent.childs[0] = CreateQuad(rect, parent.depth + 1);

			rect = new Rect(new Vector2(parent.rect.center.x - parent.rect.size.x * 0.5f, parent.rect.center.y), childSize);
			parent.childs[1] = CreateQuad(rect, parent.depth + 1);

			rect = new Rect(new Vector2(parent.rect.center.x, parent.rect.center.y), childSize);
			parent.childs[2] = CreateQuad(rect, parent.depth + 1);

			rect = new Rect(new Vector2(parent.rect.center.x, parent.rect.center.y - parent.rect.size.y * 0.5f), childSize);
			parent.childs[3] = CreateQuad(rect, parent.depth + 1);
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
					Handles.Label(GetQuatPositionWS(qt), $"{qt.instanceId}");

					Vector2[] dirs = { Vector2.left, Vector2.up, Vector2.right, Vector2.down };

					for (int k = 0; k < 4; ++k)
					{
						var pos = qt.rect.center + dirs[k] * qt.rect.size.x * 0.45f;
						Handles.Label(GetPositionWS(pos), $"{qt.neighborDepthDelta[k]}");
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

		private Color FindNeighborDepth(PlaneQuadTree qt)
		{
			Vector2[] dirs = { Vector2.left, Vector2.up, Vector2.right, Vector2.down };

			Color packedDepth = new Color();
			for (int i = 0; i < 4; ++i)
			{
				if (root.TryFind(qt.rect.center + dirs[i] * qt.rect.size.x, out var neighbor))
				{
					if (neighbor.depth < qt.depth)
					{
						var delta = qt.depth - neighbor.depth;
						qt.neighborDepthDelta[i] = delta;
						packedDepth[i] = delta;
					}
				}
				
			}
			return packedDepth;
		}
	}

}