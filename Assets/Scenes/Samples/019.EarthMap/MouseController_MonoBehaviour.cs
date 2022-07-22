using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseController_MonoBehaviour : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		RaycastHit hit;
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

		if (Physics.Raycast(ray, out hit))
		{
			Transform objectHit = hit.transform;
			Debug.Log($"mouse:{Input.mousePosition} hit:{objectHit.name}");

			// Do something with the object that was hit by the raycast.
		}
	}
}
