using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class NewTestScript
{

	[Test]
	public void NewTestScriptSimplePasses()
	{
		Vector3 direction = new Vector3(1f, 0f, -1f).normalized;
		Vector3 normal = new Vector3(0f, 0, 1f).normalized;
		float result = Vector3.Dot(direction, normal);
		Debug.Log("result: " + result);
	}

}
