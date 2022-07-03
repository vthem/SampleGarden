using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _019_EarthMap
{
	public class TileManager_MonoBehaviour : MonoBehaviour
	{
		public Vector2Int tileCount = new Vector2Int(2, 2);

		public int zoom = 11;
		public int x = 1017;
		public int y = 739;

		public GameObject tileTemplate;

		public float u = 0f;
		public float v = 0f;

		private void Awake()
		{
			tileTemplate.SetActive(false);
		}

		// Start is called before the first frame update
		void Start()
		{
			for (int i = 0; i < tileCount.x; ++i)
			{
				for (int j = 0; j < tileCount.y; ++j)
				{
					var newTile = GameObject.Instantiate(tileTemplate);
					newTile.SetActive(true);
					var tile = newTile.GetComponent<Tile_MonoBehaviour>();
					tile.zoom = zoom;
					tile.x = x + i;
					tile.y = y + j;
					tile.transform.localPosition = new Vector3(i, 0, -j);
				}
			}
		}

		// Update is called once per frame
		void Update()
		{
			float max = Mathf.Pow(2f, zoom + 1f);
			u = x / max;
			v = y / max;
		}
	}
}
