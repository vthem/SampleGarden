using UnityEngine;

public class Racer
{
	public Transform transform = null;
	public float speed = .1f;

	public void Update()
	{
		if (!transform)
		{
			return;
		}

		Vector3 pos = transform.localPosition;
		pos.z += speed;
		transform.localPosition = pos;
	}
}

public class World
{
	public Transform transform = null;

	public void Update()
	{
		if (!transform)
		{
			return;
		}
	}
}

public class MainLoop : MonoBehaviour
{
	[SerializeField]
	private GameObject racerObj = null;

	[SerializeField]
	private GameObject worldObj = null;

	private Racer _racer = null;
	private World _world = null;

	// Start is called before the first frame update
	private void Start()
	{
		_racer = new Racer { transform = racerObj.transform };
		_world = new World { transform = worldObj.transform };
	}

	// Update is called once per frame
	private void Update()
	{
		_racer.Update();
		_world.Update();
	}
}
