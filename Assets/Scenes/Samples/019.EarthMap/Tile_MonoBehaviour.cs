using System.Collections;

using UnityEngine;
using UnityEngine.Networking;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _019_EarthMap
{
#if false
	internal class TileCache : CustomYieldInstruction
	{
		public string URL { get; private set; } = string.Empty;

		public bool IsValid { get; private set; } = false;

		public override bool keepWaiting => false;

		public TileCache(Vector2Int index, int zoom)
		{
			// read cache should be async
			var path = Path.Combine(Application.dataPath, "TileCache");
			path = Path.Combine(path, $"{index.x}.{index.y}.{zoom}.png");
			if (File.Exists(path))
			{
				var uri = new System.Uri(path);
				URL = uri.AbsoluteUri;
				IsValid = true;
			}
		}

		public static void Save(Vector2Int index, int zoom, Texture2D texture)
		{
			//then Save To Disk as PNG
			byte[] bytes = texture.EncodeToPNG();
			var dirPath = Application.dataPath + "/../SaveImages/";
			if (!Directory.Exists(dirPath))
			{
				Directory.CreateDirectory(dirPath);
			}
			File.WriteAllBytes(GetPath(index, zoom), bytes);
		}

		public static string GetPath(Vector2Int index, int zoom)
		{
			var path = Path.Combine(Application.dataPath, "TileCache");
			return Path.Combine(path, $"{index.x}.{index.y}.{zoom}.png");
		}
	}


	internal class TileDownloader : IEnumerator
	{
		private enum State
		{
			None = -1,
			ReadCache,
			Download
		}

		private readonly int zoom = 11;
		private Vector2Int index = new Vector2Int(1017, 739);
		private State state = State.None;
		private TileCache cache;
		private UnityWebRequest www;
		private UnityWebRequestAsyncOperation wwwOp;

		IEnumerator Run()
		{
			switch (state)
			{
				case State.None:
					yield return null;
					break;
				case State.ReadCache:
					yield return cache;
					break;
				case State.Download:
					yield return wwwOp;
					break;
			}
			yield return null;
		}

		public override bool keepWaiting
		{
			get
			{
				switch (state)
				{
					case State.None:
						return false;
					case State.ReadCache:
						if (!cache.keepWaiting)
						{
							state = State.Download;
							string url = BuildURL();
							www = UnityWebRequestTexture.GetTexture(url);
							wwwOp = www.SendWebRequest();
							return true;
						}
						return true;
					case State.Download:
						return wwwOp.c
				}
				return false;
			}
		}

		public object Current => throw new System.NotImplementedException();

		public void Reset() { }

		public TileDownloader(Vector2Int index, int zoom)
		{
			// read cache should be async
			cache = new TileCache(index, zoom);
			state = State.ReadCache;
		}

		private string BuildURL()
		{
			return $"https://b.tile.thunderforest.com/outdoors/{zoom}/{index.x}/{index.y}.png?apikey=e5e7c9f4fdca44c0a925f3af8cbd58fe";
		}

		public bool MoveNext()
		{
			throw new System.NotImplementedException();
		}
	}
#endif
	[ExecuteInEditMode]
	public class Tile_MonoBehaviour : MonoBehaviour
	{
		public string apiKey = "e5e7c9f4fdca44c0a925f3af8cbd58fe";
		public string URL = "https://b.tile.thunderforest.com/outdoors/__zoom__/__x__/__y__.png?apikey=__key__";

		public Vector3Int index = new Vector3Int(1017, 739, 11);

		public bool forceGet = false;

		public MapViewport viewport;

		public bool Keep { get; set; } = false;

		public Texture2D texture = null;

		public string HasError { get; private set; } = null;
		private Coroutine getRoutine;


		// Start is called before the first frame update
		private void Start()
		{
			GetTextureAsync();
		}

		// Update is called once per frame
		private void Update()
		{
			HasError = null;

			viewport.Update();

			Vector3 position;
			if (TileUtils.IndexToWorld(index, viewport, out position))
			{
				transform.position = position;
			}
			else
			{
				HasError = "fail to find world position";
			}

			if (forceGet)
			{
				forceGet = false;

				GetTextureAsync();
			}
		}

		private void GetTextureAsync()
		{
#if UNITY_EDITOR
			if (!EditorApplication.isPlaying)
			{
				return;
			}
#endif
			if (getRoutine != null)
			{
				StopCoroutine(getRoutine);
				getRoutine = null;
			}
			getRoutine = StartCoroutine(GetTexture());
		}

		private IEnumerator GetTexture()
		{
			string getURL = BuilURL();
			Debug.Log($"GET {getURL}");
			UnityWebRequest www = UnityWebRequestTexture.GetTexture(getURL);
			yield return www.SendWebRequest();

			if (www.result != UnityWebRequest.Result.Success)
			{
				Debug.Log(www.error);
			}
			else
			{
				texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
				var mat = GetComponent<MeshRenderer>().material;
				mat.SetTexture("_BaseMap", texture);
			}
		}

		private string BuilURL()
		{
			string u = URL;
			u = u.Replace("__zoom__", index.z.ToString());
			u = u.Replace("__x__", index.x.ToString());
			u = u.Replace("__y__", index.y.ToString());
			u = u.Replace("__key__", apiKey);
			return u;
		}

		private void OnDrawGizmosSelected()
		{
			viewport.DrawGizmo();
		}
	}
#if UNITY_EDITOR
	[CustomEditor(typeof(Tile_MonoBehaviour))]
	public class Tile_Editor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var tile = target as Tile_MonoBehaviour;

			var zTileCount = (int)TileUtils.TileCount(tile.index.z);

			Vector2Int position = Vector2Int.zero, size = Vector2Int.zero;
			TileUtils.FloorUVToIndex(tile.viewport.rectUV.position, tile.index.z, ref position);
			TileUtils.FloorUVToIndex(tile.viewport.rectUV.size, tile.index.z, ref size);

			Vector2Int half = new Vector2Int(Mathf.RoundToInt(tile.viewport.rectUV.size.x * zTileCount * .5f), Mathf.RoundToInt(tile.viewport.rectUV.size.y * zTileCount * .5f));
			Vector2Int center = position + half;

			GUILayout.Label("Viewport Info");
			center = EditorGUILayout.Vector2IntField("center", center);
			size = EditorGUILayout.Vector2IntField("size", size);

			size.x = Mathf.Clamp(size.x, 0, zTileCount);
			size.y = Mathf.Clamp(size.y, 0, zTileCount);
			tile.viewport.rectUV.size = TileUtils.IndexToUV(size, tile.index.z);

			half = new Vector2Int(Mathf.RoundToInt(tile.viewport.rectUV.size.x * zTileCount * .5f), Mathf.RoundToInt(tile.viewport.rectUV.size.y * zTileCount * .5f));

			center.x = Mathf.Clamp(center.x, half.x, zTileCount - half.x);
			center.y = Mathf.Clamp(center.y, half.y, zTileCount - half.y);

			position = center - size / 2;

			tile.viewport.rectUV.position = TileUtils.IndexToUV(position, tile.index.z);

			GUILayout.Label($"Viewport z:{tile.viewport.Z} zMin:{tile.viewport.ZMin}");
			GUILayout.Label($"Viewport w:{tile.viewport.rectUV.width * Mathf.Pow(2, tile.index.z)} size:{size}");
			GUILayout.Label($"Viewport UV rect min width for z:{tile.index.z} -> {tile.viewport.MinWidthForZ(tile.index.z)} -> {tile.viewport.MinWidthForZ(tile.index.z) * zTileCount}");
			GUILayout.Space(10f);


			if (!string.IsNullOrEmpty(tile.HasError))
			{
				GUILayout.Label("Error:");
				GUILayout.Label(tile.HasError);
			}
		}
	}
#endif
}