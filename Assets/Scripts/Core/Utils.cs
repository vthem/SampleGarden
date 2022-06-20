#if UNITY_EDITOR
using Unity.Collections;

using UnityEditor;
#endif

using UnityEngine;

public static class Utils
{
    public static int Modulo(this int n, int range)
    {
        return (n % range + range) % range;
    }

	public static float Modulo(this float n, float range)
	{
		return (n % range + range) % range;
	}

	public static bool SetTo<T>(this T src, ref T dst) where T : struct, System.IEquatable<T>
	{
		bool diff = src.Equals(dst);
		dst = src;
		return diff;
	}

    public static float Remap(this float value, float fromBegin, float fromEnd, float toBegin, float toEnd)
    {
        return (value - fromBegin) / (fromEnd - fromBegin) * (toEnd - toBegin) + toBegin;
    }

    public static int GetArrayIdx(int x, int y, int xCount, int yCount)
    {
        if (x < 0 || x >= xCount) return -1;
        if (y < 0 || y >= yCount) return -1;
        return x + y * xCount;
    }

    public static int GetArrayIdx(Vector2Int v, Vector2Int count)
    {
        if (v.x < 0 || v.x >= count.x) return -1;
        if (v.y < 0 || v.y >= count.y) return -1;
        return v.x + v.y * count.x;
    }

	public static int GetArrayIdxClamp(Vector2Int v, Vector2Int count)
	{
		v.x = Mathf.Clamp(v.x, 0, count.x);
		v.y = Mathf.Clamp(v.y, 0, count.y);
		return v.x + v.y * (count.x);
	}

	public static Vector2Int GetXYFromIndex(int index, int xCount)
	{
		Vector2Int v = new Vector2Int(); ;
		v.x = index % xCount;
		v.y = (index - v.x) / xCount;
		return v;
	}

	public static Color32 SampleColorFromNativeArray(Vector2 uv, NativeArray<Color32> colorArray, Vector2Int textureSize)
	{
		Vector2Int pixelMaxIndex = textureSize - Vector2Int.one;
		Vector2Int pixelCount = textureSize;
		Vector2 pixelIndex = pixelMaxIndex * new Vector2(uv.x, uv.y);
		Vector2Int pixelIndexRounded = new Vector2Int(Mathf.RoundToInt(pixelIndex.x), Mathf.RoundToInt(pixelIndex.y));
		int idx = Utils.GetArrayIdxClamp(pixelIndexRounded, pixelCount);
		return colorArray[idx];
	}
}

public static class UnityExt
{
    public static T SafeGetComponent<T>(this MonoBehaviour monoBehaviour) where T : Component
    {
        T comp = monoBehaviour.GetComponent<T>();
        if (!comp)
        {
            comp = monoBehaviour.gameObject.AddComponent<T>();
        }
        return comp;
    }

	public static void SafeDestroy(this Object obj)
	{
		if (!obj)
		{
			return;
		}
#if UNITY_EDITOR
		if (EditorApplication.isPlaying)
		{
			Object.Destroy(obj);
		}
		else
		{
			Object.DestroyImmediate(obj);
		}
#else
		Object.Destroy(obj);
#endif
	}
}