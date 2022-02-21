using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VertexShaderMaterialBehaviour : MonoBehaviour
{
	public Vector3 value;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		var mat = GetComponent<MeshRenderer>().sharedMaterial;
		Vector4[] vArray = new Vector4[1];
		vArray[0] = value;
		mat.SetVectorArray("_PositionArray", vArray);
		mat.SetInt("_PositionArrayCount", 1);
    }
}
