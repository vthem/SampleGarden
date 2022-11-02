using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World_Behaviour : MonoBehaviour
{
	public struct GravityData
	{
		public Vector3 direction;
		public Mesh mesh;
		public GameObject gameObject;
	}

	public GameObject[] worldObjectArray;

	private void Awake()
	{
		var l = new List<GameObject>();
		for(int i = 0; i < transform.childCount; ++i)
		{
			if (!transform.GetChild(i).gameObject.activeInHierarchy)
			{
				continue;
			}
			l.Add(transform.GetChild(i).gameObject);
		}
		worldObjectArray = l.ToArray();
	}

	public bool TryFindGravityAt(Vector3 wPoint, ref GravityData gravity)
	{
		if (!TryFindGameObjectAt(wPoint, out gravity.gameObject))
		{
			return false;
		}

		Mesh mesh = gravity.gameObject.GetComponent<MeshCollider>().sharedMesh;
		Matrix4x4 localToWorld = gravity.gameObject.transform.localToWorldMatrix;

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
