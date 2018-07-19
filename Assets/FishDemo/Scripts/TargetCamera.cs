using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Camera behavior which tracks the position of a target object. 
/// </summary>
public class TargetCamera : MonoBehaviour
{
	/// <summary>
    /// The object the camera should follow.
    /// </summary>
	public Transform targetObject;

	/// <summary>
	/// The number of seconds it should take for the camer to catch up to the 
	/// target when not pointed directy at it.
	/// </summary>
	public float followSpeed = 1.0f;


	void Update()
	{
		var goalDirection = targetObject.position - transform.position;
		var goalRotation = Quaternion.LookRotation(goalDirection, Vector3.up);
		var interimRotation = Quaternion.Slerp(transform.rotation, goalRotation, Time.deltaTime / followSpeed);

		transform.rotation = interimRotation;
	}

}