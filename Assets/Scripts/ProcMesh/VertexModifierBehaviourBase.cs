using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexModifierBehaviourBase : MonoBehaviour, IVertexModifier
{
	public float XSize { get => xSize; set { RequireRebuild |= value.SetTo(ref xSize); } }
	public float ZSize { get => zSize; set { RequireRebuild |= value.SetTo(ref zSize); } }
	public int Lod { get => lod; set { RequireRebuild |= value.SetTo(ref lod); } }

	public virtual bool Initialize()
	{
		xStart = -xSize * 0.5f;
		zStart = -zSize * 0.5f;
		xDelta = xSize / (float)(VertexCount1D - 1);
		zDelta = zSize / (float)(VertexCount1D - 1);
		return true;
	}

	public virtual Vector3 Vertex(int x, int z)
	{
		float xVal = xStart + x * xDelta;
		float zVal = zStart + z * zDelta;
		return new Vector3(xVal, 0, zVal);
	}

	public virtual Vector3 Normal(int x, int z)
	{
		return Vector3.up;
	}

	public bool RequireRebuild { get; set; }
	public bool RequireUpdate { get; set; }

	public int VertexCount1D => VertexModifier.ComputeVertexCount1D(lod);

	public int VertexCount2D
	{
		get
		{
			var vc1d = VertexCount1D;
			return vc1d * vc1d;
		}
	}

	public int IndiceCount
	{
		get
		{
			var vc1d = VertexCount1D;
			return (vc1d - 1) * (vc1d - 1) * 2 * 3;
		}
	}

	[SerializeField] protected float xSize = 1f;
	[SerializeField] protected float zSize = 1f;
	[SerializeField][Range(0, 7)] protected int lod = 0;

	protected float xDelta;
	protected float zDelta;
	protected float xStart;
	protected float zStart;
}
