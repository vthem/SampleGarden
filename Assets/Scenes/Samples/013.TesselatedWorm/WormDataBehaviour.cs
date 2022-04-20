using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace _013_TesselatedWorm
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

		[Range(10, 1000)]
		public int gizmoPointCount = 1000;

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
				return new Vector3(wormData[index].xDeviation, wormData[index].yDeviation, index * stepLength);
			float dec = fIndex - index;
			Vector3 v1 = new Vector3(wormData[index].xDeviation, wormData[index].yDeviation, index * stepLength); ;
			Vector3 v2 = new Vector3(wormData[index + 1].xDeviation, wormData[index + 1].yDeviation, (index + 1) * stepLength); ;
			return Vector3.Lerp(v1, v2, dec);
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


		// Start is called before the first frame update
		private void OnEnable()
		{
			indexOfKernel = computeShader.FindKernel("CSMain");
			wormData = new WormData[Mathf.FloorToInt(length / stepLength)];
			for (int i = 0; i < wormData.Length; ++i)
			{
				wormData[i] = new WormData();
			}
		}

		// Update is called once per frame
		void Update()
		{
			if (indexOfKernel < 0)
				return;

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
			computeShader.Dispatch(indexOfKernel, 16, 16, 1);
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

			for (int i = 0; i < gizmoPointCount; ++i)
			{

				//			{
				////float radius = Mathf.PerlinNoise(0f, i * config.step + config.worldPosZ + config.radiusOffset);
				//Gizmos.DrawLine(GetPosition(i - 1), GetPosition(i));
				//Vector3 forward = GetForward(i);
				//Vector3 up = GetUp(i);
				GizmoDrawCircle(Vector3.forward, Vector3.up, GetPosition(i / (float)(gizmoPointCount - 1)), 1, 16);
			}
		}
	}
}