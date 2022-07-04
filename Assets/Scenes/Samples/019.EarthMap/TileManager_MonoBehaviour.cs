using System.Collections.Generic;

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _019_EarthMap
{
	public class TileUtils
	{
		public static float TileCount(int zoom)
		{
			return Mathf.Pow(2f, zoom + 1f);
		}

		public static Vector2Int FloorUVToIndex(Vector2 uv, int zoom)
		{
			float max = TileCount(zoom);
			return new Vector2Int(Mathf.FloorToInt(uv.x * max), Mathf.FloorToInt(uv.y * max));
		}

		public static Vector2 IndexToUV(Vector2Int index, int zoom)
		{
			float max = TileCount(zoom);
			return new Vector2(index.x / max, index.y / max);
		}

		public static Vector2 UVTileRoundGap(Vector2 uv, int zoom)
		{
			var index = FloorUVToIndex(uv, zoom);
			var roundedUV = IndexToUV(index, zoom);
			var gap = uv - roundedUV;
			float max = TileCount(zoom);
			var tileSize = 1 / max;
			return gap * tileSize;
		}
	}

	public class TileManager_MonoBehaviour : MonoBehaviour
	{
		public Vector2Int tileCountXY = new Vector2Int(2, 2);

		public int zoom = 11;
		public Vector2Int startIndex = new Vector2Int(1017, 739);

		public GameObject tileTemplate;

		private Vector2 uv;
		private readonly Dictionary<Vector2Int, Tile_MonoBehaviour> tiles = new Dictionary<Vector2Int, Tile_MonoBehaviour>();

		public Vector2 UpdateUV()
		{
			uv = TileUtils.IndexToUV(startIndex, zoom);
			return uv;
		}

		private void Awake()
		{
			tileTemplate.SetActive(false);
			UpdateUV();
			zoom = zoom;
		}

		private void Update()
		{
			Vector2Int firstIndex = TileUtils.FloorUVToIndex(uv, zoom);

			int arrayIndex = 0;
			for (int i = 0; i < tileCountXY.x; ++i)
			{
				for (int j = 0; j < tileCountXY.y; ++j)
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


#if UNITY_EDITOR
	[CustomEditor(typeof(TileManager_MonoBehaviour))]
	public class TerraGenCPU_MeshGen_Editor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var mono = target as TileManager_MonoBehaviour;
			var uv = mono.UpdateUV();
			EditorGUILayout.LabelField($"uv.x:{uv.x}");
			EditorGUILayout.LabelField($"uv.y:{uv.y}");

			if (GUILayout.Button("+u.x"))
			{
				uv.x += .1f / TileUtils.TileCount(mono.zoom);
			}
			if (GUILayout.Button("-u.x"))
			{
				uv.x -= .1f / TileUtils.TileCount(mono.zoom);
			}
		}
	}
#endif
}
