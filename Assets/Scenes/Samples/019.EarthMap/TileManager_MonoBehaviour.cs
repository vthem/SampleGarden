using System.Collections.Generic;

using UnityEngine;

namespace _019_EarthMap
{
	public class TileUtils
	{
		public static Vector2Int FloorUVToIndex(Vector2 uv, int zoom)
		{
			float max = Mathf.Pow(2f, zoom + 1f);
			return new Vector2Int(Mathf.FloorToInt(uv.x * max), Mathf.FloorToInt(uv.y * max));
		}

		public static Vector2 IndexToUV(Vector2Int index, int zoom)
		{
			float max = Mathf.Pow(2f, zoom + 1f);
			return new Vector2(index.x / max, index.y / max);
		}

		public static Vector2 UVTileRoundGap(Vector2 uv, int zoom)
		{
			var index = FloorUVToIndex(uv, zoom);
			var roundedUV = IndexToUV(index, zoom);
			var gap = uv - roundedUV;
			float max = Mathf.Pow(2f, zoom + 1f);
			var tileSize = 1 / max;
			return gap / tileSize;
		}
	}

	public class TileManager_MonoBehaviour : MonoBehaviour
	{
		public Vector2Int tileCount = new Vector2Int(2, 2);

		public int startZoom = 11;
		public Vector2Int startIndex = new Vector2Int(1017, 739);

		public GameObject tileTemplate;

		private Vector2 uv;
		private int zoom;
		private readonly Dictionary<Vector2Int, Tile_MonoBehaviour> tiles = new Dictionary<Vector2Int, Tile_MonoBehaviour>();

		private void Awake()
		{
			tileTemplate.SetActive(false);
			uv = TileUtils.IndexToUV(startIndex, startZoom);
			zoom = startZoom;
		}

		private void Update()
		{
			Vector2Int firstIndex = TileUtils.FloorUVToIndex(uv, zoom);

			int arrayIndex = 0;
			for (int i = 0; i < tileCount.x; ++i)
			{
				for (int j = 0; j < tileCount.y; ++j)
				{
					Vector2Int indexOffset = new Vector2Int(i, j);
					Tile_MonoBehaviour tile;
					if (!tiles.TryGetValue(firstIndex + indexOffset, out tile))
					{
						tile = CreateTile(firstIndex + indexOffset, $"tile[{arrayIndex}]");
					}
					tile.transform.localPosition = new Vector3(i, 0, -j);

					arrayIndex++;
				}
			}
		}

		private Tile_MonoBehaviour CreateTile(Vector2Int index, string name)
		{
			GameObject tileObj = GameObject.Instantiate(tileTemplate);
			tileObj.SetActive(true);
			tileObj.name = name;
			Tile_MonoBehaviour tile = tileObj.GetComponent<Tile_MonoBehaviour>();
			tile.zoom = zoom;
			tile.x = index.x;
			tile.y = index.y;
			tile.transform.SetParent(transform);
			return tile;
		}
	}
}
