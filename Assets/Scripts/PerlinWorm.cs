using UnityEngine;


[System.Serializable]
public class PerlinWormConfig
{
	public float worldPosZ = 0f;
	public float xyOffset = 1.294387493f;
	public float radiusOffset = 2.397987f;
	public float wormRadius = 0.1f;
	public float deviation = 1f;
	public float perlinScaleZ = 1f;

	public float length = 10f;
	public float stepLength = 1f;
}

public class PerlinWorm
{
	public PerlinWormConfig config;

	public int Count => _positionArray.Length;

	public Vector3 GetPosition(int index)
	{
		return _positionArray[index];
	}

	public Vector3 GetUp(int index)
	{
		return _upArray[index];
	}

	public Vector3 GetForward(int index)
	{
		return _forwardArray[index];
	}

	public Vector3 GetPosition(float normalizedLength)
	{		
		return GetInterpolatedValue(_positionArray, normalizedLength);
	}

	public Vector3 GetUp(float normalizedLength)
	{
		return GetInterpolatedValue(_upArray, normalizedLength);
	}

	public Vector3 GetForward(float normalizedLength)
	{
		return GetInterpolatedValue(_forwardArray, normalizedLength);
	}

	public void Update()
	{
		int count = Mathf.CeilToInt(config.length / config.stepLength);
		if (count < 1)
		{
			return;
		}

		_positionArray = new Vector3[count];
		_upArray = new Vector3[count];
		_forwardArray = new Vector3[count];

		_first = GetPoint(-config.stepLength + config.worldPosZ, -config.stepLength);
		_last = GetPoint(config.stepLength + config.worldPosZ, config.stepLength * count);
		for (int i = 0; i < count; ++i)
		{
			_positionArray[i] = GetPoint(i * config.stepLength + config.worldPosZ, i * config.stepLength);
		}

		for (int i = 1; i < count - 1; ++i)
		{
			Vector3 forward = (_positionArray[i + 1] - _positionArray[i - 1]).normalized;
			Vector3 up = Vector3.Cross(forward, Vector3.right);
			_forwardArray[i] = forward;
			_upArray[i] = up;
		}

		{ // 0
			Vector3 next = _last;
			if (count > 2)
			{
				next = _positionArray[1];
			}
			Vector3 forward = (next - _first).normalized;
			Vector3 up = Vector3.Cross(forward, Vector3.right);
			_forwardArray[0] = forward;
			_upArray[0] = up;
		}

		{ // count-1
			Vector3 prev = _first;
			if (count > 2)
			{
				prev = _positionArray[1];
			}
			Vector3 forward = (_last - prev).normalized;
			Vector3 up = Vector3.Cross(forward, Vector3.right);
			_forwardArray[count - 1] = forward;
			_upArray[count - 1] = up;
		}
	}

#if UNITY_EDITOR
	public void DrawGizmos()
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

		for (int i = 1; i < Count; ++i)
		{
			//float radius = Mathf.PerlinNoise(0f, i * config.step + config.worldPosZ + config.radiusOffset);
			Gizmos.DrawLine(GetPosition(i - 1), GetPosition(i));
			Vector3 forward = GetForward(i);
			Vector3 up = GetUp(i);
			GizmoDrawCircle(forward, up, GetPosition(i), config.wormRadius, 20);
		}
	}
#endif

	#region private
	private Vector3[] _positionArray;
	private Vector3[] _upArray;
	private Vector3[] _forwardArray;
	private Vector3 _first;
	private Vector3 _last;

	private Vector3 GetInterpolatedValue(Vector3[] values, float normalizedLength)
	{
		float fIndex = Mathf.Lerp(0, values.Length - 1, normalizedLength);
		//float length = normalizedLength * config.length;
		int index = Mathf.FloorToInt(fIndex);
		if (index >= values.Length - 1)
			return values[index];
		float dec = fIndex - index;
		return Vector3.Lerp(values[index], values[index + 1], dec);
	}
	private Vector3 GetPoint(float zWorld, float zLocal)
	{
		float x = Perlin(zWorld * config.perlinScaleZ) * config.deviation;
		float y = Perlin((zWorld + config.xyOffset) * config.perlinScaleZ) * config.deviation;
		return new Vector3(x, y, zLocal);
	}

	private static float Perlin(float t)
	{
		return (Mathf.PerlinNoise(0f, t) * 2) - 1f;
	}
	#endregion
}
