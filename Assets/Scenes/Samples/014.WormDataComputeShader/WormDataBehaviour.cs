using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace _014_WormDataComputeShader
{
	// https://medium.com/@simonalbou/lusage-de-compute-shaders-sur-unity-un-gain-de-performance-violent-54c1b0f72698

	[ExecuteInEditMode]
	public class WormDataBehaviour : MonoBehaviour
	{
		public float zWorld = 0f;
		public int seed = 1;
		public float xyPeriodScale = 1f;

		public float length = 10f;
		public float stepLength = 1f;

		public ComputeShader computeShader;

		private ComputeBuffer computeBuffer;
		private int indexOfKernel = -1;
		private int Count => wormData.Length;
		private WormData[] wormData;

		public Vector3 GetPosition(float t)
		{
			int lastIndex = wormData.Length - 1;
			float fIndex = Mathf.Lerp(0, lastIndex, t);
			int index = Mathf.FloorToInt(fIndex);
			if (index >= lastIndex)
				return new Vector3(wormData[index].xDeviation, wormData[index].yDeviation, t * length);
			float dec = fIndex - index;
			float x = Mathf.Lerp(wormData[index].xDeviation, wormData[index + 1].xDeviation, dec);
			float y = Mathf.Lerp(wormData[index].yDeviation, wormData[index + 1].yDeviation, dec);
			return new Vector3(x, y, t * length);
		}

		private struct WormData
		{
			public float xDeviation;
			public float yDeviation;
			public float radius;

			public static int Size()
			{
				return sizeof(float) * 3;
			}
		}

		int sampleCount = 32 * 32;

		// Update is called once per frame
		void Update()
		{
			if (indexOfKernel < 0)
			{
				indexOfKernel = computeShader.FindKernel("CSMain");
			}

			stepLength = Mathf.Max(0.001f, stepLength);

			
			if (wormData == null || wormData.Length != sampleCount)
			{
				wormData = new WormData[sampleCount];
				for (int i = 0; i < wormData.Length; ++i)
				{
					wormData[i] = new WormData();
				}
			}

			if (null == computeBuffer || computeBuffer.count != wormData.Length)
			{
				if (computeBuffer != null)
				{
					computeBuffer.Release();
				}
				computeBuffer = new ComputeBuffer(wormData.Length, WormData.Size());
			}
			computeBuffer.SetData(wormData);
			computeShader.SetBuffer(indexOfKernel, "_wormData", computeBuffer);
			computeShader.SetInt("_seed", seed);
			computeShader.SetFloat("_xyPeriodScale", xyPeriodScale);
			computeShader.SetFloat("_zWorld", zWorld);
			computeShader.Dispatch(indexOfKernel, 32, 32, 1);
			computeBuffer.GetData(wormData);
		}

		private void OnDisable()
		{
			indexOfKernel = -1;
			wormData = null;
			if (computeBuffer != null && computeBuffer.IsValid())
			{
				computeBuffer.Release();
			}
		}

		private void OnDrawGizmos()
		{
			if (wormData == null)
				return;

			void GizmoDrawCircle(Vector3 forward, Vector3 up, Vector3 position, float radius, int segmentCount)
			{
				Vector3 prev = Vector3.Cross(forward, up);
				Quaternion rot = Quaternion.AngleAxis(360f / segmentCount, forward);
				for (int i = 0; i < segmentCount; ++i)
				{
					Vector3 cur = rot * prev;
					Gizmos.DrawLine(position + prev * radius, position + cur * radius);
					prev = cur;
				}
			}

			int sCount = Mathf.FloorToInt(length / stepLength);
			sCount = Mathf.Min(sampleCount, sCount);
			for (int i = 0; i < sCount; ++i)
			{

				//			{
				////float radius = Mathf.PerlinNoise(0f, i * config.step + config.worldPosZ + config.radiusOffset);
				//Gizmos.DrawLine(GetPosition(i - 1), GetPosition(i));
				//Vector3 forward = GetForward(i);
				//Vector3 up = GetUp(i);
				GizmoDrawCircle(Vector3.forward, Vector3.up, GetPosition(i / (float)(sCount - 1)), 1, 16);
			}
		}
	}
}