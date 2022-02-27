using System.Collections.Generic;

using UnityEngine;

namespace _011_PlaneQuadTree
{
	public struct PlaneQuadTree
	{
		public Rect rect;
		public int index;
		public int depth;
		public bool visible;
		public int child0;
		public int child1;
		public int child2;
		public int child3;
		public GameObject plane;
	}

	public class ChunkBehaviour : MonoBehaviour
	{
		private const int QuadTreeMaxSize = 200;
		private const float planeSize = 10f;
		public float[] depthMinDistance = new float[] { 20f, 5f, 2.5f, 1f };
		public Rect region;
		public GameObject template;

		private PlaneQuadTree[] tree = new PlaneQuadTree[QuadTreeMaxSize];
		private Stack<int> freeQuads = new Stack<int>(QuadTreeMaxSize);
		private float[] depthMinSqDistance;
		private int rootIndex;
		private bool started = false;

		public void RebuildAt(Vector2 worldPosition)
		{
			if (!started)
				return;

			depthMinSqDistance = new float[depthMinDistance.Length];
			for (int i = 0; i < depthMinDistance.Length; ++i)
			{
				var v = depthMinDistance[i];
				depthMinSqDistance[i] = v * v;
			}

			DestroyTree(rootIndex);
			Vector3 worldPosition3 = new Vector3(worldPosition.x, 0, worldPosition.y);
			var localPosition3 = transform.InverseTransformPoint(worldPosition3);
			var localPosition2 = new Vector2(localPosition3.x, localPosition3.z);
			UpdateQuad(rootIndex, localPosition2);
		}

		// Start is called before the first frame update
		void Start()
		{
			for (int i = 0; i < QuadTreeMaxSize; ++i)
			{
				var gobj = GameObject.Instantiate(template);
				gobj.transform.SetParent(transform);
				gobj.SetActive(false);
				gobj.name = $"plane#{i}";
				PlaneQuadTree qt = new PlaneQuadTree();
				qt.plane = gobj;
				tree[i] = qt;
				freeQuads.Push(i);
			}

			if (CreateQuad(region, 0, out rootIndex))
			{
				//CreateQuad(rootIndex);
				//CreateQuad(tree[rootIndex].child0);
			}
			else
			{
				Debug.Log("Fail to create root QuadTree");
			}

			started = true;
		}

		private void DestroyTree(int qtIndex)
		{
			if (qtIndex < 0)
				return;

			var qt = tree[qtIndex];
			DestroyTree(qt.child0);
			DestroyTree(qt.child1);
			DestroyTree(qt.child2);
			DestroyTree(qt.child3);

			qt.child0 = qt.child1 = qt.child2 = qt.child3 = -1;
			qt.visible = false;
			qt.plane.SetActive(false);
			if (qt.depth == 0) // don't destroy root tree
			{
				tree[qtIndex] = qt;
				return;
			}
			qt.depth = -1;
			qt.index = -1;
			tree[qtIndex] = qt;
			freeQuads.Push(qtIndex);
		}

		private void UpdateQuad(int qtIndex, Vector2 position)
		{
			if (qtIndex < 0)
				return;
			var qt = tree[qtIndex];
			if (qt.depth >= depthMinDistance.Length)
				return;
			var qtSqDist = (qt.rect.center - position).sqrMagnitude;
			var minSqDist = depthMinSqDistance[qt.depth];
			if (qtSqDist < minSqDist && qt.child0 == -1)
			{
				CreateQuad(qtIndex);
				qt = tree[qtIndex];
			}
			UpdateQuad(qt.child0, position); UpdateQuad(qt.child1, position); UpdateQuad(qt.child2, position); UpdateQuad(qt.child3, position);
		}

		private bool CreateQuad(Rect rect, int depth, out int index)
		{
			if (freeQuads.Count == 0)
			{
				index = -1;
				Debug.LogError("no free QuadTree");
				return false;
			}

			index = freeQuads.Pop();
			var qt = tree[index];
			qt.index = index;
			qt.rect = rect;
			qt.depth = depth;
			qt.plane.SetActive(true);
			qt.visible = true;
			qt.plane.transform.localPosition = new Vector3(rect.center.x, 0, rect.center.y);
			qt.plane.transform.localScale = new Vector3(rect.size.x / planeSize, 0, rect.size.y / planeSize);
			qt.child0 = qt.child1 = qt.child2 = qt.child3 = -1;

			tree[index] = qt;

			return true;
		}

		private bool CreateQuad(int parentIndex)
		{
			if (freeQuads.Count < 4)
			{
				Debug.LogError("no free QuadTree");
				return false;
			}

			var parent = tree[parentIndex];

			Vector2 childSize = parent.rect.size / 2f;

			Rect rect = new Rect(new Vector2(parent.rect.center.x - parent.rect.size.x * 0.5f, parent.rect.center.y - parent.rect.size.y * 0.5f), childSize);
			CreateQuad(rect, parent.depth + 1, out int child);
			parent.child0 = child;

			rect = new Rect(new Vector2(parent.rect.center.x - parent.rect.size.x * 0.5f, parent.rect.center.y), childSize);
			CreateQuad(rect, parent.depth + 1, out child);
			parent.child1 = child;

			rect = new Rect(new Vector2(parent.rect.center.x, parent.rect.center.y), childSize);
			CreateQuad(rect, parent.depth + 1, out child);
			parent.child2 = child;

			rect = new Rect(new Vector2(parent.rect.center.x, parent.rect.center.y - parent.rect.size.y * 0.5f), childSize);
			CreateQuad(rect, parent.depth + 1, out child);
			parent.child3 = child;

			parent.plane.SetActive(false);
			parent.visible = false;

			tree[parentIndex] = parent;

			return true;
		}
	}

}