using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DisplayMeshNormals_Behaviour : MonoBehaviour
{
	public float length = 1f;
	public Color color;
	public int debugVertexCount = 0;

    // Update is called once per frame
    void Update()
    {
		var mesh = GetComponent<MeshFilter>().sharedMesh;

		var vertices = mesh.vertices;
		var normals = mesh.normals;

		debugVertexCount = vertices.Length;

		for (int i = 0; i < vertices.Length; ++i)
		{
			var vWorld = transform.TransformPoint(vertices[i]);
			Debug.DrawLine(vWorld, vWorld + normals[i] * length, color);
		}
    }
}
