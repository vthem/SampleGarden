using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _013_TesselatedWorm
{
	// https://medium.com/@simonalbou/lusage-de-compute-shaders-sur-unity-un-gain-de-performance-violent-54c1b0f72698
	public class WormDataBehaviour : MonoBehaviour
	{
		public float worldPosZ = 0f;
		public float xSeed = 1.294387493f;
		public float ySeed = 2.394384938f;
		public float perlinScaleZ = 1f;

		public float length = 10f;
		public float stepLength = 1f;

		public ComputeShader computeShader;

		private ComputeBuffer computeBuffer;
		private int indexOfKernel;
		private int Count => Mathf.FloorToInt(length / stepLength);

		private struct WormData
		{
			public float xDeviation;
			public float yDeviation;

			public static int Size()
			{
				return sizeof(float) * 2;
			}
		}
		private WormData[] wormData;


		// Start is called before the first frame update
		void Start()
		{
			indexOfKernel = computeShader.FindKernel("CSMain");
			wormData = new WormData[Count];
			for (int i = 0; i < Count; ++i)
			{
				wormData[i] = new WormData();
			}
		}

		// Update is called once per frame
		void Update()
		{
			if (null == computeBuffer || computeBuffer.count != Count)
			{
				if (computeBuffer != null)
				{
					computeBuffer.Release();
				}
				computeBuffer = new ComputeBuffer(Count, WormData.Size());
			}
			computeBuffer.SetData(wormData);
			// TODO FINISH THIS
		}
	}

}