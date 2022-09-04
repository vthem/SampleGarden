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
			return Mathf.Pow(2f, zoom);
		}

		public static Vector3Int FloorUVToIndex(Vector2 uv, int z)
		{
			float tileCount = TileCount(z);
			return new Vector3Int(Mathf.FloorToInt(uv.x * tileCount), Mathf.FloorToInt(uv.y * tileCount), z);
		}

		public static void FloorUVToIndex(Vector2 uv, int z, ref Vector2Int index)
		{
			float tileCount = TileCount(z);
			index.x = Mathf.FloorToInt(uv.x * tileCount);
			index.y = Mathf.FloorToInt(uv.y * tileCount); 
		}

		public static Vector3Int WrapIndex(Vector3Int index)
		{
			int tileCount = Mathf.RoundToInt(TileCount(index.z));
			index.x = index.x.Modulo(tileCount);
			index.y = index.y.Modulo(tileCount);
			return index;
		}

		public static Vector2 IndexToUVCenter(Vector3Int index)
		{
			float max = TileCount(index.z);
			return new Vector2((index.x / max) + (1 / (2 * max)), (index.y / max) + (1 / (2 * max)));
		}

		public static Vector2 IndexToUV(Vector3Int index)
		{
			float max = TileCount(index.z);
			return new Vector2(index.x / max, index.y / max);
		}

		public static Vector2 IndexToUV(Vector2Int index, int z)
		{
			float max = TileCount(z);
			return new Vector2(index.x / max, index.y / max);
		}

		public static Vector3 TileOffsetFromUV(Vector2 uv, int z)
		{
			var index = FloorUVToIndex(uv, z);
			var roundedUV = IndexToUVCenter(index);
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

		public static bool IndexToWorld(Vector3Int index, MapViewport viewport, out Vector3 pos)
		{
			Vector2 uv = IndexToUVCenter(index);
			return UVToWorld(uv, viewport, out pos);
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

			Vector2 size = new Vector2(viewport.tileSize * viewport.InnerTileCount.x, viewport.tileSize * viewport.InnerTileCount.y);
			var minWorld = -size * 0.5f;
			var maxWorld = size * 0.5f;

			pos = new Vector3(Mathf.Lerp(minWorld.x, maxWorld.x, uv.x), 0, Mathf.Lerp(minWorld.y, maxWorld.y, uv.y));

			pos.x -= viewport.tileSize * .5f;
			pos.z -= viewport.tileSize * .5f;

			pos.z *= -1;
			return true;
		}
	}

	[System.Serializable]
	public class MapViewport
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

		// float vpk = pixelCount / (rectUV.width * pixelPerTile);
		// Z = Mathf.Log(vpk, 2f);
		// 2^z = pixelCount / (uv.width * pixelPerTile)
		// uv.width  = pixelCount / (2^z * pixelPerTile)

		public float MinWidthForZ(int z)
		{
			return pixelCount / (Mathf.Pow(2, z) * pixelPerTile);
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
			float d = InnerTileCount.x * .5f * TileScale * tileSize;
			v0 = new Vector3(-d, 0, -d);
			v1 = new Vector3(-d, 0, d);
			v2 = new Vector3(d, 0, d);
			v3 = new Vector3(d, 0, -d);

			Gizmos.DrawLine(v0, v1);
			Gizmos.DrawLine(v1, v2);
			Gizmos.DrawLine(v2, v3);
			Gizmos.DrawLine(v3, v0);
		}

		public void DrawInspector(int zRef)
		{
			var zTileCount = (int)TileUtils.TileCount(zRef);

			Vector2Int position = Vector2Int.zero, size = Vector2Int.zero;
			TileUtils.FloorUVToIndex(rectUV.position, zRef, ref position);
			TileUtils.FloorUVToIndex(rectUV.size, zRef, ref size);

			Vector2Int half = new Vector2Int(Mathf.RoundToInt(rectUV.size.x * zTileCount * .5f), Mathf.RoundToInt(rectUV.size.y * zTileCount * .5f));
			Vector2Int center = position + half;

			GUILayout.Label("Viewport Info");
			center = EditorGUILayout.Vector2IntField("center", center);
			size = EditorGUILayout.Vector2IntField("size", size);

			size.x = Mathf.Clamp(size.x, 0, zTileCount);
			size.y = Mathf.Clamp(size.y, 0, zTileCount);
			rectUV.size = TileUtils.IndexToUV(size, zRef);

			half = new Vector2Int(Mathf.RoundToInt(rectUV.size.x * zTileCount * .5f), Mathf.RoundToInt(rectUV.size.y * zTileCount * .5f));

			center.x = Mathf.Clamp(center.x, half.x, zTileCount - half.x);
			center.y = Mathf.Clamp(center.y, half.y, zTileCount - half.y);

			position = center - size / 2;

			rectUV.position = TileUtils.IndexToUV(position, zRef);

			GUILayout.Label($"Viewport z:{Z} zMin:{ZMin}");
			GUILayout.Label($"Viewport w:{rectUV.width * Mathf.Pow(2, zRef)} size:{size}");
			GUILayout.Label($"Viewport UV rect min width for z:{zRef} -> {MinWidthForZ(zRef)} -> {MinWidthForZ(zRef) * zTileCount}");
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
			tile.viewport = viewport;
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
