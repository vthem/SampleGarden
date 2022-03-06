using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _011_PlaneQuadTree
{
	public class PlaneQuadTree
	{
		public Rect rect;
		public int depth;
		public PlaneQuadTree[] childs;

		public bool HasChilds { get { return childs[0] != null; } }
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

		//private PlaneQuadTree[] tree = new PlaneQuadTree[QuadTreeMaxSize];
		private Stack<PlaneQuadTree> freeQuads = new Stack<PlaneQuadTree>();
		private float[] depthMinSqDistance;
		private PlaneQuadTree root;
		private bool paused = false;
		private List<PlaneQuadTree> quads = new List<PlaneQuadTree>();
		private Vector2 targetPosition;
		private List<Matrix4x4> planes = new List<Matrix4x4>();

		private void Update()
		{
			RebuildRoot();

			RebuildPlaneList();

			Graphics.DrawMeshInstanced(
				mesh: mesh,
				submeshIndex: 0,
				material: material,
				matrices: planes,
				properties: null,
				castShadows: UnityEngine.Rendering.ShadowCastingMode.Off,
				receiveShadows: true);
		}

		private void RebuildPlaneList()
		{
			planes.Clear();

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

			DestroyTree(root);

			ComputeTargetPosition();

			quads.Clear();
			UpdateQuad(root);

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
				planes.Add(planeMatrix);
			}
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

		void RebuildRoot()
		{
			var sqSize = Mathf.Max(region.size.x, region.size.y);
			var rootRect = new Rect(region.xMin, region.yMin, sqSize, sqSize);
			if (root == null)
			{
				root = CreateQuad(rootRect, 0);
			}
			else
			{
				if (root.rect != rootRect)
				{
					DestroyTree(root);
					root = CreateQuad(rootRect, 0);
				}
			}
		}

		private PlaneQuadTree NewPlaneQuadTree()
		{
			PlaneQuadTree qt = new PlaneQuadTree();
			qt.childs = new PlaneQuadTree[4];
			return qt;
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

			if (qt.depth == 0) // don't destroy root tree
			{
				return;
			}
			qt.depth = -1;
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

			if (qt.depth >= depthMinDistance.Length)
				return;


			bool createChilds = qt.rect.Contains(targetPosition);
			if (!createChilds)
			{
				var qtSqSqSize = qt.rect.size.x * qt.rect.size.x;
				var qtSqDist = (qt.rect.center - targetPosition).sqrMagnitude - qtSqSqSize;
				qtSqDist = Mathf.Max(0, qtSqDist);
				var minSqDist = depthMinSqDistance[qt.depth];

				createChilds = qtSqDist < minSqDist;

			}
			if (createChilds)
			{
				CreateQuadChilds(qt);
			}
			for (int i = 0; i < qt.childs.Length; ++i)
			{
				UpdateQuad(qt.childs[i]);
			}
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
					Handles.Label(GetQuatPositionWS(qt), $"{i}:{GetViewportArea(qt):F2}");
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
	}

}