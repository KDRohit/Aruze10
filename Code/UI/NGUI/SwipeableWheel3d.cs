using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Controls the swipeable 3D spinner thing in the t201 Wheel game.
*/

public class SwipeableWheel3d : SwipeArea
{

	public class SpinResultInfo
	{
		public float degrees;
		public GenericDelegate wheelStoppedCallBack; //This is called after the spin is done.
		public GenericDelegate wheelStartCallBack;

		public SpinResultInfo(float degrees, GenericDelegate wheelStartCallBack, GenericDelegate wheelStoppedCallBack)
		{
			this.degrees = degrees;
			this.wheelStartCallBack = wheelStartCallBack;
			this.wheelStoppedCallBack = wheelStoppedCallBack;
		}
	}

	public GameObject wheel;
	public Texture tex;
	public bool draw;
	public Camera cameraOnWheel = null;
	public SpinResultInfo spinResultInfo;
	public float pixelsBeforeSpin = 100;
	public float amountToMoveBeforeSpin;
	public bool wasSwipeTarget = false; // < toggled when you switch from being the current swipeArea.
	public Vector2 circleCenter;
	private float radius;
	private Vector2int lastTouch;
	public float velocity = 0;
	public float velocitySensitivity = 3E-5F;
	public float angularVelocity = 0;
	public float sensitivity = 1;
	public float dampen = .9f;
	public float threshold = 50;
	public bool spinStarted = false;
	public int direction = 0;
	public float time = 0;
	public float degreesSinceSwipe = 0;
	public float maxAngularVelocity = .05f;
	public Rect swipeArea;
	
	private WheelSpinner3d wheelSpinner;
	private bool thresholdReached = false;
	private bool isSwipeEnabled = true;
	private Renderer wheelRenderer = null;

	/// Attempts to initialize the object. Setting the camera and calculating the Rect used for the touch events.
	public bool init(GameObject wheel, float degrees, GenericDelegate wheelStartCallBack, GenericDelegate wheelStoppedCallBack)
	{
		if (wheel == null)
		{
			return false;
		}
		this.wheel = wheel;
		wheelRenderer = wheel.GetComponent<Renderer>();	// cache for performance
		spinResultInfo = new SpinResultInfo(degrees, wheelStartCallBack, wheelStoppedCallBack);
		cameraOnWheel = NGUIExt.getObjectCamera(gameObject);
		swipeArea = getScreenRect();
		circleCenter.x = swipeArea.x + swipeArea.width/2;
		circleCenter.y = swipeArea.y + swipeArea.height/2;
		return true;
	}

	/// Returns the area as a Rect that is scaled properly for the screen.
	/// If camera is null then this retruns a Rect(0,0,0,0).
	/// If the Parent Object is rotated this won't give you the results you expect.
	public override Rect getScreenRect()
	{
		if (swipeArea.width == 0 || swipeArea.height == 0)
		{
			swipeArea = BoundsToScreenRect(wheelRenderer.bounds);
		}
		return swipeArea;
	}

	// Takes the bounds from a renderer and gives you a ScreenRect. This assumes the extents of the mesh are on the same plane.
	public Rect BoundsToScreenRect(Bounds bounds)
	{
		Vector3 center = bounds.center;
		Vector3 extent = bounds.extents;
		// List of all the extentPoints (more points needed if the extents are not on the same plane)
		Vector2[] extentPoints = new Vector2[4]
		{
		new Vector2(center.x-extent.x, center.y-extent.y),
		new Vector2(center.x-extent.x, center.y+extent.y),
		new Vector2(center.x+extent.x, center.y-extent.y),
		new Vector2(center.x+extent.x, center.y+extent.y)
		};
		 
		Vector2 min = extentPoints[0];
		Vector2 max = extentPoints[0];
		// Get the max and Min extents
		foreach(Vector2 v in extentPoints)
		{
		min = Vector2.Min(min, v);
		max = Vector2.Max(max, v);
		}
		// Convert to screen coords
		Vector3 minScreen = cameraOnWheel.WorldToScreenPoint(new Vector3(min.x,min.y,0));
		Vector3 maxScreen = cameraOnWheel.WorldToScreenPoint(new Vector3(max.x,max.y,0));
		 
		return new Rect(minScreen.x, minScreen.y, maxScreen.x-minScreen.x, maxScreen.y-minScreen.y);
	}



	// Allows you to turn on and off swipes for this object. WheelSpinners will still update.
	public void enableSwipe(bool state)
	{
		isSwipeEnabled = state;
	}

	public void resetSpinResultInfo(float degrees, GenericDelegate wheelStartCallBack, GenericDelegate wheelStoppedCallBack)
	{
		wheelSpinner = null;
		spinResultInfo = new SpinResultInfo(degrees, wheelStartCallBack, wheelStoppedCallBack);
	}

	/// Checks to see if a touch is inside the circle, using squaredDistance so it's less intensive.
	/// If this is the first click on an object it will also set the proper variables.
	private bool isTouchInsideCircle()
	{
		lastTouch = TouchInput.position;
		bool result = true;
		// Check to make sure the the mesh was actually hit.
		
		if (!wasSwipeTarget && result) //This is the first touch getting the object.
		{
			//Debug.Log("isTouchInsideCircle");
			wasSwipeTarget = true;
			time = Time.time;
			degreesSinceSwipe = 0;
			angularVelocity = 0;
			spinStarted = false; //Once it's touched we don't want to spin anymore.
		}
		return result;
	}

	protected virtual void Update()
	{
		if (wheelSpinner != null)
		{
			wheelSpinner.updateWheel();
		}
		if (isSwipeEnabled)
		{
			if (spinStarted)
			{
				//Debug.Log("degreesSinceSwipe: " + degreesSinceSwipe);
				
				//Check to see if the spin should have started. When degrees goes from 360 -> 0 you pass the maxAngularVelocty. 
				if (thresholdReached && wheelSpinner == null) 
				{
					Debug.Log("We are spinning!");
					spinResultInfo.wheelStartCallBack();
					if (direction < 0)
					{
						//we need to calculate the correct amount of degrees
						wheelSpinner = new WheelSpinner3d (wheel, 360 - spinResultInfo.degrees, spinResultInfo.wheelStoppedCallBack, true);
					}
					else
					{
						wheelSpinner = new WheelSpinner3d (wheel, spinResultInfo.degrees, spinResultInfo.wheelStoppedCallBack, false);
					}
					wheelSpinner.setStartingAngularVelocity(Mathf.Abs(angularVelocity / Time.deltaTime));
					angularVelocity = 0;
				}
				else if (Mathf.Abs(velocity) < .001)
				{
					velocity = 0;
					wasSwipeTarget = false;
					spinStarted = false;
				}

				//Animate the wheel from spinning
				wheelRenderer.materials[0].mainTextureOffset += -1f * Vector2.right * velocity;

				velocity *= dampen;
			}
			
			if (wheelSpinner == null) //We have not started the spin
			{
				//This is the swipe object that we are using.
				if (TouchInput.swipeObject == this.gameObject)
				{
					Debug.Log("We have clicked the area!");
					if (wasSwipeTarget || isTouchInsideCircle()) //This only needs to happen once.
					{
						calculateWheelSpin();
					}
					else
					{
						//Debug.Log("Making swipeArea null");
						TouchInput.swipeArea = null; //You didn't click the wheel
					}
				}
				else if (TouchInput.swipeObject != this.gameObject)
				{
					wasSwipeTarget = false;
					if (angularVelocity != 0)
					{
						spinStarted = true;
						//Debug.Log("angularVelocity (degreesPerFrame) = " + (degreesSinceSwipe / (Time.time - time)));
						//angularVelocity = (degreesSinceSwipe / (Time.time - time)) * Time.deltaTime;
						if (Mathf.Abs(angularVelocity) > maxAngularVelocity * 0.5f)
						{
							Debug.Log("We are spinning fast enough!");
							thresholdReached = true;
							angularVelocity = Mathf.Sign(angularVelocity) * maxAngularVelocity;
						}
					}
				}
			}
		}
	}

	/// Does the physics required to calculate angular velocity. This is pretty simple because this 3d wheel only moves left and right.
	public void calculateWheelSpin()
	{
		Vector2int moveVectorint = TouchInput.position - lastTouch;
		Vector2 moveVector = new Vector2(moveVectorint.x, moveVectorint.y) / sensitivity;
		moveVector.y = 0; // We only care about the x component.
		lastTouch = TouchInput.position;

		if (moveVector.x != 0)
		{
			int newdirection = (int)Mathf.Sign(moveVector.x);
			// Check to see if we have changed direction
			if (newdirection != direction)
			{
				time = Time.time;
				direction = newdirection;
				velocity = 0;
			}
			velocity += moveVector.x * velocitySensitivity / Time.deltaTime;
			angularVelocity = velocity * 360 / time;

			wheelRenderer.materials[0].mainTextureOffset = -1f * Vector2.right * TouchInput.dragDistanceX / 360f / (MobileUIUtil.getDotsPerInch() / 160f);
		}
		else
		{
			velocity = 0;
		}
	}
}

