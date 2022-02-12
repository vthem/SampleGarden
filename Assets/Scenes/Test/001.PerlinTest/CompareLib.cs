
using System.Diagnostics;

using UnityEngine;

namespace _001.Test
{

	public class CompareLib : MonoBehaviour
	{
		// Start is called before the first frame update
		private void Start()
		{
			int count = 1000000;
			{
				Stopwatch sw = Stopwatch.StartNew();
				for (int i = 0; i < count; ++i)
				{
					UnityEngine.Mathf.PerlinNoise(UnityEngine.Random.Range(-100f, 100f), UnityEngine.Random.Range(-100f, 100f));
				}
				UnityEngine.Debug.Log($"Unity elapsed:{sw.ElapsedMilliseconds}");
			}

			{
				Stopwatch sw = Stopwatch.StartNew();
				for (int i = 0; i < count; ++i)
				{
					TSW.Core.Perlin.Noise(UnityEngine.Random.Range(-100f, 100f), UnityEngine.Random.Range(-100f, 100f));
				}
				UnityEngine.Debug.Log($"Keijiro Takahashi elapsed:{sw.ElapsedMilliseconds}");
			}

			{
				Stopwatch sw = Stopwatch.StartNew();
				for (int i = 0; i < count; ++i)
				{
					TSW.Core.Perlin.Noise(UnityEngine.Random.Range(-100f, 100f));
					UnityEngine.Random.Range(-100f, 100f);
				}
				UnityEngine.Debug.Log($"Keijiro Takahashi 1D elapsed:{sw.ElapsedMilliseconds}");
			}
		}

		// Update is called once per frame
		private void Update()
		{

		}
	}
}
