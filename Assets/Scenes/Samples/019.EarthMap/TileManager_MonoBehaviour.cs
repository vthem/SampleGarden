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
			return new Vector2((index.x / max) + (1 / (2 * max)), (index.y / max) + (1 / (2 * max)));
		}

		public static Vector2 TileOffsetFromUV(Vector2 uv, int zoom)
		{
			var index = FloorUVToIndex(uv, zoom);
			var roundedUV = IndexToUV(index, zoom);
			var gap = uv - roundedUV;
			float max = TileCount(zoom);
			var tileSize = 1 / max;
			var offset = gap / tileSize;
			return offset;
		}
	}

	struct TilePosition
	{
		public Vector2Int Index { get => index; }
		public int Zoom { get => zoom; }

		private Vector2Int index;
		private int zoom;
	}

	struct TileUV // should use Vector2d
	{
		public double u;
		public double v;
	}

	public class TileManager_MonoBehaviour : MonoBehaviour
	{
		public Vector2Int tileCountXY = new Vector2Int(2, 2);

		public int zoom = 11;
		public Vector2Int index = new Vector2Int(1017, 739);

		public GameObject tileTemplate;

		public Vector2 uv;
		private List<Vector2Int> tileToRemoveArray = new List<Vector2Int>(16);
		private readonly Dictionary<Vector2Int, Tile_MonoBehaviour> tilesMap = new Dictionary<Vector2Int, Tile_MonoBehaviour>();

		private void Awake()
		{
			tileTemplate.SetActive(false);
		}

		public void Update()
		{
			Vector2Int firstIndex = TileUtils.FloorUVToIndex(uv, zoom);
			Debug.Log($"firstIndex:{firstIndex}");
			int arrayIndex = 0;
			for (int i = 0; i < tileCountXY.x; ++i)
			{
				for (int j = 0; j < tileCountXY.y; ++j)
				{
					Vector2Int indexOffset = new Vector2Int(i, j);
					Tile_MonoBehaviour tile;
					var key = firstIndex + indexOffset;
					if (!tilesMap.TryGetValue(key, out tile))
					{
						tile = CreateTile(key, $"tile[{arrayIndex}]");
						tilesMap[key] = tile;
					}
					var posOffset = TileUtils.TileOffsetFromUV(uv, zoom);
					tile.transform.localPosition = new Vector3(i - posOffset.x, 0, -j + posOffset.y);
					tile.Keep = true;

					arrayIndex++;
				}
			}			
			foreach (var kv in tilesMap) {
				if (kv.Value.Keep)
				{
					kv.Value.Keep = false;
					continue;
				}
				tileToRemoveArray.Add(kv.Key);
			}

			foreach (var tileToRemove in tileToRemoveArray)
			{
				Tile_MonoBehaviour tile;
				if (tilesMap.TryGetValue(tileToRemove, out tile))
				{
					Destroy(tile.gameObject);
					tilesMap.Remove(tileToRemove);
				}
			}
			tileToRemoveArray.Clear();
		}

		private Tile_MonoBehaviour CreateTile(Vector2Int index, string name)
		{
			GameObject tileObj = GameObject.Instantiate(tileTemplate);
			tileObj.SetActive(true);
			tileObj.name = name;
			Tile_MonoBehaviour tile = tileObj.GetComponent<Tile_MonoBehaviour>();
			tile.zoom = zoom;
			tile.index = index;
			tile.transform.SetParent(transform);
			return tile;
		}
	}


#if UNITY_EDITOR
	[CustomEditor(typeof(TileManager_MonoBehaviour))]
	public class TileManager_Editor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var mgr = target as TileManager_MonoBehaviour;
			if (GUILayout.Button("Compute UV from index"))
			{
				mgr.uv = TileUtils.IndexToUV(mgr.index, mgr.zoom);
			}
			var step = .1f / TileUtils.TileCount(mgr.zoom);
			EditorGUILayout.LabelField($"step:{step:F8}");

			var uv = mgr.uv;
			EditorGUILayout.LabelField($"uv.x:{uv.x:F8}");
			EditorGUILayout.LabelField($"uv.y:{uv.y:F8}");

			var offset = TileUtils.TileOffsetFromUV(uv, mgr.zoom);
			EditorGUILayout.LabelField($"offset.xy:{offset.x:F8},{offset.y:F8}");

			if (GUILayout.Button("+u.x"))
			{
				uv.x += .1f / TileUtils.TileCount(mgr.zoom);
			}
			if (GUILayout.Button("-u.x"))
			{
				uv.x -= .1f / TileUtils.TileCount(mgr.zoom);
			}
			if (GUILayout.Button("+u.y"))
			{
				uv.y += .1f / TileUtils.TileCount(mgr.zoom);
			}
			if (GUILayout.Button("-u.y"))
			{
				uv.y -= .1f / TileUtils.TileCount(mgr.zoom);
			}
			if (GUILayout.Button("ForceUpdate"))
			{
				//mgr.ForceUpdate();
			}

			mgr.uv = uv;
		}
	}
#endif
}
