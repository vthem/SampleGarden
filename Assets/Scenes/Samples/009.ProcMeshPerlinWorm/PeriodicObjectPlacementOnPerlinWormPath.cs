using System.Collections.Generic;

using UnityEngine;

public class PeriodicObjectPlacementOnPerlinWormPath : MonoBehaviour
{
	public float maxDistance = 100f;
	public float period = 10f;
	public GameObject template = null;

	private PerlinWorm perlinWorm;
	private List<GameObject> objects = new List<GameObject>();

	// Start is called before the first frame update
	private void Start()
	{
		perlinWorm = WormDataBehaviour.GetPerlinWorm();
		objects.Capacity = Mathf.CeilToInt(maxDistance / period);
	}

	// Update is called once per frame
	private void Update()
	{
		var config = perlinWorm.config;

		var cur = Mathf.RoundToInt(config.worldPosZ / period);
		var curPos = cur * period;
		var objIndex = 0;
		while (curPos < config.worldPosZ + maxDistance)
		{
			bool notEnoughObjectAvailable = objIndex == objects.Count;
			GameObject gobj = null;
			if (notEnoughObjectAvailable)
			{
				gobj = GameObject.Instantiate(template);
				objects.Add(gobj);

			}
			else
			{
				gobj = objects[objIndex];
			}
			gobj.SetActive(true);
			gobj.transform.position = perlinWorm.GetPositionRelative(curPos - config.worldPosZ);

			cur++;
			curPos = cur * period;
			objIndex++;
		}

		for (int i = objIndex; i < objects.Count; ++i)
		{
			if (objects[i].activeSelf)
				objects[i].SetActive(false);
		}
	}
}
