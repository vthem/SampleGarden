using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World_Behaviour : MonoBehaviour
{
	public struct GravityData
	{
		public Vector3 direction;
		public Mesh mesh;
	}

	public GameObject[] worldObjectArray;

	public bool TryFindGravityAt(Vector3 wPoint, ref GravityData gravity)
	{
		if (!TryFindGameObjectAt(wPoint, out GameObject worldGameObject))
		{
			return false;
		}

		Mesh mesh = worldGameObject.GetComponent<MeshCollider>().sharedMesh;
		Matrix4x4 localToWorld = worldGameObject.transform.localToWorldMatrix;

		var vertices = mesh.vertices;
		float minDistance = float.MaxValue;
		int minIdx = -1;
		for (int i = 0; i < vertices.Length; ++i)
		{
			var wVertex = localToWorld.MultiplyPoint(vertices[i]);
			var sqDistance = (wVertex - wPoint).sqrMagnitude;
			if (sqDistance < minDistance)
			{
				minDistance = sqDistance;
				minIdx = i;
			}
		}
		if (minIdx < 0)
		{
			return false;
		}

		gravity.direction = -mesh.normals[minIdx];
		gravity.mesh = mesh;

		return true;
	}

	private bool TryFindGameObjectAt(Vector3 wPoint, out GameObject worldGameObject)
	{
		for (int i = 0; i < worldObjectArray.Length; ++i)
		{
			if (!worldObjectArray[i].activeInHierarchy)
			{
				continue;
			}
			if (worldObjectArray[i].TryGetComponent<MeshCollider>(out MeshCollider meshCollider))
			{
				if (meshCollider.bounds.Contains(wPoint))
				{
					worldGameObject = worldObjectArray[i];
					return true;
				}
			}
		}
		worldGameObject = null;
		return false;
	}
}
