
using _009_ProcMeshPerlinWorm;

using UnityEngine;

namespace _010_WormMesh
{
	public class WormMeshMaterialBehaviour : MonoBehaviour
	{
		private PerlinWorm perlinWorm = null;

		private void Start()
		{
			perlinWorm = WormDataBehaviour.GetPerlinWorm();
		}

		// Update is called once per frame
		private void Update()
		{
			Vector4[] vPos = new Vector4[perlinWorm.Count];
			Vector4[] vForward = new Vector4[perlinWorm.Count];
			for (int i = 0; i < vPos.Length; ++i)
			{
				vPos[i] = perlinWorm.GetPosition(i);
				vForward[i] = perlinWorm.GetForward(i);
			}

			Material mat = GetComponent<MeshRenderer>().sharedMaterial;
			mat.SetVectorArray("_PositionArray", vPos);
			mat.SetVectorArray("_ForwardArray", vForward);
			mat.SetInt("_ArrayCount", perlinWorm.Count);
		}
	}

}