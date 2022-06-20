using System.Collections;
using System.Collections.Generic;

using Unity.Collections;

using UnityEngine;

[ExecuteInEditMode]
public class TextureSampler : MonoBehaviour
{
	public Texture2D tex;
	public Color color_FromGet;
	public Color color_FromArraySampling;
	[Range(0, 1)] public float u = 0;
	[Range(0, 1)] public float v = 0;
	public Color32[] colors = new Color32[0];
	public Vector2Int pixelIndexRounded;

    // Update is called once per frame
    void Update()
	{
		if (!tex)
			return;

		color_FromGet = tex.GetPixelBilinear(u, v);

		NativeArray<Color32> colorArray = tex.GetRawTextureData<Color32>();
		color_FromArraySampling = Utils.SampleColorFromNativeArray(new Vector2(u, v), colorArray, new Vector2Int(tex.width, tex.height));

		if (colors.Length != colorArray.Length)
		{
			colors = new Color32[colorArray.Length];
			for (int i = 0; i < colorArray.Length; ++i)
			{
				colors[i] = colorArray[i];
			}
		}
	}
}
