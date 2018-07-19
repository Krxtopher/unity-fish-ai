using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Provides fish behavior including swimming animation, obstacle avoidance, and
/// wandering behavior.
/// </summary>
public class Fish : MonoBehaviour
{

	/// <summary>
	/// A location inside the tank that will be used as a reference point when
	/// calculating turns to avoid obstacles.
	/// </summary>
	public Transform tankCenterGoal;

	/// <summary>
	/// Indicates how close an obstacle must be (in meters) before the fish 
	/// begins to take evasive action. 
    /// </summary>
    public float obstacleSensingDistance = 0.8f;

	/// <summary>
	/// The minimum speed this fish should move in meters/second.
	/// </summary>
	public float swimSpeedMin = 0.2f;

	/// <summary>
	/// The maximum speed this fish should move in meters/second.
	/// </summary>
	public float swimSpeedMax = 0.6f;

	/// <summary>
    /// Controls how quickly the fish can turn.
    /// </summary>
    public float maxTurnRateY = 5f;

	/// <summary>
    /// When the fish randomly changes direction while wondering, this value
    /// controls the maximum allowed change in direction.
    /// </summary>
    public float maxWanderAngle = 45f;

	/// <summary>
	/// Sets the duration of each wander period (in seconds). At the start of 
	/// each wander period the fish is given an opportunity to change direction. 
	/// The likelihood of changing direction at each period is controlled by
    /// <tt>wanderProbability</tt>.
    /// </summary>
    public float wanderPeriodDuration = 0.8f;

	/// <summary>
    /// Indicates how likely the fish is to turn while wondering. A value from 
    /// 0 through 1.
    /// </summary>
    public float wanderProbability = 0.15f;
       

	// The current speed of the fish in meters/second.
	[HideInInspector]
	public float swimSpeed;

	// The fish's current direction of movement.
    private Vector3 swimDirection
    {
        get { return transform.TransformDirection(Vector3.forward); }
    }

	// Flag to track whether an obstacle has been detected.
	private bool obstacleDetected = false;

	// The timestamp indicating when the current wander period started.
	private float wanderPeriodStartTime;

	// The orientation goal that the fish is rotating toward over time.
	private Quaternion goalLookRotation;

	// Cached reference to the fish body's transform.
	private Transform bodyTransform;

	// A random value set dynamically so that each fish's behavior is slightly
	// different.
	private float randomOffset;

	// Location variables used to draw debug aids.
	private Vector3 hitPoint;
	private Vector3 goalPoint;


	/* ----- MonoBehaviour Methods ----- */


	void Start()
	{
		// Warn the developer loudly if they haven't set tankCenterGoal.
		if (tankCenterGoal == null)
		{
			Debug.LogError("[" + name + "] The tankCenterGoal parameter is required but is null.");
			UnityEditor.EditorApplication.isPlaying = false;
		}

		bodyTransform = transform.Find("Body");
		randomOffset = Random.value;
	}


	private void Update()
	{
		Wiggle();
		Wander();
		AvoidObstacles();

		DrawDebugAids();
		UpdatePosition();
	}


	private void OnDrawGizmos()
	{
		DrawDebugAids();
	}


	/* ----- Fish Methods ----- */


	/// <summary>
	/// Updates the fish's wiggle animation.
	/// </summary>
	void Wiggle()
	{
		// Calculate a wiggle speed (wiggle cycles per second) based on the 
		// fish's forward speed.
		float speedPercent = swimSpeed / swimSpeedMax;
		float minWiggleSpeed = 12f;
		float maxWiggleSpeed = minWiggleSpeed + 1f;
		float wiggleSpeed = Mathf.Lerp(minWiggleSpeed, maxWiggleSpeed, speedPercent);

		// Use sine and game time to animate the wiggle rotation of the fish.
		float angle = Mathf.Sin(Time.time * wiggleSpeed) * 5f;
		var wiggleRotation = Quaternion.AngleAxis(angle, Vector3.up);
		bodyTransform.localRotation = wiggleRotation;
	}


	/// <summary>
	/// Defines the fish's wander behavior.
	/// </summary>
	void Wander()
	{
		// User Perlin noise to change the fish's speed over time in a random
		// but smooth fashion.
		float noiseScale = .5f;
		float speedPercent = Mathf.PerlinNoise(Time.time * noiseScale + randomOffset, randomOffset);
		speedPercent = Mathf.Pow(speedPercent, 2);
		swimSpeed = Mathf.Lerp(swimSpeedMin, swimSpeedMax, speedPercent);

		if (obstacleDetected) return;

		if (Time.time > wanderPeriodStartTime + wanderPeriodDuration)
		{
			// Start a new wander period.
			wanderPeriodStartTime = Time.time;

			if (Random.value < wanderProbability)
			{
				// Pick new wander direction.
				var randomAngle = Random.Range(-maxWanderAngle, maxWanderAngle);
				var relativeWanderRotation = Quaternion.AngleAxis(randomAngle, Vector3.up);
				goalLookRotation = transform.rotation * relativeWanderRotation;
			}
		}

		// Turn toward the fish's goal rotation.
		transform.rotation = Quaternion.Slerp(transform.rotation, goalLookRotation, Time.deltaTime / 2f);
	}


	/// <summary>
	/// Defines the fish's obstacle avoidance behavior.
	/// </summary>
	void AvoidObstacles()
	{
		// Look ahead to see if an obstacle is within range.
		RaycastHit hit;
		obstacleDetected = Physics.Raycast(transform.position, swimDirection, out hit, obstacleSensingDistance);
              
		if (obstacleDetected)
		{
			hitPoint = hit.point;

			// Calculate a point (which we're calling "reflectedPoint") indicating
			// where the fish would end up if it bounced off of the obstacle and
			// continued travelling. This will be one of our points of reference
			// for determining a new safe goal point.
			Vector3 reflectionVector = Vector3.Reflect(swimDirection, hit.normal);
			float goalPointMinDistanceFromHit = 1f;
			Vector3 reflectedPoint = hit.point + reflectionVector * Mathf.Max(hit.distance, goalPointMinDistanceFromHit);

			// Set the goal point to halfway between the reflected point above
			// and the tank center goal.
			goalPoint = (reflectedPoint + tankCenterGoal.position) / 2f;

			// Set the rotation we eventually want to achieve.
			Vector3 goalDirection = goalPoint - transform.position;
			goalLookRotation = Quaternion.LookRotation(goalDirection);

			// Determine a danger level using a exponential scale so that danger
			// ramps up more quickly as the fish gets nearer obstacle.
			float dangerLevel = Mathf.Pow(1 - (hit.distance / obstacleSensingDistance), 4f);

            // Clamp minimum danger level to 0.01.
			dangerLevel = Mathf.Max(0.01f, dangerLevel);

			// Use dangerLevel to influence how quickly the fish turns toward
			// its goal direction.
			float turnRate = maxTurnRateY * dangerLevel;

			// Rotate the fish toward its goal direction.
			Quaternion rotation = Quaternion.Slerp(transform.rotation, goalLookRotation, Time.deltaTime * turnRate);
			transform.rotation = rotation;
		}
	}


	/// <summary>
	/// Draws visual debug aids that can be seen in the editor viewport.
	/// </summary>
	void DrawDebugAids()
	{
		// Draw lines from the fish illustrating what it "sees" and what
		// evasive action it may be taking.

		Color rayColor = obstacleDetected ? Color.red : Color.cyan;
		Debug.DrawRay(transform.position, swimDirection * obstacleSensingDistance, rayColor);

		if (obstacleDetected)
		{
			Debug.DrawLine(hitPoint, goalPoint, Color.green);
		}

	}


	/// <summary>
	/// Updates the fish's position as it swims.
	/// </summary>
	private void UpdatePosition()
	{
		Vector3 position = transform.position + swimDirection * swimSpeed * Time.fixedDeltaTime;
		transform.position = position;
	}
}
