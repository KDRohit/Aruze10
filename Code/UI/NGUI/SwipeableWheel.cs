using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/**
Attach to a collider that is an area that can be swiped for paging, so that the source of the swipe area can be detected,
so that we can only do the swipe action on the thing being swiped for if there is multiple swipe areas on screen at once.
*/

public class SwipeableWheel : SwipeArea
{

	public class SpinResultInfo
	{
		public GameObject _wheel;
		public float _degrees;
		public GenericDelegate _wheelStoppedCallBack; //This is called after the spin is done.
		public GenericDelegate _wheelStartCallBack;

		public SpinResultInfo(GameObject wheel,float degrees, GenericDelegate wheelStartCallBack, GenericDelegate wheelStoppedCallBack)
		{
			_wheel = wheel;
			_degrees = degrees;
			_wheelStartCallBack = wheelStartCallBack;
			_wheelStoppedCallBack = wheelStoppedCallBack;
		}
	}


	public Camera cameraOnWheel = null;
	public SpinResultInfo spinResultInfo;
	public float pixelsBeforeSpin = 100;
	public float amountToMoveBeforeSpin;
	public bool wasSwipeTarget = false; // < toggled when you switch from being the current swipeArea.
	public Vector2 circleCenter;
	private Vector3 worldCircleCenter;	// World circle center, used only for drawing the gizmo
	private float radius;
	private float worldRadius;			// World radius, used only for drawing the gizmo
	private Vector2int lastTouch;
	public float angularVelocity = 0;
	public float sensitivity = 1;
	public float dampen = .98f;
	public float threshold = 50;
	public bool spinStarted = false;
	public int direction = 0;
	public float time = 0;
	public float degreesSinceSwipe = 0;
	public static float maxAngularVelocity = 12f;
	public Rect swipeArea;
	public WheelSpinner wheelSpinner;
	private bool thresholdReached = false;
	private bool isSwipeEnabled = true;
	private bool isPlayingCrowdNoises = true;

	/// Attempts to initialize the object. Setting the camera and calculating the Rect used for the touch events.
	public bool init(GameObject wheel,float degrees, GenericDelegate wheelStartCallBack, GenericDelegate wheelStoppedCallBack, UISprite sprite)
	{
		if (sprite == null)
		{
			return false;
		}
		
		return init(wheel, degrees, wheelStartCallBack, wheelStoppedCallBack, sprite.transform);
	}
	
	/// A variant of the above that takes in a UITexture.
	public bool init(GameObject wheel,float degrees, GenericDelegate wheelStartCallBack, GenericDelegate wheelStoppedCallBack, UITexture sprite)
	{
		if (sprite == null)
		{
			return false;
		}
		
		return init(wheel, degrees, wheelStartCallBack, wheelStoppedCallBack, sprite.transform);
	}
	
	/// A variant of the above that takes in a UITexture.
	public bool init(GameObject wheel, float degrees, GenericDelegate wheelStartCallBack, GenericDelegate wheelStoppedCallBack, Transform uiTransform, Camera passedCameraOnWheel = null, bool passedIsPlayingCrowdNoises = true, bool setSizeFromTargetCollider = false)
	{
		isPlayingCrowdNoises = passedIsPlayingCrowdNoises;

		spinResultInfo = new SpinResultInfo(wheel,degrees,wheelStartCallBack,wheelStoppedCallBack);

		// Set from box collider for certain games, transform scale by default
		if(setSizeFromTargetCollider)
		{
			BoxCollider targetCollider = uiTransform.gameObject.GetComponent<BoxCollider>();
			size.x = targetCollider.size.x;
			size.y = targetCollider.size.y;
		}
		else
		{
			size.x = uiTransform.localScale.x;
			size.y = uiTransform.localScale.y;
		}

		amountToMoveBeforeSpin = CommonGameObject.getObjectBounds(gameObject).extents.x * 0.5f;

		if (passedCameraOnWheel == null)
		{
			cameraOnWheel = NGUIExt.getObjectCamera(gameObject);
		}
		else
		{
			cameraOnWheel = passedCameraOnWheel;
		}

		swipeArea =  setupSwipeableWheelScreenRect();
		circleCenter.x = swipeArea.x + swipeArea.width/2;
		circleCenter.y = swipeArea.y + swipeArea.height/2;
		radius = Mathf.Max(swipeArea.width / 2.0f, swipeArea.height / 2.0f);
		return true;
	}

	/// Returns the area as a Rect that is scaled properly for the screen.
	/// If camera is null then this retruns a Rect(0,0,0,0).
	/// If the Parent Object is rotated this won't give you the results you expect.
	public override Rect getScreenRect()
	{
		if (swipeArea.width == 0 || swipeArea.height == 0)
		{
			 setupSwipeableWheelScreenRect();
		}

		return swipeArea;
	}

	/// Need different version of this funciton that can be called in init multiple times to account for a swipe area that
	/// moves in from off screen
	private Rect setupSwipeableWheelScreenRect()
	{
		Vector3 topLeftWorld = transform.TransformPoint(new Vector3(center.x - size.x / 2, center.y + size.y / 2, 0));
		Vector3 bottomRightWorld = transform.TransformPoint(new Vector3(center.x + size.x / 2, center.y - size.y / 2, 0));
		Vector2int topLeft = NGUIExt.screenPositionOfWorld(cameraOnWheel, topLeftWorld);
		Vector2int bottomRight = NGUIExt.screenPositionOfWorld(cameraOnWheel, bottomRightWorld);

		float x = topLeft.x;
		float y = bottomRight.y;
		float w = bottomRight.x - topLeft.x;
		float h = topLeft.y - bottomRight.y;
		swipeArea = new Rect(x, y, w, h);

		// cache out info used to render the gizmo
		float worldX = topLeftWorld.x;
		float worldY = bottomRightWorld.y;
		float worldW = bottomRightWorld.x - topLeftWorld.x;
		float worldH = topLeftWorld.y - bottomRightWorld.y;
		Rect worldSwipeArea = new Rect(worldX, worldY, worldW, worldH);

		worldRadius = Mathf.Max(worldSwipeArea.width / 2.0f, worldSwipeArea.height / 2.0f);

		worldCircleCenter = new Vector3(0, 0, 0);
		worldCircleCenter.x = worldSwipeArea.x + worldSwipeArea.width/2;
		worldCircleCenter.y = worldSwipeArea.y + worldSwipeArea.height/2;

		return swipeArea;
	}

	// Allows you to turn on and off swipes for this object. WheelSpinners will still update.
	public void enableSwipe(bool state)
	{
		isSwipeEnabled = state;
	}

	public void resetSpinResultInfo(GameObject wheel,float degrees, GenericDelegate wheelStartCallBack, GenericDelegate wheelStoppedCallBack)
	{
		wheelSpinner = null;
		spinResultInfo = new SpinResultInfo(wheel,degrees,wheelStartCallBack,wheelStoppedCallBack);
	}

	/// Checks to see if a touch is inside the circle, using squaredDistance so it's less intensive.
	/// If this is the first click on an object it will also set the proper variables.
	private bool isTouchInsideCircle()
	{
		bool result = false;
		float squareDistanceToPoint = CommonMath.sqrDistance(circleCenter.x,circleCenter.y,TouchInput.downPosition.x,TouchInput.downPosition.y);
		float squareDistanceToOutside = radius * radius;
		lastTouch = TouchInput.position;
		result = squareDistanceToPoint < squareDistanceToOutside;
		
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
				if (thresholdReached && wheelSpinner == null && Mathf.Abs(degreesSinceSwipe) > 30f) 
				{
					if (direction > 0)
					{
						//we need to calculate the correct amount of degrees
						// Counter Clockwise
						wheelSpinner = new WheelSpinner (spinResultInfo._wheel, 360 - spinResultInfo._degrees, spinResultInfo._wheelStoppedCallBack, true, -80.0f, 0.0f, isPlayingCrowdNoises);
					}
					else
					{
						wheelSpinner = new WheelSpinner (spinResultInfo._wheel, spinResultInfo._degrees, spinResultInfo._wheelStoppedCallBack, false, -80.0f, 0.0f, isPlayingCrowdNoises);
					}
					wheelSpinner.setStartingAngularVelocity(Mathf.Abs(angularVelocity / Time.deltaTime));
					angularVelocity = 0;
					spinResultInfo._wheelStartCallBack();
				}
				else if (Mathf.Abs(angularVelocity) < 1)
				{
					angularVelocity = 0;
					wasSwipeTarget = false;
					spinStarted = false;
				}

				//Animate the wheel from spinning
				gameObject.transform.Rotate(Vector3.forward * angularVelocity);
				angularVelocity *= dampen;
			}
			
			if (wheelSpinner == null) //We have not started the spin
			{
				//This is the swipe object that we are using.
				if (TouchInput.swipeObject == this.gameObject)
				{
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
						angularVelocity = (degreesSinceSwipe / (Time.time - time)) * Time.deltaTime;
						if (Mathf.Abs(angularVelocity) > maxAngularVelocity * 0.5f)
						{
							thresholdReached = true;
							angularVelocity = Mathf.Sign(angularVelocity) * maxAngularVelocity;
						}
					}
				}
			}
		}
	}

	/// Does the physics required to calculate angular velocity.
	public void calculateWheelSpin()
	{
		Vector2int moveVectorint = TouchInput.position - lastTouch;
		Vector2 moveVector = new Vector2(moveVectorint.x, moveVectorint.y) / sensitivity;
		Vector2 vectorToPos = new Vector2(TouchInput.position.x, TouchInput.position.y) -  circleCenter;
		lastTouch = TouchInput.position;
		
		if (moveVector != Vector2.zero)
		{
			Vector2 tangentVelocityVector = (moveVector * Mathf.Sin(Vector2.Angle(moveVector, vectorToPos)));
			float tangentVelocity = tangentVelocityVector.magnitude;
			Vector3 crossProduct = Vector3.Cross(vectorToPos,moveVector);
			int newdirection = crossProduct.normalized == Vector3.forward ? 1 : -1;
			//Check to see if we have changed direction. If we have then our velocity needs to be modified.
			if (newdirection != direction)
			{
				degreesSinceSwipe = 0;
				time = Time.time;
				direction = newdirection;
				angularVelocity = 0;
			}
			//Debug.Log("degreesSinceSwipe = " + degreesSinceSwipe + "angularVelocity = " + (degreesSinceSwipe / (Time.time - time)));
			float effectiveRadius = isTouchInsideCircle() ? vectorToPos.magnitude : radius;
			float angularVelocityRad = tangentVelocity / effectiveRadius;
			angularVelocity = direction * CommonMath.radiansToDegrees(angularVelocityRad);
			degreesSinceSwipe += angularVelocity;

			gameObject.transform.Rotate(Vector3.forward * angularVelocity);
		}
	}

	/// Draw gizmo showing the circular hit area
	protected override void OnDrawGizmosSelected()
	{
		// Draw outline
		Gizmos.color = Color.red;
		Gizmos.DrawWireSphere(worldCircleCenter, worldRadius);
	}
}

