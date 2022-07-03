using System.Collections;

using UnityEngine;
using UnityEngine.Networking;

namespace _019_EarthMap {

	public class Tile_MonoBehaviour : MonoBehaviour
	{
		public string apiKey = "e5e7c9f4fdca44c0a925f3af8cbd58fe";
		public string URL = "https://b.tile.thunderforest.com/outdoors/__zoom__/__x__/__y__.png?apiKey=__key__";

		public int zoom = 11;
		public int x = 1017;
		public int y = 739;

		public bool forceGet = false;

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
			u = u.Replace("__x__", x.ToString());
			u = u.Replace("__y__", y.ToString());
			u = u.Replace("__key__", apiKey);
			return u;
		}
	}

}