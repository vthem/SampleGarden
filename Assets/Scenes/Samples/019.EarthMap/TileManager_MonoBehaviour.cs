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

		public static Vector3Int FloorUVToIndex(Vector2 uv, int z)
		{
			float tileCount = TileCount(z);
			return new Vector3Int(Mathf.FloorToInt(uv.x * tileCount), Mathf.FloorToInt(uv.y * tileCount), z);
		}

		public static Vector3Int WrapIndex(Vector3Int index)
		{
			int tileCount = Mathf.RoundToInt(TileCount(index.z));
			index.x = index.x.Modulo(tileCount);
			index.y = index.y.Modulo(tileCount);
			return index;
		}

		public static Vector2 IndexToUV(Vector3Int index)
		{
			float max = TileCount(index.z);
			return new Vector2((index.x / max) + (1 / (2 * max)), (index.y / max) + (1 / (2 * max)));
		}

		public static Vector3 TileOffsetFromUV(Vector2 uv, int z)
		{
			var index = FloorUVToIndex(uv, z);
			var roundedUV = IndexToUV(index);
			var gap = uv - roundedUV;
			float tileCount = TileCount(z);
			var tileSize = 1 / tileCount;
			var offset = gap / tileSize;
			return offset;
		}

		public static Vector3 IndexToWorld(Vector3Int index, MapViewport viewport)
		{
			Vector3Int centerIndex = FloorUVToIndex(viewport.rectUV.center, index.z);
			Vector3 pos = new Vector3(index.x - centerIndex.x, 0f, index.y - centerIndex.y);
			pos.x = pos.x * viewport.TileScale * viewport.tileSize;
			pos.y = pos.y * viewport.TileScale * viewport.tileSize;
			return pos;
		}

		public static bool UVToWorld(Vector2 uvPoint, MapViewport viewport, out Vector3 pos)
		{
			if (!viewport.rectUV.ContainsPoint(uvPoint))
			{
				pos = Vector3.zero;
				return false;
			}
			Vector2 uv;

			uv.x = Mathf.InverseLerp(viewport.rectUV.xMin, viewport.rectUV.xMax, uvPoint.x);
			uv.y = Mathf.InverseLerp(viewport.rectUV.yMin, viewport.rectUV.yMax, uvPoint.y);

			uv -= viewport.rectUV.center;

			pos = new Vector3(viewport.tileSize * viewport.InnerTileCount.x * uv.x, 0, viewport.tileSize * viewport.InnerTileCount.y * uv.y);
			return true;
		}
	}

	[System.Serializable]
	public struct MapViewport
	{
		public Rect rectUV;
		public int pixelCount;
		public int pixelPerTile;
		public float tileSize;
		public float TileScale { get; private set; }
		public int ZMin{ get; private set; }
		public float Z { get; private set; }

		public Vector2Int InnerTileCount => Vector2Int.one * (pixelCount / pixelPerTile);

		public static MapViewport Default
		{
			get
			{
				var vp = new MapViewport
				{
					rectUV = new Rect(0, 0, 1, 1),
					pixelCount = 1024,
					pixelPerTile = 256,
					tileSize = 1
				};
				vp.Update();
				return vp;
			}
		}

		// z = log2 vpk
		// z = log2 ( pixelCount / (uv.width * pixelPerTile) )
		// 2^z = pixelCount / (uv.width * pixelPerTile)
		// 2^z * uv.width = pixelCount / pixelPerTile
		// pixelPerTile = pixelCount / (2^z * uv.width) 

		public void Update()
		{
			UpdateZ();
			UpdateScale();
		}

		private void UpdateZ()
		{
			float vpk = pixelCount / (rectUV.width * pixelPerTile);
			Z = Mathf.Log(vpk, 2f);
			ZMin = Mathf.FloorToInt(Z);
		}

		private void UpdateScale()
		{
			TileScale = pixelPerTile / (pixelCount / (Mathf.Pow(2, ZMin) * rectUV.width));
		}

		public void DrawGizmo()
		{
			Vector3 v0, v1, v2, v3;
			TileUtils.UVToWorld(new Vector2(rectUV.xMin, rectUV.yMin), this, out v0);
			TileUtils.UVToWorld(new Vector2(rectUV.xMin, rectUV.yMax), this, out v1);
			TileUtils.UVToWorld(new Vector2(rectUV.xMax, rectUV.yMax), this, out v2);
			TileUtils.UVToWorld(new Vector2(rectUV.xMax, rectUV.yMin), this, out v3);
			Gizmos.DrawLine(v0, v1);
			Gizmos.DrawLine(v1, v2);
			Gizmos.DrawLine(v2, v3);
			Gizmos.DrawLine(v3, v0);
			Gizmos.
		}
	}

	public class TileManager_MonoBehaviour : MonoBehaviour
	{
		public GameObject tileTemplate;
		public Rect UV { get { return viewport.rectUV; } }
		public int PixelCount { get { return viewport.pixelCount; } }
		public int PixelPerTile { get { return viewport.pixelPerTile; } }

		public MapViewport viewport = MapViewport.Default;

		private List<Vector2Int> tileToRemoveArray = new List<Vector2Int>(16);
		private readonly Dictionary<Vector2Int, Tile_MonoBehaviour> tilesMap = new Dictionary<Vector2Int, Tile_MonoBehaviour>();

		private void Awake()
		{
			tileTemplate.SetActive(false);

			if (PixelCount % PixelPerTile != 0)
			{
				Debug.LogError($"ViewportMap > pixelCount:{PixelCount} should be a multiple of pixelPerTile:{PixelPerTile}");
				enabled = false;
			}
		}

		public void Update()
		{
			viewport.Update();

			int tileCount1d = (PixelCount / PixelPerTile) + 2;
			Vector2Int tileCountXY = new Vector2Int(tileCount1d, tileCount1d);

			Vector3Int middleIndex = TileUtils.FloorUVToIndex(UV.center, viewport.ZMin);
			Vector3Int firstIndex = new Vector3Int(middleIndex.x - tileCountXY.x, middleIndex.y - tileCountXY.y, viewport.ZMin);

			Debug.Log($"centerIndex:{firstIndex} middleIndex:{middleIndex}");
			
			int arrayIndex = 0;
			for (int i = 0; i < tileCountXY.x; ++i)
			{
				for (int j = 0; j < tileCountXY.y; ++j)
				{
					Vector3Int indexOffset = new Vector3Int(i, j, 0);
					Tile_MonoBehaviour tile;
					var currentIndex = firstIndex + indexOffset;
					currentIndex = TileUtils.WrapIndex(currentIndex);
					var key = new Vector2Int(currentIndex.x, currentIndex.y);
					if (!tilesMap.TryGetValue(key, out tile))
					{
						tile = CreateTile(currentIndex, $"tile[{arrayIndex}]");
						tilesMap[key] = tile;
					}
					//var posOffset = TileUtils.TileOffsetFromUV(uv, zMin);
					//tile.transform.localPosition = new Vector3(i - posOffset.x, 0, -j + posOffset.y) - new Vector3(tileCountXY.x, 0, -tileCountXY.y) * .5f;
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

		private Tile_MonoBehaviour CreateTile(Vector3Int index, string name)
		{
			GameObject tileObj = GameObject.Instantiate(tileTemplate);
			tileObj.SetActive(true);
			tileObj.name = name;
			Tile_MonoBehaviour tile = tileObj.GetComponent<Tile_MonoBehaviour>();
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

			//var mgr = target as TileManager_MonoBehaviour;
			//if (GUILayout.Button("Compute UV from index"))
			//{
			//	mgr.uv = TileUtils.IndexToUV(mgr.index, mgr.zoom);
			//}
			//var step = .1f / TileUtils.TileCount(mgr.zoom);
			//EditorGUILayout.LabelField($"step:{step:F8}");

			//var uv = mgr.uv;
			//EditorGUILayout.LabelField($"uv.x:{uv.x:F8}");
			//EditorGUILayout.LabelField($"uv.y:{uv.y:F8}");

			//var offset = TileUtils.TileOffsetFromUV(uv, mgr.zoom);
			//EditorGUILayout.LabelField($"offset.xy:{offset.x:F8},{offset.y:F8}");

			//if (GUILayout.Button("+u.x"))
			//{
			//	uv.x += .1f / TileUtils.TileCount(mgr.zoom);
			//}
			//if (GUILayout.Button("-u.x"))
			//{
			//	uv.x -= .1f / TileUtils.TileCount(mgr.zoom);
			//}
			//if (GUILayout.Button("+u.y"))
			//{
			//	uv.y += .1f / TileUtils.TileCount(mgr.zoom);
			//}
			//if (GUILayout.Button("-u.y"))
			//{
			//	uv.y -= .1f / TileUtils.TileCount(mgr.zoom);
			//}
			//if (GUILayout.Button("zoom+"))
			//{
			//	mgr.zoom += 1;				
			//}
			//if (GUILayout.Button("zoom-"))
			//{
			//	mgr.zoom -= 1;
			//}
			//mgr.zoom = Mathf.Clamp(mgr.zoom, 0, 22);
			//if (GUILayout.Button("ForceUpdate"))
			//{
			//	//mgr.ForceUpdate();
			//}

			//mgr.uv = uv;
		}
	}
#endif
}
