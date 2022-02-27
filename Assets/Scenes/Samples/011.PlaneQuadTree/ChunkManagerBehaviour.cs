using UnityEngine;

namespace _011_PlaneQuadTree
{
	public class ChunkManagerBehaviour : MonoBehaviour
	{
		public Vector2Int size = new Vector2Int(1, 1);
		public GameObject template;
		public GameObject positionObj;
		

		private ChunkBehaviour[] chunkArray;
		private Vector2Int lastRebuildSize = new Vector2Int(-1, -1);

		// Update is called once per frame
		void Update()
		{
			RebuildChunkArray();

			Vector2 position = new Vector2(positionObj.transform.position.x, positionObj.transform.position.z);
			for (int i = 0; i < chunkArray.Length; ++i)
			{
				chunkArray[i].RebuildAt(position);
			}
		}

		void RebuildChunkArray()
		{
			if (lastRebuildSize == size)
				return;

			if (chunkArray != null)
			{
				for (int i = 0; i < chunkArray.Length; ++i)
				{
					Destroy(chunkArray[i].gameObject);
				}
			}

			chunkArray = new ChunkBehaviour[size.x * size.y];
			for (int i = 0; i < chunkArray.Length; ++i)
			{
				GameObject gobj = GameObject.Instantiate(template);
				var v = Utils.GetXYFromIndex(i, size.x);
				gobj.name = $"chunk[{v.x},{v.y}]";
				gobj.transform.SetParent(transform);
				var chunk = gobj.GetComponent<ChunkBehaviour>();
				gobj.transform.localPosition = new Vector3(chunk.region.size.x * v.x, 0, chunk.region.size.y * v.y);
				gobj.SetActive(true);
				chunkArray[i] = chunk;
			}

			lastRebuildSize = size;
		}
	}
}