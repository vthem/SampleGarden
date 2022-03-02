using System.Collections.Generic;

using UnityEngine;

namespace _011_PlaneQuadTree
{
	public class PlaneQuadTree
	{
		public Rect rect;
		public int depth;
		public bool visible;
		public PlaneQuadTree[] childs;
	}

	public class ChunkBehaviour : MonoBehaviour
	{
		private const int QuadTreeMaxSize = 200;
		private const float planeSize = 10f;
		public float[] depthMinDistance = new float[] { 20f, 5f, 2.5f, 1f };
		public Rect region;

		//private PlaneQuadTree[] tree = new PlaneQuadTree[QuadTreeMaxSize];
		private Stack<PlaneQuadTree> freeQuads = new Stack<PlaneQuadTree>();
		private float[] depthMinSqDistance;
		private PlaneQuadTree root;
		private bool started = false;
		private bool paused = false;
		private List<PlaneQuadTree> quads = new List<PlaneQuadTree>();
		private Vector2 targetPosition;

		public void RebuildAt(Vector2 worldPosition, List<Matrix4x4> planes)
		{
			if (!started)
				return;

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
			
			Vector3 worldPosition3 = new Vector3(worldPosition.x, 0, worldPosition.y);
			var localPosition3 = transform.InverseTransformPoint(worldPosition3);
			targetPosition = new Vector2(localPosition3.x, localPosition3.z);
			
			quads.Clear();
			UpdateQuad(root);

			for (int i = 0; i < quads.Count; ++i)
			{
				var qt = quads[i];
				if (!qt.visible)
					continue;
				var pos = new Vector3(qt.rect.center.x, 0, qt.rect.center.y) + transform.localPosition;
				var scale = new Vector3(qt.rect.size.x / planeSize, 1, qt.rect.size.y / planeSize);
				Matrix4x4 planeMatrix = Matrix4x4.TRS(pos, Quaternion.identity, scale);
				planes.Add(planeMatrix);
			}
		}

		void Start()
		{
			root = CreateQuad(region, 0);
			started = true;
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

			qt.visible = false;
			if (qt.depth == 0) // don't destroy root tree
			{
				return;
			}
			qt.depth = -1;
			freeQuads.Push(qt);
		}

		private void UpdateQuad(PlaneQuadTree qt)
		{
			if (null == qt)
				return;

			if (qt.depth >= depthMinDistance.Length)
				return;

			var qtSqDist = (qt.rect.center - targetPosition).sqrMagnitude;
			var minSqDist = depthMinSqDistance[qt.depth];
			if (qtSqDist < minSqDist && qt.childs[0] == null)
			{
				CreateQuad(qt);
			}
			for (int i = 0; i < qt.childs.Length; ++i)
			{
				UpdateQuad(qt.childs[i]);
			}
		}

		private PlaneQuadTree CreateQuad(Rect rect, int depth)
		{
			PlaneQuadTree qt = null;
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
			qt.visible = true;

			for (int i = 0; i < qt.childs.Length; ++i)
			{
				qt.childs[i] = null;
			}

			quads.Add(qt);

			return qt;
		}

		private void CreateQuad(PlaneQuadTree parent)
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
			
			parent.visible = false;
		}
	}

}