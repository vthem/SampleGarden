using System;
using UnityEngine;

namespace _016_TerraGenCPU
{
	[ExecuteInEditMode]
	public class WorldPerlinVertexModifierBehaviour : VertexModifierBehaviourBase
	{
		public override bool Initialize()
		{
			if (!base.Initialize())
				return false;

			//perlinMatrix = Matrix4x4.TRS(Vector3.one, Quaternion.identity, Vector3.one * perlinScale);

			return true;
		}

		public override Vector3 Vertex(int x, int z)
		{
			float xVal = xStart + x * xDelta;
			float zVal = zStart + z * zDelta;
			//var v = perlinMatrix.MultiplyPoint(new Vector3(xVal, 0, zVal));
			var v = transform.TransformPoint(new Vector3(xVal, 0, zVal));
			return new Vector3(xVal, Mathf.PerlinNoise(v.x , v.z), zVal);
		}

		//public float PerlinScale { get => perlinScale; set => perlinScale = value; }
		//public Vector3 LocalPosition { get => localPosition; set => localPosition = value; }

		#region private
		//[SerializeField] protected float perlinScale;
		//[SerializeField] protected Vector3 localPosition;

		//protected Matrix4x4 perlinMatrix;

		private void Update()
		{
			//LocalPosition = transform.position;
		}

		#endregion // private
	}

}