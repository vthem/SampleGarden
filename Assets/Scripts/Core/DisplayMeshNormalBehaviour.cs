
using UnityEngine;

public class DisplayMeshNormalBehaviour : MonoBehaviour
{
	public float normalLength = 1f;

	private void OnDrawGizmosSelected()
	{
		MeshFilter meshFilter = GetComponent<MeshFilter>();
		if (!meshFilter)
		{
			return;
		}

		Mesh mesh = meshFilter.sharedMesh;
		if (!mesh)
		{
			return;
		}

		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.color = Color.yellow;
		Vector3[] verts = mesh.vertices;
		Vector3[] normals = mesh.normals;
		int len = mesh.vertexCount;

		for (int i = 0; i < len; i++)
		{
			Gizmos.DrawLine(verts[i], verts[i] + normals[i] * normalLength);
		}
	}

}
