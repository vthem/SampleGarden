using System.Collections;

using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _019_EarthMap {

	[System.Serializable]
	public struct Vector2d
	{
		public double x;
		public double y;
	}

	public class Tile_MonoBehaviour : MonoBehaviour
	{
		public string apiKey = "e5e7c9f4fdca44c0a925f3af8cbd58fe";
		public string URL = "https://b.tile.thunderforest.com/outdoors/__zoom__/__x__/__y__.png?apikey=__key__";

		public int zoom = 11;
		public Vector2Int index = new Vector2Int(1017, 739);
		public bool useUV = false;
		public Vector2 uv;

		public bool forceGet = false;

		public bool Keep { get; set; } = false;

		public Texture2D texture = null;
		private Coroutine getRoutine;	


		// Start is called before the first frame update
		private void Start()
		{
			getRoutine = StartCoroutine(GetTexture());
		}

		// Update is called once per frame
		private void Update()
		{
			if (forceGet)
			{
				forceGet = false;
				
				if (getRoutine != null)
				{
					StopCoroutine(getRoutine);
					getRoutine = null;
				}
				getRoutine = StartCoroutine(GetTexture());
			}
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
			u = u.Replace("__zoom__", zoom.ToString());
			if (useUV)
			{
				index = TileUtils.FloorUVToIndex(uv, zoom);
			}
			u = u.Replace("__x__", index.x.ToString());
			u = u.Replace("__y__", index.y.ToString());
			u = u.Replace("__key__", apiKey);
			return u;
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
			if (GUILayout.Button("Compute UV from index"))
			{
				tile.uv = TileUtils.IndexToUV(tile.index, tile.zoom);
			}
		}
	}
#endif
}