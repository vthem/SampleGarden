using UnityEngine;

public class WormRelativeTransformSetterBehaviour : MonoBehaviour
{
	[Range(0, 50)] public int wormPosition = 1;
	[Range(0, 50)] public int lookAtDistance = 5;
	private PerlinWorm perlinWorm = null;

	private void Start()
	{
		perlinWorm = WormDataBehaviour.GetPerlinWorm();
	}

	// Update is called once per frame
	private void Update()
	{
		transform.position = perlinWorm.GetPosition(wormPosition);
		Vector3 lookatPoint = perlinWorm.GetPosition(lookAtDistance);
		transform.LookAt(lookatPoint, Vector3.up);
	}
}
