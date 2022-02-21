using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class WormMeshMaterialBehaviour : MonoBehaviour
{
	private PerlinWorm perlinWorm = null;

	private void Start()
	{
		perlinWorm = WormDataBehaviour.GetPerlinWorm();
	}

	// Update is called once per frame
	void Update()
	{
		Vector4[] vPos = new Vector4[perlinWorm.Count];
		Vector4[] vForward = new Vector4[perlinWorm.Count];
		for (int i = 0; i < vPos.Length; ++i)
		{
			vPos[i] = perlinWorm.GetPosition(i);
			vForward[i] = perlinWorm.GetForward(i);
		}

		var mat = GetComponent<MeshRenderer>().sharedMaterial;
		mat.SetVectorArray("_PositionArray", vPos);
		mat.SetVectorArray("_ForwardArray", vForward);
		mat.SetInt("_ArrayCount", perlinWorm.Count);
	}
}
