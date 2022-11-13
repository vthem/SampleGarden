using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PIDTester_Behaviour : MonoBehaviour
{
	public float targetAltitude = 1f;
	public TSW.Algorithm.PIDController pidController;

	public float verticalSpeed = -1f;
	public float thrust = 0f;

	public float currentAltitude = 0f;

	private Vector3 initPos;

    // Start is called before the first frame update
    void Start()
    {
		initPos = transform.position;
	}

    // Update is called once per frame
    void Update()
    {
		Ray r = new Ray(transform.position, Vector3.down);
		if (Physics.Raycast(r, out RaycastHit hit, 10f))
		{
			currentAltitude = (transform.position - hit.point).magnitude;
			Debug.DrawLine(transform.position, hit.point, Color.green);
			transform.position += Vector3.up * verticalSpeed * Time.deltaTime;

			currentAltitude = (transform.position - hit.point).magnitude;
			thrust = pidController.Update(targetAltitude, currentAltitude, Time.deltaTime);
			transform.position += Vector3.up * thrust * Time.deltaTime;
		}

		if (Input.GetKeyDown(KeyCode.Space))
		{
			transform.position = initPos;
			thrust = 0f;
			pidController.Reset();
		}
	}
}
