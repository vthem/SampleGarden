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

	private Vector3[] _positionArray;
	private Vector3[] _upArray;
	private Vector3[] _forwardArray;
	private Vector3 _first;
	private Vector3 _last;

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

	public void BuildPointsArray()
	{
		int count = Mathf.FloorToInt(config.length / config.stepLength);
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
}
